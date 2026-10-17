---
title: Common tasks
parent: Weapons
grand_parent: Player life and skills
nav_order: 1
---

# Common tasks

Make mining twice as effective. The factor is kept only until the next scene load, so set it again after each load.

```csharp
Weapons.SetHitFactor(Mod.Id, HitTarget.Mining, 2f);
```

Give a small hit a chance to land one more point instead of always rounding.

```csharp
Weapons.SetHitFactor(Mod.Id, HitTarget.Chopping, 1.3f, true);
```

Remove every hit factor your mod has set.

```csharp
Weapons.ClearHitFactors(Mod.Id);
```

Count the Batteries D (item 10580) the player carries, and reload the weapon in the right hand.

```csharp
Log.Info("Batteries D: " + Weapons.Ammo(10580));
if (Weapons.Reload()) Log.Info("Reloading.");
```

Turn the flashlight on, if the player holds one.

```csharp
if (Weapons.FlashlightOn() == false) Weapons.SetFlashlightOn(true);
Log.Info("Charge: " + Weapons.FlashlightCharge());
```

Read the osrat.

```csharp
Log.Info("Osrat signal " + Osrats.Strength + ", beeping: " + Osrats.IsBeeping);
```
