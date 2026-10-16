---
title: Spawners API
parent: API reference
nav_order: 76
---

# Spawners API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.spawners`.
Files: `src/Api/Spawners.cs`, `src/Api/SpawnersPatches.cs`.

This area wraps the `Spawner` family: scripts that place items, furniture, blueprints or characters in a scene, and respawn them on a timer. `Spawner` is the base class, so `All` also lists its subclasses (`CollectibleItemSpawner`, `AnimalSpawner`, `PrefabSpawner`, `FurnitureBlueprintSpawner`, `CharacterSpawner`). The spawn itself is still the game's code.

## If a hook cannot be installed

The framework installs one hook, a postfix on `Spawner.TrySpawn(bool)`. If it fails, the owner is disabled and `Spawners.IsAvailable` is `false`. The reads, `SetRespawn` and `TriggerNow` keep working. `Spawned` stops.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no save loaded, reads return an empty list or `null`, and writers return `false`.
- Nothing throws. A null or unknown `id` finds nothing.
- Writers return `false` while `SaveData.Busy` is true.

## API

```csharp
public static class Spawners
{
    public static List<SpawnerInfo> All();
    public static SpawnerInfo Get(string id);
    public static bool SetRespawn(string id, bool enabled, float chance);
    public static bool SetRespawnTiming(string id, RespawnTime time, bool useCustomSpawnTime, int minSeconds, int maxSeconds);
    public static bool TriggerNow(string id);

    public static event Action<SpawnerArgs> Spawned;

    public static bool IsAvailable { get; }
}

public sealed class SpawnerInfo
{
    public string Id { get; }            // the save GUID, or the scene path
    public string TypeName { get; }      // the runtime type, for example "CollectibleItemSpawner"
    public bool RespawnEnabled { get; }  // spawnSettings.respawnEnabled
    public float Chance { get; }         // spawnSettings.spawnChance, 0 to 1
    public int SpawnedCount { get; }     // live spawned objects the spawner tracks
    public RespawnTime SpawnTime { get; }        // spawnSettings.spawnTime
    public bool UseCustomSpawnTime { get; }      // spawnSettings.useCustomSpawnTime
    public int MinRespawnTime { get; }           // spawnSettings.minRespawnTime, in game seconds
    public int MaxRespawnTime { get; }           // spawnSettings.maxRespawnTime, in game seconds
    public int SecondsToSpawn { get; }           // spawnSettings.timetospawn, the game's countdown, read only
}

public enum RespawnTime { Default, VeryShort, Short, Long, VeryLong, EveryDay }

public sealed class SpawnerArgs
{
    public string Id { get; }
    public string TypeName { get; }
    public int SpawnedCount { get; }     // the count after the spawn
}
```

```csharp
using System;
using System.Collections.Generic;
using ObenseuerCore;

public static class SpawnersExample
{
    public static void Run()
    {
        List<SpawnerInfo> all = Spawners.All();
        SpawnerInfo one = Spawners.Get("some-id");
        bool set = one != null && Spawners.SetRespawn(one.Id, true, 0.5f);
        bool spawned = Spawners.TriggerNow("some-id");
        Spawners.Spawned += delegate(SpawnerArgs args) { Console.WriteLine(args.TypeName + " " + args.SpawnedCount); };
    }
}
```

## Behaviour

- `SetRespawn(id, enabled, chance)` sets `spawnSettings.respawnEnabled` and `spawnSettings.spawnChance`. The chance is clamped to 0 to 1. It returns `true` when the values were written.
- Enabling respawn at runtime is not enough by itself. The game starts the respawn timer only when the spawner starts with respawn on. A spawner that started with respawn off gets the new flag, but its timer does not run until its scene loads again.
- `SetRespawnTiming(id, time, useCustomSpawnTime, minSeconds, maxSeconds)` sets `spawnSettings.spawnTime`, `useCustomSpawnTime`, `minRespawnTime` and `maxRespawnTime`. It returns `false` for an unknown id, a null `spawnSettings`, an enum value outside `RespawnTime`, `minSeconds` below 0, `maxSeconds` below `minSeconds`, or while `SaveData.Busy` is set. `minSeconds` and `maxSeconds` are game seconds and are used only when `useCustomSpawnTime` is true, as in the game.
  - The game draws the next countdown in `SetSpawnTime`, which returns early while a countdown is already running. A new timing applies from the next spawn cycle, not to the countdown in progress.
  - `SecondsToSpawn` is the countdown itself (`timetospawn`), and the facade does not write it.
- `TriggerNow(id)` calls `TrySpawn()` once and returns its result.
  - The chance roll applies. With `spawnChance` below 1, a miss returns `false`. Set the chance to 1 for a sure spawn.
  - When the spawner has `locations`, the player-distance check applies. If the player is inside `playerDistance`, the spawn is skipped and returns `false`. The spawner then sets `readyToSpawn`, and it spawns on the next map change. Besides a scene reload, a `TriggerNow` on a spawner with `locations` while the player is near also re-subscribes its respawn timer.
  - A spawner with no `locations` skips the player-distance check.
- `Spawned` is raised when `TrySpawn` returns `true`. That includes spawns the game makes itself, such as the spawn at scene start and the spawn on a map change.
  - A non-removing spawner can return `true` and still spawn nothing, when an object already sits at the location. `Spawned` still fires then.
- `CharacterSpawner` can make `TriggerNow` return `true` with nothing spawned. `TrySpawn` returns `true` after the chance roll, and the character step can then skip the spawn (a character with that name is already in the scene, or every character has been used). `Spawned` still fires. Its authored character and spawn-point arrays are private and are not listed.
- `SpawnedCount` counts the non-null entries of the protected `objects` list.

## Save and state

- `respawnEnabled`, `spawnChance`, `spawnTime`, `useCustomSpawnTime`, `minRespawnTime` and `maxRespawnTime` are marked `fsIgnore` (`SpawnSettings`), so the save does not store them. A changed value lasts for the scene session, and a fresh scene load restores the authored value.
- `timetospawn` is not `fsIgnore`, so the save keeps the timer.

## Not exposed

- The game's difficulty scaling of the respawn timer (`currentDifficultyInfo.GetRespawningTime`, called from `SpawnSettings.SetSpawnTime`, `SpawnSettings`). The timing fields are set, but the countdown is only read.
- The spawn contents: which items, furniture or characters appear. `LootPools` and `Spawn` cover item lists.
- `Spawn()` with no chance roll. It is not wrapped, because the game calls it from its own code and there is no single place to hook it.

## Ceilings and risks

- `All` and `Get` list only the active scene. They use `FindObjectsByType`, which skips inactive GameObjects.
- `SpawnedCount` reads the protected `objects` list through reflection. A game update that renames the field makes the count `0`.
- `Spawned` fires on every successful `TrySpawn`, including the spawn the game makes when a scene starts.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
