---
title: Growing stations
parent: Animals and plants
grand_parent: API reference
nav_order: 5
---

# Growing stations

This page uses `Vector3` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

## What it is

A growing station is a machine in the game that grows plants. It has several growing spots, and each spot holds one plant. This API lets your mod list the stations in the scene, see which spots hold a plant, and pause or rot all the plants in a station at once.

Looking at one plant's age, or harvesting it, is on other pages: [Plant actions](plantactions.md) and [Recipes, crafting and growing](recipes.md).

## When to use it

- Find the growing stations in the scene the player is in, and count how many spots are used.
- Pause a whole station so none of its plants grow for a while, and start it again later.
- Make every plant in a station rot, or stop them rotting.
- Find the plants that the player picks up by hand, and harvest them from your mod.

## Quick example

This mod writes every growing station in the scene to the log, with how many of its spots are taken. It runs when a scene finishes loading.

```csharp
using ObenseuerCore;

[ModInfo("example.growlist", "Grow List", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class GrowList : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.SceneLoaded += scene =>
        {
            foreach (GrowingStationInfo station in GrowingStations.All())
                Log.Info(station.Id + " has " + station.Slots.Count + " of " + station.SlotCount + " spots in use.");
        };
    }
}
```

`SceneLoaded` is an event: the framework calls your code each time a scene finishes loading. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

Pause every growing station, so no plant in the scene grows, and then let them grow again.

```csharp
foreach (GrowingStationInfo station in GrowingStations.All())
    GrowingStations.SetPaused(Mod.Id, station.Id, true);

foreach (GrowingStationInfo station in GrowingStations.All())
    GrowingStations.SetPaused(Mod.Id, station.Id, false);
```

Make the plants in one station rot.

```csharp
foreach (GrowingStationInfo station in GrowingStations.All())
{
    GrowingStations.SetRotting(Mod.Id, station.Id, true);
    break;
}
```

Harvest every plant that is picked up by hand.

```csharp
foreach (GrowingStationInfo station in GrowingStations.All())
{
    foreach (GrowingSlotInfo slot in station.Slots)
    {
        if (slot.Collectible) PlantActions.HarvestSpot(Mod.Id, slot.SpotId);
    }
}
```

Look up one station by id. `Get` returns null when no station has that id.

```csharp
GrowingStationInfo found = GrowingStations.Get("some-station-id");
if (found != null) Log.Info("Paused: " + found.Paused);
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail you can skip on a first read.

### Stations and spots

`GrowingStations` lists the stations as `GrowingStationInfo` snapshots, copies taken when you asked. The snapshot does not update by itself, so ask again for fresh values. A `modId` is your mod's id (see [Owner](../glossary.md#owner) in the glossary).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>GrowingStations</code> <span>list and change growing stations</span></p>

This is the class you start from. `All()` and `Get(id)` list and find stations, and `SetPaused` and `SetRotting` act on a whole station.

<div class="api-pair" markdown="1">

```csharp
public static class GrowingStations
{
    public static List<GrowingStationInfo> All();
    public static GrowingStationInfo Get(string id);
    public static bool SetPaused(string modId, string id, bool paused);
    public static bool SetRotting(string modId, string id, bool rotting);
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<GrowingStationInfo>` | Every growing station in the scene the player is in. |
| `Get(id)` | `GrowingStationInfo` | The station with this id. `null` when none has it. |
| `SetPaused(modId, id, paused)` | `bool` | Pauses or unpauses a whole station. |
| `SetRotting(modId, id, rotting)` | `bool` | Starts or stops rot in a whole station. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>GrowingStationInfo</code> <span>a snapshot of one station</span></p>

`All()` and `Get(id)` give you this, one per station. It is a copy of the station when you asked, so ask again for a current `Paused`. <span class="tech">(It is filled from the game's `Growing` component on each station.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class GrowingStationInfo
{
    public string Id { get; }
    public Vector3 Position { get; }
    public int SlotCount { get; }
    public bool Paused { get; }
    public bool ManualHarvest { get; }
    public List<GrowingSlotInfo> Slots { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The station's id. The number the game saves it under, or a path through the scene. |
| `Position` | `Vector3` | Where the station is, in world coordinates. |
| `SlotCount` | `int` | How many growing spots the station has in total. |
| `Paused` | `bool` | `true` when the station is paused. |
| `ManualHarvest` | `bool` | `true` when the player harvests by clicking a plant. |
| `Slots` | `List<GrowingSlotInfo>` | The spots that hold a plant. Empty spots are not listed. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>GrowingSlotInfo</code> <span>one growing spot that holds a plant</span></p>

`Slots` on a `GrowingStationInfo` gives you one of these for each spot that holds a plant. When `Collectible` is `true`, pass its `SpotId` to `PlantActions.HarvestSpot` to harvest the plant.

<div class="api-pair" markdown="1">

```csharp
public sealed class GrowingSlotInfo
{
    public string SpotId { get; }
    public string PlantId { get; }
    public bool Collectible { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `SpotId` | `string` | The spot's id. Use it to harvest a hand-picked plant. |
| `PlantId` | `string` | The plant's id. `null` when the plant has no growth data. |
| `Collectible` | `bool` | `true` for a plant that is picked up by hand and does not grow. |

</div>
</div>

#### Things to know

- `All()` lists every growing station in the scene the player is in. It gives an empty list when the scene has none. <span class="tech">(it searches the active scene with `FindObjectsByType` for `Growing` components. A station's id is its `GUID` or the GUID of the `SavableScript` on its object, or else a path counted from the scene root.)</span>
- `Get(id)` gives one station, or `null` when no station has that id.
- `Slots` only holds spots that have a plant. An empty spot is not listed, so use `SlotCount` for the total number of spots.
- `PlantId` is the same id the plant pages use. You can pass it to the plant calls to read the plant's fruit or change the plant. See [Plant actions](plantactions.md) and [Recipes, crafting and growing](recipes.md). <span class="tech">(it is the id `Plants.All` reports for the same plant, so a grow plant can be read with `Plants.Fruits` and changed with `PlantActions`. It is the `GUID` of the plant's `grow` component.)</span>
- `Collectible` is `true` for a plant that is picked up by hand. Such a plant does not grow and has no age. Harvest it with `PlantActions.HarvestSpot(modId, slot.SpotId)`. <span class="tech">(a plant with no `grow` component and with a `CollectibleItem`.)</span>
- A plant that is neither growing nor picked up by hand has a `PlantId` of `null` and `Collectible` of `false`. The framework does not act on it. <span class="tech">(it has neither a `grow` component nor a `CollectibleItem`.)</span>
- `SetPaused` pauses or unpauses the whole station, the same way the game does. Every growing plant in the station is paused. If the station has a sun modifier, its plants only grow during the day. It returns `true` when the station's paused state is now what you asked for. <span class="tech">(every grow plant gets `Paused` set, and `GrowOnlyDuringDay` when the station has the sun modifier.)</span>
- `SetRotting` starts or stops rot in every growing plant in the station. It returns `true` when the station was found and the call was made. <span class="tech">(it sets `Rotting` on every grow plant in the station.)</span>
- A station's pause and rot are saved with the game. A loaded save keeps what was set, so your mod does not need to set them again after a load. <span class="tech">(the game saves the station's `paused` flag, and each plant's `Paused` and `Rotting`.)</span>
- Two mods can call the setters without clashing, and the last call wins. The station is paused or rotting as the most recent call left it. <span class="tech">(`modId` is checked with the same rule as the other calls that take one, and an invalid one throws an `ArgumentException`.)</span>
- None of this needs the breeding changes. Breeding is on its own page, [Breeding](breeding.md). <span class="tech">(`Breeding.Bred` uses `Growing.PlantExternal` under its own feature group.)</span>

#### Limits

- `SetPaused` returns `false` when no station has that id, and while a save is loading or being written. A later pause or unpause by the player or by the station overwrites yours. <span class="tech">(the save check is `SaveData.Busy`.)</span>
- `SetRotting` returns `false` when no station has that id, and while a save is loading or being written.

### What you can't do (yet)

- You can't pause or rot one plant from here. The pause and rot here cover the whole station. For one plant, use `PlantActions.Pause` and `PlantActions.SetRotting`.
- You can't read or change the station's recipes, its output storage or its growing panel here. They are on the [Recipes, crafting and growing](recipes.md) and [Crafting sources](craftingsources.md) pages.
- Autofarms and breeders that are built on a growing station are listed here too, but their own features are elsewhere. Their replanting times and breeding events are in [Autofarms](autofarms.md) and [Breeding](breeding.md). An autofarm that is not a growing station plants on plain positions, not on growing spots, so its plants are not in `Slots`. Read them with `Plants.All`. <span class="tech">(an autofarm with no `Growing` component plants on plain transforms.)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log name: `obenseuer.moddingframework.growingstations`.

Everything not listed in this reference is internal and may change without notice.

### When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call at any time. If there is nothing to read, you get an empty list, `null` or `false`. The two setters do nothing and return `false` while a save is loading or being written.

### If something fails to set up

This page changes none of the game's methods, so there is nothing to set up. It has no `IsAvailable` and no feature group that can be turned off. If the game fails while the framework reads, you get an empty list, `null` or `false`, and the first failure goes to the log.
