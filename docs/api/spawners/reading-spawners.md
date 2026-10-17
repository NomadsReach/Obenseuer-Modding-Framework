---
title: Reading spawners
parent: Spawners
grand_parent: Objects and devices
nav_order: 2
---

# Reading spawners

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

Each spawner is a `SpawnerInfo`, a snapshot of its state at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public static class Spawners
{
    public static List<SpawnerInfo> All();       // every spawner in the scene you are in
    public static SpawnerInfo Get(string id);    // the spawner with this id; null when there is none

    public static bool IsAvailable { get; }     // false when the Spawned event could not be set up
}

public sealed class SpawnerInfo    // a snapshot of one spawner
{
    public string Id { get; }                    // names this spawner; see the first bullet below
    public string TypeName { get; }              // what kind of spawner it is, for example "CollectibleItemSpawner"
    public bool RespawnEnabled { get; }          // true when the spawner respawns things
    public float Chance { get; }                 // the chance of a spawn each time, from 0 to 1
    public int SpawnedCount { get; }             // how many things it has spawned that still exist
    public RespawnTime SpawnTime { get; }        // the wait-time preset it uses
    public bool UseCustomSpawnTime { get; }      // true when it uses MinRespawnTime and MaxRespawnTime
    public int MinRespawnTime { get; }           // the shortest custom wait, in game seconds
    public int MaxRespawnTime { get; }           // the longest custom wait, in game seconds
    public int SecondsToSpawn { get; }           // the game's countdown to the next spawn, in seconds; you can only read it
}

public enum RespawnTime
{
    Default,     // a random wait of 22 to 25 game hours
    VeryShort,   // a random wait of 1 to 2 game hours
    Short,       // a random wait of 5 to 7 game hours
    Long,        // a random wait of 48 to 72 game hours
    VeryLong,    // a random wait of 144 to 192 game hours
    EveryDay     // respawns once each game day
}
```

- **A spawner's `Id` names one spawner in the scene you are in.** If the game gave the spawner a save id, that is the id. If not, the id is the spawner's place in the scene, and it changes if someone edits the objects around it.
  - Technical: the id is the object's GUID when it has one, otherwise a scene path such as `Scene:Root[0]/Child[2]`.
- **`All()` and `Get` only see the scene you are in.** Spawners that are switched off, and spawners in other scenes, are not listed. A null or unknown id finds nothing.
  - Technical: both use `FindObjectsByType`, which skips inactive objects.
- **`SpawnedCount` counts the things the spawner has made that still exist.** If a game update renames the field the framework reads it from, the count shows `0`.
  - Technical: it counts the non-null entries of the protected `objects` list, read through reflection.
- **`RespawnTime` is a preset wait, and the game then adjusts it for its difficulty setting.** The ranges in the code block above are the wait before that adjustment, so the real wait can be longer or shorter. A custom wait (`UseCustomSpawnTime` set to `true`) is a random number of game seconds between your minimum and maximum, and the difficulty setting does not change it.
  - Technical: `SpawnSettings.SetSpawnTime` passes a preset's wait through `currentDifficultyInfo.GetRespawningTime`. The custom wait is `Random.Range(minRespawnTime, maxRespawnTime)` with no scaling. `EveryDay` stores the current day (`daySpawned`) instead of a countdown. The framework does not change that scaling.
- **`MinRespawnTime` and `MaxRespawnTime` are used only when `UseCustomSpawnTime` is `true`.** That is how the game uses them as well.
