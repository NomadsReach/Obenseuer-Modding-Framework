---
title: Weapons
parent: Player life and skills
grand_parent: API reference
has_children: true
nav_order: 10
---

# Weapons

Status: experimental.

## What it is

This API is about the things the player holds in their hands. It lets your mod see which weapon is held in each hand and how much ammo it has, reload the held weapon, and get told when a Detonator goes off, when a melee swing starts and when a melee hit lands. It can make melee hits stronger or weaker for each kind of target (rocks, trees, breakable things and animal props). It also reads and switches the flashlight, and reads the osrat, a hand-held signal detector.

The game has no guns the player can hold, so there is no gun in this API. See "What you can't do (yet)".

## When to use it

- Log or react when the player swings a melee weapon or hits something with it.
- Make mining, chopping or breaking things easier or harder.
- Check what the player holds, and how much ammo it has left.
- Turn the flashlight on or off from your own code.
- Read the osrat's signal strength.

## Quick example

This mod writes a log line for every melee swing, and for every hit with the damage before and after any change. Once a save is loaded, it also logs what the player holds in the right hand.

```csharp
using ObenseuerCore;

[ModInfo("example.weaponwatch", "Weapon Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class WeaponWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Weapons.MeleeSwung += weapon => Log.Info("Swing with item " + weapon.ItemId);
        Weapons.Hit += hit => Log.Info(hit.Target + " hit for " + hit.BaseDamage + ", final " + hit.FinalDamage);
        Events.WhenReady(() =>
        {
            WeaponInfo held = Weapons.Equipped();
            Log.Info(held == null ? "Nothing in the right hand." : "Right hand: item " + held.ItemId + ", " + held.Kind);
        });
    }
}
```

`MeleeSwung` and `Hit` are events: the framework calls your code each time they happen. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.
