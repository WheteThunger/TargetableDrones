## Features

- Allows RC drones to be targeted by Auto Turrets and SAM Sites
- Allows sharing authorization with team, clan and friends
- Side effect: Allows drones to land on the cargo ship

## Configuration

Default configuration:

```json
{
  "EnableTurretTargeting": true,
  "EnableSAMTargeting": true,
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
- `DefaultSharingSettings` (each `true` or `false`) -- These settings determine whether a player's Auto Turrets or SAM Sites will target drones deployed by their teammates, friends, clanmates, or ally clanmates.

## FAQ

#### How do I get a drone?

As of this writing, RC drones are a deployable item named `drone`, but they do not appear naturally in any loot table, nor are they craftable. However, since they are simply an item, you can use plugins to add them to loot tables, kits, GUI shops, etc. Admins can also get them with the command `inventory.give drone 1`, or spawn one in directly with `spawn drone.deployed`.

#### How do I remote-control a drone?

If a player has building privilege, they can pull out a hammer and set the ID of the drone. They can then enter that ID at a computer station and select it to start controlling the drone. Controls are `W`/`A`/`S`/`D` to move, `shift` (sprint) to go up, `ctrl` (duck) to go down, and mouse to steer.

Note: If you are unable to steer the drone, that is likely because you have a plugin drawing a UI that is grabbing the mouse cursor. The Movable CCTV was previously guilty of this and was patched in March 2021.

## Recommended compatible plugins

- [Drone Hover](https://umod.org/plugins/drone-hover) -- Allows RC drones to hover in place while not being controlled.
- [Drone Lights](https://umod.org/plugins/drone-lights) -- Adds controllable search lights to RC drones.
- [Drone Storage](https://umod.org/plugins/drone-storage) -- Allows players to deploy a small stash to RC drones.
- [Drone Turrets](https://umod.org/plugins/drone-turrets) -- Allows players to deploy auto turrets to RC drones.
- [Drone Effects](https://umod.org/plugins/drone-effects) -- Adds collision effects and propeller animations to RC drones.
- [RC Identifier Fix](https://umod.org/plugins/rc-identifier-fix) -- Auto updates RC identifiers saved in computer stations to refer to the correct entity.

## Developer Hooks

You can use standard hooks to prevent Auto Turrets and SAM Sites from targeting drones under specific circumstances.
- `bool? OnTurretTarget(AutoTurret turret, Drone drone)`
- `bool? OnSamSiteTarget(SamSite samSite, Drone drone)`

#### OnDroneTargetableBecome

```csharp
bool? OnDroneTargetableBecome(Drone drone)
```

- Called when a drone is about to become targetable by Auto Turrets and SAM Sites
- Returning `false` will prevent the drone from becoming targetable
- Returning `null` will result in the default behavior
