---
title: Crafting stations
parent: Recipes and crafting
grand_parent: Items and crafting
nav_order: 3
---

# Crafting stations

A crafting station is a place that carries out recipes. `Crafting.All()` lists the stations in the scene you are in, each one as a `CraftingStationInfo`. The craft events tell you when a station takes ingredients and when it makes the results.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CraftingStationInfo</code> <span>a snapshot of one crafting station</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class CraftingStationInfo
{
    public string Kind { get; }
    public Vector3 Position { get; }
    public bool Busy { get; }
    public string Id { get; }
    public List<string> Modifiers { get; }
    public List<string> RecipeTypes { get; }
    public bool? HasNaturalLight { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Kind` | `string` | The game's class name for it, such as `"manufacturing"`, `"manufacturingProcess"` or `"Growing"`. |
| `Position` | `Vector3` | Where the station is, in world coordinates. |
| `Busy` | `bool` | `true` while the station is working on a craft. |
| `Id` | `string` | The id the scene gives it. It can be `null` or empty, and it is for information only. |
| `Modifiers` | `List<string>` | The conditions the station has now, such as `Clean`, `Makeshift` or `Industrial`. |
| `RecipeTypes` | `List<string>` | The kinds of recipe the station can make. |
| `HasNaturalLight` | `bool?` | `true` when sunlight reaches it. It is `null` when that can't be told. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CraftStartArgs</code> <span>what CraftStarted gives you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class CraftStartArgs
{
    public int RecipeId { get; }
    public CraftingStationInfo Station { get; }
    public int Multiples { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `RecipeId` | `int` | The recipe being made. |
| `Station` | `CraftingStationInfo` | The station making it. |
| `Multiples` | `int` | How many times over the recipe is made. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CraftedArgs</code> <span>what Crafted gives you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class CraftedArgs
{
    public int RecipeId { get; }
    public CraftingStationInfo Station { get; }
    public bool Success { get; }
    public int Multiples { get; }
    public List<ItemCount> Results { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `RecipeId` | `int` | The recipe that was made. |
| `Station` | `CraftingStationInfo` | The station that made it. |
| `Success` | `bool` | `true` when the craft worked. `false` when it failed. |
| `Multiples` | `int` | How many times over the recipe was made. |
| `Results` | `List<ItemCount>` | What the craft produced. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Crafting</code> <span>the station list and craft events</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Crafting
{
    public static event Action<CraftStartArgs> CraftStarted;
    public static event Action<CraftedArgs> Crafted;
    public static List<CraftingStationInfo> All();
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<CraftingStationInfo>` | Every crafting station in the active scene. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the craft events could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `CraftStarted` | `CraftStartArgs` | A station just took the ingredients. |
| `Crafted` | `CraftedArgs` | A station just made the results. |

</div>
</div>

## Things to know

- `CraftStarted` fires once for each craft, when the station takes the ingredients. It does not fire for price or time previews. Growing stations also raise it when a grow starts, because the game uses the same call. <span class="tech">(It fires on `CraftingBase.RemoveItems`. `Growing.StartGrow` uses the same call.)</span>
- `Crafted` fires when the station creates its results. It covers manufacturing and manufacturing-process stations. Growing stations do not raise it; use `Plants.Harvested` for them. <span class="tech">(It fires on `CraftingBase.CreateItems`.)</span>
- `Results` are the recipe's results times `Multiples`. On a failed craft (`Success` is `false`) they are the failure results instead. They are the amounts the recipe states, not what the game rolled.
- For a recipe where the game picks one item from a group, `Results` lists the group's entry, not the item the game picked.
- `Busy` is read at the moment the event fires. For `CraftStarted` that is before the new craft is queued.
- `Kind` is the game's class name, so it can change in a future game version. Compare it with the names above.
- `Modifiers` and `RecipeTypes` are copies taken when the info object is built, so they do not follow later changes. Upgrades add to both lists. <span class="tech">(They are copied from `ModifiersWithUpgrades` and `RecipesWithUpgrades` as lists of `RecipeCondition` and `RecipeType` names.)</span>
- `HasNaturalLight` is worked out when you read it, not when `All()` runs. `All()` itself does no sunlight test, and neither do the craft events until you read the value.
- `HasNaturalLight` is `null` when the station has no sunlight checker, a save is loading, the game's time is paused, the window light is not ready, or the check fails. The game applies its modifiers one frame later, so `HasNaturalLight` and `Modifiers` can briefly disagree. <span class="tech">(It is `NaturalLightSourceChecker.HasSun()`. It is `null` when there is no checker in the station's children, a save is loading, `TimeOfDayAzure.updateTimeDisabled` is set, or `WindowNaturalLight` is not initialised.)</span>
- `IsAvailable` tells you whether the craft events work. It is `false` when the framework could not set up the events, or when its feature group was turned off. `All()` and the recipe reads keep working.

## Limits

- You can't start a craft from code. The game's only way to craft is driven by the crafting panel on screen, and it reads the panel's choices and updates the panel. A mod can't call it safely without that panel, so there is no `Craft` call. <span class="tech">(The game's only craft entry is `manufacturing.Craft()`.)</span>
- You can't read a station's crafting progress or timer. The game keeps them private, so `All()` gives only `Busy`.
