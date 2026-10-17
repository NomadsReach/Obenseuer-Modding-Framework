---
title: Growing plants
parent: Recipes and crafting
grand_parent: Items and crafting
nav_order: 4
---

# Growing plants

`Plants` lists the plants that grow in the scene you are in and tells your mod when they are harvested or picked. Each plant is a `PlantInfo`.

```csharp
public sealed class PlantInfo    // a snapshot of one plant
{
    public string Id { get; }            // the plant's id; can be null on a plant that was just planted
    public Vector3 Position { get; }     // where the plant is, in world coordinates
    public int Age { get; }              // how old it is, as the game counts
    public int FullGrownAge { get; }     // the age at which it is fully grown
    public float Progress { get; }       // how far it has come: 0 to 1 while growing, and then it counts up through spoiling
    public bool FullyGrown { get; }      // true when Age has reached FullGrownAge
    public bool Paused { get; }          // true when its growth is paused
    public bool Rotting { get; }         // true when it is rotting
    public int ReadyFruit { get; }       // how many visible fruits are ready to pick
}

public sealed class PlantFruitInfo    // a snapshot of one fruit slot on a plant
{
    public int ItemId { get; }           // the fruit's item number; 0 when the fruit has no item
    public int Amount { get; }           // how many the game rolled for this fruit
    public bool Visible { get; }         // true when the fruit's object is switched on
    public bool Ready { get; }           // true when it is ready to pick
    public bool Spoiled { get; }         // true when it has spoiled
    public float GrowProgress { get; }   // 0 to 1; how far it has grown; 0 when its full age is 0
    public float SpoilProgress { get; }  // 0 to 1; how far it has spoiled; 0 when the value is not a number
}

public sealed class PlantHarvestArgs    // what Harvested tells you
{
    public string PlantId { get; }             // the plant that was harvested
    public List<ItemCount> Items { get; }      // what the harvest gave
}

public sealed class PlantPickArgs    // what Picked tells you
{
    public string PlantId { get; }    // the plant the fruit came from
    public int ItemId { get; }        // the fruit's item number
    public int Amount { get; }        // how many; the amount the game rolled for the fruit
}

public static class Plants
{
    public static event Action<PlantHarvestArgs> Harvested;   // a plant's harvest was handed out
    public static event Action<PlantPickArgs> Picked;         // one fruit was picked

    public static List<PlantInfo> All();                       // every plant in the active scene
    public static List<PlantFruitInfo> Fruits(string plantId); // the fruit slots of one plant
    public static bool IsAvailable { get; }                    // false when the plant events could not be set up
}
```

- **`All()` lists every growing plant in the scene you are in.** That is the plants in growing spots and any others.
  - Technical: it returns every `grow` object in the active scene. `Id` is `grow.GUID`. `Progress` is `grow.GetCurrentStageProcess()`. `ReadyFruit` counts active fruits with `readyToPick`; hidden animal-meta fruits are not counted.
- **`Harvested` fires when the game hands out a plant's harvest.** That covers the player harvesting a ripe plant and a growing station harvesting its spots. It also fires when a dead plant is pulled up, with the plant's leftover items.
  - Technical: it fires on `grow.HarvestExternal`.
- **`Harvested` does not fire for a multi-fruit plant picked fruit by fruit.** Picking ripe fruit off such a plant hands the items out another way, so the harvest comes back empty and the event is not raised. Use `Picked` for those.
- **`Harvested` does not cover two cases.** It does not cover plants that are single collectable items, which a growing station picks up another way. It does not cover pulling up a plant that has not grown up, which destroys the plant without giving any items.
  - Technical: the first is `CollectibleItem` plants. The second is the immature-plant harvest.
- **`Picked` fires when one fruit is picked.** That is a player picking a ripe fruit. It also fires for each ripe fruit that a harvest of a multi-fruit plant picks, so that harvest raises one `Picked` for each ripe fruit and no `Harvested`.
  - Technical: it fires when `GrowItem.GetItem` hands out a fruit: it returned 0 and the fruit is not spoiled. The harvest case is the loop inside the plant's `HarvestExternal`.
- **A spoiled fruit raises nothing, and seeds are not reported.** `Amount` is the number the game rolled for the fruit. It is not checked against free space in the inventory.
  - Technical: it is the rolled `GrowItem.itemAmount`.
- **`Fruits(plantId)` lists the fruit slots of one plant, including hidden ones.** Check `Visible`. It returns an empty list for a `null`, empty or unknown id. It does not read the growth through the game's stage call, because that call can throw an error.
  - Technical: the stage call is `GetCurrentStageProcess`. `GrowProgress` is age divided by `fullGrownAgeFinal`, and `SpoilProgress` is `currentSpoilTime` divided by `randomSpoilTime`.
- **`IsAvailable` tells you whether `Harvested` and `Picked` work.** It is `false` when the framework could not set up the events, or when its feature group was turned off. `All()` and `Fruits()` keep working.
