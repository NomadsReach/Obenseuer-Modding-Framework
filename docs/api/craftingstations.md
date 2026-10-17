---
title: Crafting stations
parent: Items and crafting
grand_parent: API reference
has_children: true
nav_order: 2
---

# Crafting stations

Status: experimental.

## What it is

A crafting station is any place in the game where the player crafts, processes or grows things. This page covers three small parts of the API for them:

- `CraftingSpeed` makes stations take more or less time to finish a craft.
- `CraftingQuality` makes what stations produce better or worse.
- `CraftingStations` starts, stops, pauses and resumes a station from your mod.

Each station has an id. You get the ids from `Crafting.All()` (see [Recipes, crafting and growing](recipes.md)), where each station is a `CraftingStationInfo` with an `Id`.

## When to use it

- Make all crafting faster or slower as a game rule, such as a "slow crafting" or "quick crafting" mod.
- Make everything the player crafts higher or lower quality.
- Start a craft at a station from your mod, for example as part of an automation or a quest.
- Freeze a timed batch for a moment and let it carry on later.

## Quick example

This mod makes every crafting station work twice as fast. The speed is set again every time a scene loads, because the game clears it then.

```csharp
using ObenseuerCore;

[ModInfo("example.fastcraft", "Fast Craft", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class FastCraft : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.SceneLoaded += scene =>
        {
            if (!CraftingSpeed.SetSpeedFactor(Mod.Id, CraftingSpeed.AllStations, 2f))
                Log.Warn("Could not set the crafting speed.");
        };
    }
}
```

`SceneLoaded` is an event: the framework calls your code each time a scene finishes loading. The first argument to `SetSpeedFactor` is your mod's id. The framework uses it to keep your change apart from other mods' changes. See [Using events](../guides/getting-started/using-events.md).
