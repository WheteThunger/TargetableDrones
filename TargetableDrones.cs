using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    [Info("Targetable Drones", "WhiteThunder", "0.2.0")]
    [Description("Allows RC drones to be targeted by Auto Turrets and SAM Sites.")]
    internal class TargetableDrones : CovalencePlugin
    {
        #region Fields

        [PluginReference]
        private Plugin Clans, Friends, DroneScaleManager;

        private const string PermissionUntargetable = "targetabledrones.untargetable";

        private static TargetableDrones _pluginInstance;
        private static Configuration _pluginConfig;

        #endregion

        #region Hooks

        private void Init()
        {
            _pluginInstance = this;

            permission.RegisterPermission(PermissionUntargetable, this);

            Unsubscribe(nameof(OnEntitySpawned));
        }

        private void OnServerInitialized()
        {
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                var drone = entity as Drone;
                if (drone == null || !IsDroneEligible(drone))
                    continue;

                OnEntitySpawned(drone);
            }

            Subscribe(nameof(OnEntitySpawned));
        }

        private void Unload()
        {
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                var drone = entity as Drone;
                if (drone == null || !IsDroneEligible(drone))
                    continue;

                var turretComponent = drone.GetComponent<TurretTargetComponent>();
                if (turretComponent != null)
                    UnityEngine.Object.DestroyImmediate(turretComponent);

                var samComponent = drone.GetComponent<SAMTargetComponent>();
                if (samComponent != null)
                    UnityEngine.Object.DestroyImmediate(samComponent);
            }

            _pluginConfig = null;
            _pluginInstance = null;
        }

        private void OnEntitySpawned(Drone drone)
        {
            if (!IsDroneEligible(drone))
                return;

            MaybeAddTargetComponents(drone);
        }

        // Avoid unwanted trigger interactions.
        private bool? OnEntityEnter(TriggerBase trigger, Drone drone)
        {
            if (trigger is PlayerDetectionTrigger)
            {
                // Only allow interaction with Laser Detectors.
                // This avoids NREs with HBHF sensors or anything unknown.
                if (trigger.GetComponentInParent<BaseEntity>() is LaserDetector)
                    return null;

                return false;
            }

            if (trigger is TargetTrigger)
            {
                // Only allow interaction with Auto Turrets.
                // This avoids NREs with flame turrets, shotgun traps, tesla coils, or anything unknown.
                if (trigger.GetComponentInParent<BaseEntity>() is AutoTurret)
                    return null;

                return false;
            }

            return null;
        }

        // Adjust the sam site aim last minute as it's about to shoot.
        // This addresses the problem where vanilla targeting can't predict drone movement.
        private void CanSamSiteShoot(SamSite samSite)
        {
            var target = samSite.currentTarget;
            if (target == null)
                return;

            var drone = target as Drone;
            if (drone == null)
                return;

            var estimatedPoint = GetEstimatedPosition(samSite, GetLocalVelocityServer(drone));
            samSite.currentAimDir = (estimatedPoint - samSite.eyePoint.transform.position).normalized;
        }

        private bool? OnTurretTarget(AutoTurret turret, Drone drone)
        {
            if (turret == null || drone == null)
                return null;

            // Drones are not inherently hostile.
            if (turret is NPCAutoTurret)
                return false;

            if (!IsTargetable(drone))
                return false;

            // Don't allow a drone turret to target its parent drone.
            if (GetParentDrone(turret) == drone)
                return false;

            // If the drone has a turret, consider the drone owned by the turret owner.
            var droneOwnerId = GetDroneTurret(drone)?.OwnerID ?? drone.OwnerID;

            if (turret.OwnerID == 0 || droneOwnerId == 0)
                return null;

            // Direct authorization trumps anything else.
            if (IsAuthorized(turret, droneOwnerId))
                return false;

            // In case the owner lost authorization, don't share with team/friends/clan.
            if (!IsAuthorized(turret, turret.OwnerID))
                return null;

            if (turret.OwnerID == droneOwnerId
                || _pluginConfig.DefaultSharingSettings.Team && SameTeam(turret.OwnerID, droneOwnerId)
                || _pluginConfig.DefaultSharingSettings.Friends && HasFriend(turret.OwnerID, droneOwnerId)
                || _pluginConfig.DefaultSharingSettings.Clan && SameClan(turret.OwnerID, droneOwnerId)
                || _pluginConfig.DefaultSharingSettings.Allies && AreAllies(turret.OwnerID, droneOwnerId))
                return false;

            return null;
        }

        private bool? OnSamSiteTarget(SamSite samSite, Drone drone)
        {
            if (samSite.staticRespawn || samSite.OwnerID == 0)
                return null;

            // If the drone has a turret, consider the drone owned by the turret owner.
            var droneOwnerId = GetDroneTurret(drone)?.OwnerID ?? drone.OwnerID;
            if (droneOwnerId == 0)
                return null;

            if (samSite.OwnerID == droneOwnerId
                || _pluginConfig.DefaultSharingSettings.Team && SameTeam(samSite.OwnerID, droneOwnerId)
                || _pluginConfig.DefaultSharingSettings.Friends && HasFriend(samSite.OwnerID, droneOwnerId)
                || _pluginConfig.DefaultSharingSettings.Clan && SameClan(samSite.OwnerID, droneOwnerId)
                || _pluginConfig.DefaultSharingSettings.Allies && AreAllies(samSite.OwnerID, droneOwnerId))
                return false;

            return null;
        }

        // Make drone turrets retaliate against other turrets.
        private void OnEntityTakeDamage(Drone drone, HitInfo info)
        {
            // Ignore if not attacked by a turret.
            var turretInitiator = info?.Initiator as AutoTurret;
            if (turretInitiator == null)
                return;

            // Ignore if this drone does not have a turret since it can't retaliate.
            var droneTurret = GetDroneTurret(drone);
            if (droneTurret == null)
                return;

            // Ignore if the turret damaged its owner drone.
            if (droneTurret == turretInitiator)
                return;

            // Ignore if the turret is not online or has an existing visible target.
            if (!droneTurret.IsOnline()
                || droneTurret.HasTarget() && droneTurret.targetVisible)
                return;

            var attackerDrone = GetParentDrone(turretInitiator);
            if (attackerDrone != null)
            {
                // If the attacker turret is on a drone, target that drone.
                droneTurret.SetTarget(attackerDrone);
                return;
            }

            // Shoot back at the turret.
            droneTurret.SetTarget(turretInitiator);
        }

        private void OnDroneScaled(Drone drone, BaseEntity rootEntity, float scale, float previousScale)
        {
            if (scale == 1 || rootEntity.IsDestroyed)
                return;

            TurretTargetComponent.AddToRootEntityIfMissing(drone, rootEntity);
        }

        #endregion

        #region Helper Methods

        private static bool SameTeam(ulong userId, ulong otherUserId) =>
            RelationshipManager.ServerInstance.FindPlayersTeam(userId)?.members.Contains(otherUserId) ?? false;

        private static bool HasFriend(ulong userId, ulong otherUserId)
        {
            var friendsResult = _pluginInstance.Friends?.Call("HasFriend", userId, otherUserId);
            return friendsResult is bool && (bool)friendsResult;
        }

        private static bool SameClan(ulong userId, ulong otherUserId)
        {
            var clanResult = _pluginInstance.Clans?.Call("IsClanMember", userId.ToString(), otherUserId.ToString());
            return clanResult is bool && (bool)clanResult;
        }

        private static bool AreAllies(ulong userId, ulong otherUserId)
        {
            var clanResult = _pluginInstance.Clans?.Call("IsAllyPlayer", userId.ToString(), otherUserId.ToString());
            return clanResult is bool && (bool)clanResult;
        }

        private static BaseEntity GetRootEntity(Drone drone)
        {
            return _pluginInstance.DroneScaleManager?.Call("API_GetRootEntity", drone) as BaseEntity;
        }

        private static bool IsAuthorized(AutoTurret turret, ulong userId)
        {
            foreach (var entry in turret.authorizedPlayers)
            {
                if (entry.userid == userId)
                    return true;
            }
            return false;
        }

        private static void MaybeAddTargetComponents(Drone drone)
        {
            if (_pluginConfig.EnableTurretTargeting)
                TurretTargetComponent.AddToDroneIfMissing(drone);

            if (_pluginConfig.EnableSAMTargeting)
                SAMTargetComponent.AddToDroneIfMissing(drone);
        }

        private static bool IsDroneEligible(Drone drone) =>
            !(drone is DeliveryDrone);

        private static Drone GetParentDrone(BaseEntity entity)
        {
            var sphereEntity = entity.GetParentEntity() as SphereEntity;
            return sphereEntity != null ? sphereEntity.GetParentEntity() as Drone : null;
        }

        private static AutoTurret GetDroneTurret(Drone drone) =>
            drone.GetSlot(BaseEntity.Slot.UpperModifier) as AutoTurret;

        private static bool IsTargetable(Drone drone)
        {
            if (drone.isGrounded || !drone.IsBeingControlled)
                return false;

            if (drone.OwnerID == 0)
                return true;

            if (_pluginInstance.permission.UserHasPermission(drone.OwnerID.ToString(), PermissionUntargetable))
                return false;

            return true;

        }

        private Vector3 EntityCenterPoint(BaseEntity entity) =>
            entity.transform.TransformPoint(entity.bounds.center);

        private Vector3 GetLocalVelocityServer(Drone drone) =>
            drone.body.velocity;

        private Vector3 GetEstimatedPosition(SamSite samSite, Vector3 targetVelocity)
        {
            // Copied from vanilla code for predicting target movement.
            var eyePointPosition = samSite.eyePoint.transform.position;
            float speed = samSite.projectileTest.Get().GetComponent<ServerProjectile>().speed;
            Vector3 centerPoint = EntityCenterPoint(samSite.currentTarget);

            float entityDistance = Vector3.Distance(centerPoint, eyePointPosition);
            float travelTime = entityDistance / speed;
            Vector3 estimatedPoint = centerPoint + targetVelocity * travelTime;

            travelTime = Vector3.Distance(estimatedPoint, eyePointPosition) / speed;
            estimatedPoint = centerPoint + targetVelocity * travelTime;

            if (targetVelocity.magnitude > 0.1f)
            {
                float adjustment = Mathf.Sin(Time.time * 3f) * (1f + travelTime * 0.5f);
                estimatedPoint += targetVelocity.normalized * adjustment;
            }

            return estimatedPoint;
        }

        #endregion

        #region Custom Targeting

        private class TurretTargetComponent : EntityComponent<BaseEntity>
        {
            public static void AddToDroneIfMissing(Drone drone)
            {
                // Must be added to the drone itself since the root entity (SphereEntity) is not a BaseCombatEntity.
                drone.GetOrAddComponent<TurretTargetComponent>().InitForDrone(drone);

                // Add to the root entity to ensure consistency with side effect of landing on cargo ship.
                var rootEntity = GetRootEntity(drone);
                if (rootEntity != null)
                    AddToRootEntityIfMissing(drone, rootEntity);
            }

            public static void AddToRootEntityIfMissing(Drone drone, BaseEntity rootEntity) =>
                rootEntity.GetOrAddComponent<TurretTargetComponent>().InitForDrone(drone);

            private Drone _ownerDrone;
            private GameObject _child;

            private TurretTargetComponent InitForDrone(Drone drone)
            {
                _ownerDrone = drone;
                AddChildLayerForAutoTurrets();
                return this;
            }

            private void AddChildLayerForAutoTurrets()
            {
                _child = gameObject.CreateChild();
                _child.layer = (int)Rust.Layer.Player_Server;

                var triggerCollider = _child.gameObject.AddComponent<BoxCollider>();
                triggerCollider.size = _ownerDrone.bounds.extents;
                triggerCollider.isTrigger = true;
            }

            private void OnDestroy()
            {
                if (_child != null)
                    Destroy(_child);
            }
        }

        private class SAMTargetComponent : EntityComponent<Drone>, SamSite.ISamSiteTarget
        {
            public static void AddToDroneIfMissing(Drone drone) =>
                drone.GetOrAddComponent<SAMTargetComponent>();

            // SAM Site vanilla targeting will call this method.
            public bool IsValidSAMTarget() =>
                IsTargetable(baseEntity);
        }

        #endregion

        #region Configuration

        private class SharingSettings
        {
            [JsonProperty("Team")]
            public bool Team = false;

            [JsonProperty("Friends")]
            public bool Friends = false;

            [JsonProperty("Clan")]
            public bool Clan = false;

            [JsonProperty("Allies")]
            public bool Allies = false;
        }

        private class Configuration : SerializableConfiguration
        {
            [JsonProperty("EnableTurretTargeting")]
            public bool EnableTurretTargeting = true;

            [JsonProperty("EnableSAMTargeting")]
            public bool EnableSAMTargeting = true;

            [JsonProperty("DefaultSharingSettings")]
            public SharingSettings DefaultSharingSettings = new SharingSettings();
        }

        private Configuration GetDefaultConfig() => new Configuration();

        #endregion

        #region Configuration Boilerplate

        private class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        private static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(SerializableConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        protected override void LoadDefaultConfig() => _pluginConfig = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _pluginConfig = Config.ReadObject<Configuration>();
                if (_pluginConfig == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_pluginConfig))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(_pluginConfig, true);
        }

        #endregion
    }
}
