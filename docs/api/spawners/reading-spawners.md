---
title: Reading spawners
parent: Spawners
grand_parent: Objects and devices
nav_order: 2
---

# Reading spawners

This is the full list of what the API gives you, one section for each part. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

Each spawner is a `SpawnerInfo`, a snapshot of its state at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Spawners</code> <span>find spawners in the scene</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Spawners
{
    public static List<SpawnerInfo> All();
    public static SpawnerInfo Get(string id);
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<SpawnerInfo>` | Every spawner in the scene you are in. |
| `Get(string id)` | `SpawnerInfo` | The spawner with this id. `null` when there is none. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the `Spawned` event could not be set up. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SpawnerInfo</code> <span>a snapshot of one spawner</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class SpawnerInfo
{
    public string Id { get; }
    public string TypeName { get; }
    public bool RespawnEnabled { get; }
    public float Chance { get; }
    public int SpawnedCount { get; }
    public RespawnTime SpawnTime { get; }
    public bool UseCustomSpawnTime { get; }
    public int MinRespawnTime { get; }
    public int MaxRespawnTime { get; }
    public int SecondsToSpawn { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | Names this spawner. See the first note below. |
| `TypeName` | `string` | What kind of spawner it is, for example `CollectibleItemSpawner`. |
| `RespawnEnabled` | `bool` | `true` when the spawner respawns things. |
| `Chance` | `float` | The chance of a spawn each time, from 0 to 1. |
| `SpawnedCount` | `int` | How many things it has spawned that still exist. |
| `SpawnTime` | `RespawnTime` | The wait-time preset it uses. |
| `UseCustomSpawnTime` | `bool` | `true` when it uses `MinRespawnTime` and `MaxRespawnTime`. |
| `MinRespawnTime` | `int` | The shortest custom wait, in game seconds. |
| `MaxRespawnTime` | `int` | The longest custom wait, in game seconds. |
| `SecondsToSpawn` | `int` | The game's countdown to the next spawn, in seconds. You can only read it. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RespawnTime</code> <span>the wait-time presets</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum RespawnTime
{
    Default,
    VeryShort,
    Short,
    Long,
    VeryLong,
    EveryDay
}
```

| Value | Meaning |
|---|---|
| `Default` | A random wait of 22 to 25 game hours. |
| `VeryShort` | A random wait of 1 to 2 game hours. |
| `Short` | A random wait of 5 to 7 game hours. |
| `Long` | A random wait of 48 to 72 game hours. |
| `VeryLong` | A random wait of 144 to 192 game hours. |
| `EveryDay` | Respawns once each game day. |

</div>
</div>

## Things to know

- A spawner's `Id` names one spawner in the scene you are in. If the game gave the spawner a save id, that is the id. If not, the id is the spawner's place in the scene, and it changes if someone edits the objects around it. <span class="tech">(the id is the object's GUID when it has one, otherwise a scene path such as `Scene:Root[0]/Child[2]`.)</span>
- `All()` and `Get` only see the scene you are in. Spawners that are switched off, and spawners in other scenes, are not listed. A null or unknown id finds nothing. <span class="tech">(both use `FindObjectsByType`, which skips inactive objects.)</span>
- `RespawnTime` is a preset wait, and the game then adjusts it for its difficulty setting. The ranges in the table above are the wait before that adjustment, so the real wait can be longer or shorter. A custom wait (`UseCustomSpawnTime` set to `true`) is a random number of game seconds between your minimum and maximum, and the difficulty setting does not change it. <span class="tech">(`SpawnSettings.SetSpawnTime` passes a preset's wait through `currentDifficultyInfo.GetRespawningTime`. The custom wait is `Random.Range(minRespawnTime, maxRespawnTime)` with no scaling. `EveryDay` stores the current day (`daySpawned`) instead of a countdown. The framework does not change that scaling.)</span>
- `MinRespawnTime` and `MaxRespawnTime` are used only when `UseCustomSpawnTime` is `true`. That is how the game uses them as well.

## Limits

- `SpawnedCount` counts the things the spawner has made that still exist. If a game update renames the field the framework reads it from, the count shows `0`. <span class="tech">(it counts the non-null entries of the protected `objects` list, read through reflection.)</span>
