---
title: Growing plants
parent: Recipes and crafting
grand_parent: Items and crafting
nav_order: 4
---

# Growing plants

This page uses `Vector3` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

`Plants` lists the plants that grow in the scene you are in and tells your mod when they are harvested or picked. Each plant is a `PlantInfo`.





<div class="api-type" markdown="1">
<p class="api-type-head"><code>Plants</code> <span>the plant list and pick events</span></p>

Use it to list the plants in the scene, read the fruit slots of one plant, and hear when a plant is harvested or a fruit is picked.

<div class="api-pair" markdown="1">

```csharp
public static class Plants
{
    public static event Action<PlantHarvestArgs> Harvested;
    public static event Action<PlantPickArgs> Picked;
    public static List<PlantInfo> All();
    public static List<PlantFruitInfo> Fruits(string plantId);
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<PlantInfo>` | Every plant in the active scene. |
| `Fruits(plantId)` | `List<PlantFruitInfo>` | The fruit slots of one plant. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the plant events could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Harvested` | `PlantHarvestArgs` | A plant's harvest was handed out. |
| `Picked` | `PlantPickArgs` | One fruit was picked. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlantHarvestArgs</code> <span>what Harvested gives you</span></p>

`Plants.Harvested` hands you one of these when a plant's harvest is handed out. `Items` lists what the harvest gave.

<div class="api-pair" markdown="1">

```csharp
public sealed class PlantHarvestArgs
{
    public string PlantId { get; }
    public List<ItemCount> Items { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `PlantId` | `string` | The plant that was harvested. |
| `Items` | `List<ItemCount>` | What the harvest gave. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlantPickArgs</code> <span>what Picked gives you</span></p>

`Plants.Picked` hands you one of these for each fruit that is picked, with the fruit's item number and how many.

<div class="api-pair" markdown="1">

```csharp
public sealed class PlantPickArgs
{
    public string PlantId { get; }
    public int ItemId { get; }
    public int Amount { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `PlantId` | `string` | The plant the fruit came from. |
| `ItemId` | `int` | The fruit's item number. |
| `Amount` | `int` | How many. It is the amount the game rolled for the fruit. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlantInfo</code> <span>a snapshot of one plant</span></p>

`Plants.All()` gives you one of these for each plant in the scene. Pass its `Id` to `Plants.Fruits` or to the calls on the [Plant actions page](../plantactions.md).

<div class="api-pair" markdown="1">

```csharp
public sealed class PlantInfo
{
    public string Id { get; }
    public Vector3 Position { get; }
    public int Age { get; }
    public int FullGrownAge { get; }
    public float Progress { get; }
    public bool FullyGrown { get; }
    public bool Paused { get; }
    public bool Rotting { get; }
    public int ReadyFruit { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The plant's id. It can be `null` on a plant that was just planted. |
| `Position` | `Vector3` | Where the plant is, in world coordinates. |
| `Age` | `int` | How old it is, as the game counts. |
| `FullGrownAge` | `int` | The age at which it is fully grown. |
| `Progress` | `float` | How far it has come. It is 0 to 1 while growing, and then it counts up through spoiling. |
| `FullyGrown` | `bool` | `true` when `Age` has reached `FullGrownAge`. |
| `Paused` | `bool` | `true` when its growth is paused. |
| `Rotting` | `bool` | `true` when it is rotting. |
| `ReadyFruit` | `int` | How many visible fruits are ready to pick. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlantFruitInfo</code> <span>a snapshot of one fruit slot on a plant</span></p>

`Plants.Fruits(plantId)` gives you one of these for each fruit slot. Check `Visible` first, because hidden slots are listed too.

<div class="api-pair" markdown="1">

```csharp
public sealed class PlantFruitInfo
{
    public int ItemId { get; }
    public int Amount { get; }
    public bool Visible { get; }
    public bool Ready { get; }
    public bool Spoiled { get; }
    public float GrowProgress { get; }
    public float SpoilProgress { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ItemId` | `int` | The fruit's item number. It is 0 when the fruit has no item. |
| `Amount` | `int` | How many the game rolled for this fruit. |
| `Visible` | `bool` | `true` when the fruit's object is switched on. |
| `Ready` | `bool` | `true` when it is ready to pick. |
| `Spoiled` | `bool` | `true` when it has spoiled. |
| `GrowProgress` | `float` | 0 to 1. How far it has grown. It is 0 when its full age is 0. |
| `SpoilProgress` | `float` | 0 to 1. How far it has spoiled. It is 0 when the value is not a number. |

</div>
</div>

## Things to know

- `All()` lists every growing plant in the scene you are in. That is the plants in growing spots and any others. <span class="tech">(It returns every `grow` object in the active scene. `Id` is `grow.GUID`. `Progress` is `grow.GetCurrentStageProcess()`. `ReadyFruit` counts active fruits with `readyToPick`; hidden animal-meta fruits are not counted.)</span>
- `Harvested` fires when the game hands out a plant's harvest. That covers the player harvesting a ripe plant and a growing station harvesting its spots. It also fires when a dead plant is pulled up, with the plant's leftover items. <span class="tech">(It fires on `grow.HarvestExternal`.)</span>
- `Picked` fires when one fruit is picked. That is a player picking a ripe fruit. It also fires for each ripe fruit that a harvest of a multi-fruit plant picks, so that harvest raises one `Picked` for each ripe fruit and no `Harvested`. <span class="tech">(It fires when `GrowItem.GetItem` hands out a fruit: it returned 0 and the fruit is not spoiled. The harvest case is the loop inside the plant's `HarvestExternal`.)</span>
- A spoiled fruit raises nothing, and seeds are not reported. `Amount` is the number the game rolled for the fruit. It is not checked against free space in the inventory. <span class="tech">(It is the rolled `GrowItem.itemAmount`.)</span>
- `Fruits(plantId)` lists the fruit slots of one plant, including hidden ones. Check `Visible`. It returns an empty list for a `null`, empty or unknown id. It does not read the growth through the game's stage call, because that call can throw an error. <span class="tech">(The stage call is `GetCurrentStageProcess`. `GrowProgress` is age divided by `fullGrownAgeFinal`, and `SpoilProgress` is `currentSpoilTime` divided by `randomSpoilTime`.)</span>
- `IsAvailable` tells you whether `Harvested` and `Picked` work. It is `false` when the framework could not set up the events, or when its feature group was turned off. `All()` and `Fruits()` keep working.

## Limits

- `Harvested` does not fire for a multi-fruit plant picked fruit by fruit. Picking ripe fruit off such a plant hands the items out another way, so the harvest comes back empty and the event is not raised. Use `Picked` for those.
- `Harvested` does not cover plants that are single collectable items, which a growing station picks up another way. It also does not cover pulling up a plant that has not grown up, which destroys the plant without giving any items. <span class="tech">(The first is `CollectibleItem` plants. The second is the immature-plant harvest.)</span>
