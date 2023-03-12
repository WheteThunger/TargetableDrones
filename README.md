## Features

- Allows SAM Sites to target RC drones
- Allows player Auto Turrets to target RC drones
- Allows sharing turret/SAM authorization with team, clan and friends to avoid targeting their drones

**Highly recommended**: [Drone Settings](https://umod.org/plugins/drone-settings) -- Allows configuring toughness of drones. Without this, Auto Turrets and SAM Sites may not be able to destroy drones.

## Permissions

- `targetabledrones.untargetable` -- Drones deployed by players with this permission cannot be targeted by any Auto Turrets or SAM Sites.

## Configuration

Default configuration:

```json
{
  "EnableTurretTargeting": true,
  "EnableSAMTargeting": true,
  "NPCTargeting": {
    "Range": 60.0,
    "EnabledByNpcPrefab": {
      "assets/prefabs/npc/gingerbread/gingerbread_dungeon.prefab": false,
      "assets/prefabs/npc/gingerbread/gingerbread_meleedungeon.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/banditguard/npc_bandit_guard.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_cargo.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_cargo_turret_any.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_cargo_turret_lr300.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_ch47_gunner.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_excavator.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_any.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_lr300.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_mp5.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_pistol.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_shotgun.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_junkpile_pistol.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_oilrig.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_patrol.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_peacekeeper.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roam.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roamtethered.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/tunneldweller/npc_tunneldweller.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/tunneldweller/npc_tunneldwellerspawned.prefab": false,
      "assets/rust.ai/agents/npcplayer/humannpc/underwaterdweller/npc_underwaterdweller.prefab": false
    }
  },
  "DefaultSharingSettings": {
    "Team": false,
    "Friends": false,
    "Clan": false,
    "Allies": false
  }
}
```

- `EnableTurretTargeting` (`true` or `false`) -- While `true`, drones can be targeted by Auto Turrets.
- `EnableSAMTargeting` (`true` or `false`) -- While `true`, drones can be targeted by SAM Sites.
- `NPCTargeting`
  - `Range` (Default: `60.0`) -- Determines how far away NPCs can target drones.
  - `EnabledByNpcPrefab` -- Determines whether each type of NPC can target drones.
- `DefaultSharingSettings` (each `true` or `false`) -- These settings determine whether a player's Auto Turrets or SAM Sites will target drones deployed by their teammates, friends, clanmates, or ally clanmates.

## Recommended compatible plugins

Drone balance:
- [Drone Settings](https://umod.org/plugins/drone-settings) -- Allows changing speed, toughness and other properties of RC drones.
- [Targetable Drones](https://umod.org/plugins/targetable-drones) (This plugin) -- Allows RC drones to be targeted by Auto Turrets and SAM Sites.
- [Limited Drone Range](https://umod.org/plugins/limited-drone-range) -- Limits how far RC drones can be controlled from computer stations.

Drone fixes and improvements:
- [Better Drone Collision](https://umod.org/plugins/better-drone-collision) -- Overhauls RC drone collision damage so it's more intuitive.
- [Auto Flip Drones](https://umod.org/plugins/auto-flip-drones) -- Auto flips upside-down RC drones when a player takes control.
- [Drone Hover](https://umod.org/plugins/drone-hover) -- Allows RC drones to hover in place while not being controlled.

Drone attachments:
- [Drone Lights](https://umod.org/plugins/drone-lights) -- Adds controllable search lights to RC drones.
- [Drone Turrets](https://umod.org/plugins/drone-turrets) -- Allows players to deploy auto turrets to RC drones.
- [Drone Storage](https://umod.org/plugins/drone-storage) -- Allows players to deploy a small stash to RC drones.
- [Ridable Drones](https://umod.org/plugins/ridable-drones) -- Allows players to ride RC drones by standing on them or mounting a chair.

## Developer Hooks

You can use standard hooks to prevent Auto Turrets and SAM Sites from targeting drones under specific circumstances.
- `object OnTurretTarget(AutoTurret turret, Drone drone)`
- `object OnSamSiteTarget(SamSite samSite, Drone drone)`
