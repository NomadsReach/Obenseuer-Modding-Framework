---
title: Autofarms API
parent: API reference
nav_order: 2
---

# Autofarms API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.autofarms`.
Files: `src/Api/Autofarms.cs`, `src/Api/AutofarmsPatches.cs`.

An autofarm is an `Autofarm` machine: it fills its growing slots with plants at set times (its replant times) and when the scene starts. The plants themselves, their age and their harvest belong to `Plants` (`src/Api/GrowingPlants.cs`). This area does not duplicate them.

## If a hook cannot be installed

The framework installs one hook, `Autofarm.Plant`. If the hook fails, the owner is disabled and one warning is logged. The reads and `SetGrowPercentage` keep working, and `Replanted` stops.

## Threading and safety

- Main thread only.
- `Autofarms.IsAvailable` is false when the hook was not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, `All` returns an empty list, `Get` returns `null`, and `SetGrowPercentage` returns `false`.
- Nothing throws except `ArgumentException` for an invalid grow percentage (see below).
- `Replanted` is raised synchronously from the game's own planting call. A handler that throws is logged and the other handlers still run.

## API

```csharp
public static class Autofarms
{
    public static List<AutofarmInfo> All();
    public static AutofarmInfo Get(string id);
    public static bool SetGrowPercentage(string id, int min, int max);

    public static bool IsAvailable { get; }
    public static event Action<ReplantedArgs> Replanted;
}

public sealed class AutofarmInfo
{
    public string Id { get; }                  // save GUID, or the scene path
    public Vector3 Position { get; }           // world position
    public int SlotCount { get; }              // growing slots the machine fills
    public int PlantedCount { get; }           // slots that hold a plant now
    public List<int> PlantItemIds { get; }     // plant item ids it chooses from (ItemReference.ID)
    public bool PlantInOrder { get; }          // true: fills slots in order, false: random choice
    public int GrowPercentageMin { get; }      // lowest starting growth, percent
    public int GrowPercentageMax { get; }      // highest starting growth, percent
    public int OwnerId { get; }                // NPC id of the owner, 0 when none
}

public sealed class ReplantedArgs
{
    public string AutofarmId { get; }
    public int PlantedCount { get; }           // slots filled by this planting
    public bool Initial { get; }               // true for the first planting of a new autofarm
}
```

- `All` lists every autofarm in the active scene.
- `All` and `Get` scan the active scene with `FindObjectsByType` on every call; cache the result.
- `Get(id)` returns one `AutofarmInfo`, or `null` when no autofarm has that id.
- `SetGrowPercentage(id, min, max)` sets the starting growth range for the plants the autofarm plants next. `min` must be `0` to `100`, and `max` must be `min` to `100`, otherwise it throws `ArgumentException`. It returns `false` when no autofarm has that id. It works inside `Events.SceneLoaded` and while a save loads or saves. It changes only future plantings; plants already in the slots keep their growth.
- Plant item ids are the plain `ItemReference.ID` ints, as everywhere in the framework. The reference's `itemnamespace` is not exposed.
- `Replanted` fires when a planting fills at least one empty slot. It fires for the first planting when a new autofarm starts (`Initial` is true; a loaded save has `initDone` set, so it does not plant on start) and for each replant time (`Initial` is false). A planting that finds no empty slot, or no plant prefab, does not fire it.

## Saving

`growPercentageMin` and `growPercentageMax` are `[fsIgnore]` (the game's `Autofarm` class), so they are not saved. A save does not carry them, so after a save is loaded they hold the scene's values again. A mod that calls `SetGrowPercentage` must call it again after the scene loads, for example from `Events.SceneLoaded`.

## Not wrapped

- The replant times (`Autofarm.replantTimes`, a game `WeekdayAndHour` array). They are not exposed in this pass.
- Writes to the plant prefab list (`plantPrefabs`), the owner (`ownerId`), `plantInOrder`, and the growing slots. This pass exposes only the growth range, the plain fields the game reads on every planting (the game's `Autofarm` class).
- Planting or harvesting by hand. `Autofarm.Plant` is private, and the public harvest path is `Plants.Harvested`.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
