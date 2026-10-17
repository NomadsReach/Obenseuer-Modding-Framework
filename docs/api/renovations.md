---
title: Renovations
parent: Places and travel
grand_parent: API reference
has_children: true
nav_order: 9
---

# Renovations

## What it is

The player's building can be upgraded by contractors: new electricity, water, heating, walls, a kitchen and more. Some upgrades are for the whole building and some are for one apartment. Each upgrade is called a renovation in this API. It lets your mod list the renovations, see what state each is in, read the price and the build progress, order one, and get told when one is finished. Renovation cost and build-time tweaks are in `TweaksRenovations`. The rules for the building and its tenants are on the [Housing page](housing.md).

## When to use it

- Show which renovations the player can order and what they cost.
- Show how far an ordered renovation has got.
- Run code when a renovation is finished.
- Order a renovation for the player from your mod, while the contractor menu is open.

## Quick example

This mod writes a log line when a renovation is finished. Once a save is loaded, it also logs the renovations the player can order now, with their price.

```csharp
using ObenseuerCore;

[ModInfo("example.renowatch", "Renovation Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class RenoWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Renovations.Finished += info => Log.Info(info.Name + " is finished (apartment " + info.ApartmentId + ").");
        Events.WhenReady(() =>
        {
            foreach (RenovationInfo info in Renovations.All())
            {
                if (info.State == RenovationState.Available) Log.Info(info.Name + " can be ordered for " + info.Cost + " OC.");
            }
        });
    }
}
```

`Finished` is an event: the framework calls your code each time it happens. OC is Open Sewer Coins, the game's everyday money, and RM is Real Money, its second currency (see the glossary entries [OC](../glossary.md#oc) and [RM](../glossary.md#rm)). See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.
