---
title: Growing stations API
parent: API reference
nav_order: 99
---

# Growing stations API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.growingstations`.
Files: `src/Api/GrowingStations.cs`.

A growing station is the `Growing` crafting machine. It holds growing spots, and each spot holds one plant. This area reads the stations and their occupied spots, and sets the station-wide pause and rot. Plant age and harvest stay in `PlantActions` and `Plants` (`docs/api/plantactions.md`, `docs/api/recipes.md`). Station ids are the ids `GrowingStations.All` reports.

## API

```csharp
public static class GrowingStations
{
    public static List<GrowingStationInfo> All();
    public static GrowingStationInfo Get(string id);
    public static bool SetPaused(string modId, string id, bool paused);
    public static bool SetRotting(string modId, string id, bool rotting);
}

public sealed class GrowingStationInfo
{
    public string Id { get; }               // save GUID, or the scene path
    public Vector3 Position { get; }        // world position
    public int SlotCount { get; }           // growing spots the station has
    public bool Paused { get; }             // the station's paused flag
    public bool ManualHarvest { get; }      // true: the player harvests by clicking a plant
    public List<GrowingSlotInfo> Slots { get; }   // the occupied spots only
}

public sealed class GrowingSlotInfo
{
    public string SpotId { get; }           // save GUID, or the scene path
    public string PlantId { get; }          // grow.GUID for a grow plant, null otherwise
    public bool Collectible { get; }        // true for a plant with a CollectibleItem and no grow component
}
```

- `All` lists every `Growing` station in the active scene. It returns an empty list when the active scene has none.
- `Get(id)` returns one station, or `null` when no station has that id.
- `Slots` lists the spots that hold a plant. An empty spot is not listed. `SlotCount` is the total number of spots.
- `PlantId` is the id `Plants.All` reports for the same plant, so a grow plant can be read with `Plants.Fruits` and changed with `PlantActions`.
- `Collectible` is true for a plant that has no `grow` component and has a `CollectibleItem`. Such a plant is picked with `PlantActions.HarvestSpot(modId, SpotId)`. It has no age or growth.
- A plant with neither a `grow` component nor a `CollectibleItem` has `PlantId` null and `Collectible` false. The facade does not act on it.
- `SetPaused` pauses or unpauses the station, as the game does: every grow plant in the station gets `Paused` set (and `GrowOnlyDuringDay` when the station has the sun modifier). It returns `true` when the station's paused flag matches `paused`, and `false` when no station has the id or a save is loading or saving (`SaveData.Busy`). A later pause or unpause by the player or by the station overwrites it.
- `SetRotting` starts or stops the rot of every grow plant in the station (`Rotting`). It returns `false` when no station has the id, or while a save loads or saves. It returns `true` otherwise.
- A station pause and rot survive a save. The game saves the station's `paused` flag, and each plant's `Paused` and `Rotting`, so a loaded save keeps what was set. The mod does not need to set them again after a load.
- None of the members here depends on the breeding hooks. `Breeding.Bred` uses `Growing.PlantExternal` under its own owner, see `docs/api/breeding.md`.
- `modId` is checked with the same rule as the other facades. Two mods can call the setters without conflict. The last call wins.

## Example

```csharp
foreach (GrowingStationInfo station in GrowingStations.All())
{
    foreach (GrowingSlotInfo slot in station.Slots)
    {
        if (slot.Collectible) PlantActions.HarvestSpot("my.mod", slot.SpotId);
    }
}
```

## Not covered

- Station rot and pause are station-wide. For one plant, use `PlantActions.SetRotting` for rot and `PlantActions.Pause` for pause.
- The station's recipes, its output storage and its growing panel. See `docs/api/recipes.md` and `docs/api/craftingsources.md`.
- Autofarms and breeders that have a `Growing` component are growing stations too, and `All` lists them. Their replant times and breeding events stay in `Autofarms` and `Breeding`. An autofarm with no `Growing` component plants on plain transforms, not on growing spots, so its plants are not in `Slots`; read them with `Plants.All`.
