---
title: Crafting stations
parent: Recipes and crafting
grand_parent: Items and crafting
nav_order: 3
---

# Crafting stations

A crafting station is a place that carries out recipes. `Crafting.All()` lists the stations in the scene you are in. Each one is a `CraftingStationInfo`. The craft events tell you when a station takes ingredients and when it makes the results.

```csharp
public sealed class CraftingStationInfo    // a snapshot of one crafting station
{
    public string Kind { get; }                 // the game's class name for it: "manufacturing", "manufacturingProcess", "Growing", ...
    public Vector3 Position { get; }            // where the station is, in world coordinates
    public bool Busy { get; }                   // true while the station is working on a craft
    public string Id { get; }                   // the id the scene gives it; can be null or empty; for information only
    public List<string> Modifiers { get; }      // the conditions the station has now (names such as Clean, Makeshift or Industrial)
    public List<string> RecipeTypes { get; }    // the kinds of recipe the station can make
    public bool? HasNaturalLight { get; }       // true when sunlight reaches it; null when that can't be told
}

public sealed class CraftStartArgs    // what CraftStarted tells you
{
    public int RecipeId { get; }                  // the recipe being made
    public CraftingStationInfo Station { get; }   // the station making it
    public int Multiples { get; }                 // how many times over the recipe is made
}

public sealed class CraftedArgs    // what Crafted tells you
{
    public int RecipeId { get; }                  // the recipe that was made
    public CraftingStationInfo Station { get; }   // the station that made it
    public bool Success { get; }                  // true when the craft worked; false when it failed
    public int Multiples { get; }                 // how many times over the recipe was made
    public List<ItemCount> Results { get; }       // what the craft produced
}

public static class Crafting
{
    public static event Action<CraftStartArgs> CraftStarted;   // a station just took the ingredients
    public static event Action<CraftedArgs> Crafted;           // a station just made the results

    public static List<CraftingStationInfo> All();             // every crafting station in the active scene
    public static bool IsAvailable { get; }                    // false when the craft events could not be set up
}
```

- **`CraftStarted` fires once for each craft, when the station takes the ingredients.** It does not fire for price or time previews. Growing stations also raise it when a grow starts, because the game uses the same call.
  - Technical: it fires on `CraftingBase.RemoveItems`. `Growing.StartGrow` uses the same call.
- **`Crafted` fires when the station creates its results.** It covers manufacturing and manufacturing-process stations. Growing stations do not raise it; use `Plants.Harvested` for them.
  - Technical: it fires on `CraftingBase.CreateItems`.
- **`Results` are the recipe's results times `Multiples`.** On a failed craft (`Success` is `false`) they are the failure results instead. They are the amounts the recipe states, not what the game rolled. For a recipe where the game picks one item from a group, `Results` lists the group's entry, not the item the game picked.
- **`Busy` is read at the moment the event fires.** For `CraftStarted` that is before the new craft is queued.
- **`Kind` is the game's class name, so it can change in a future game version.** Compare it with the names above.
- **`Modifiers` and `RecipeTypes` are copies taken when the info object is built, so they do not follow later changes.** Upgrades add to both lists.
  - Technical: they are copied from `ModifiersWithUpgrades` and `RecipesWithUpgrades` as lists of `RecipeCondition` and `RecipeType` names.
- **`HasNaturalLight` is worked out when you read it, not when `All()` runs.** `All()` itself does no sunlight test, and neither do the craft events until you read the value. It is `null` when the station has no sunlight checker, a save is loading, the game's time is paused, the window light is not ready, or the check fails. The game applies its modifiers one frame later, so `HasNaturalLight` and `Modifiers` can briefly disagree.
  - Technical: it is `NaturalLightSourceChecker.HasSun()`. It is `null` when there is no checker in the station's children, a save is loading, `TimeOfDayAzure.updateTimeDisabled` is set, or `WindowNaturalLight` is not initialised.
- **You can't start a craft from code.** The game's only way to craft is driven by the crafting panel on screen, and it reads the panel's choices and updates the panel. A mod can't call it safely without that panel, so there is no `Craft` call.
  - Technical: the game's only craft entry is `manufacturing.Craft()`.
- **You can't read a station's crafting progress or timer.** The game keeps them private, so `All()` gives only `Busy`.
- **`IsAvailable` tells you whether the craft events work.** It is `false` when the framework could not set up the events, or when its feature group was turned off. `All()` and the recipe reads keep working.
