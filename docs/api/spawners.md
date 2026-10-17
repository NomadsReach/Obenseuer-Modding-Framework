---
title: Spawners
parent: Objects and devices
grand_parent: API reference
has_children: true
nav_order: 16
---

# Spawners

## What it is

A spawner is a script in a scene that places things in the world and puts them back after a while: items you can pick up, furniture, blueprints, animals or characters. This API lets your mod list the spawners in the scene you are in, read and change how they respawn, make one spawn right now, and get told each time one spawns something. The spawning itself is still done by the game.

Spawner is a family of scripts. The base one is `Spawner`, and `All()` also lists the others in the family: `CollectibleItemSpawner`, `AnimalSpawner`, `PrefabSpawner`, `FurnitureBlueprintSpawner` and `CharacterSpawner`. Each spawner's kind is in its `TypeName`.

## When to use it

- Log or react when something spawns in a scene.
- Turn respawning on or off for one spawner, or change its chance.
- Change how long a spawner waits before it respawns.
- Make a spawner spawn right now.

## Quick example

This mod writes a log line each time a spawner spawns something. When the game is ready, it also logs every spawner in the scene with its kind and how many things it has spawned.

```csharp
using ObenseuerCore;

[ModInfo("example.spawnwatch", "Spawn Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class SpawnWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Spawners.Spawned += args => Log.Info(args.TypeName + " spawned something, it now has " + args.SpawnedCount);
        Events.WhenReady(() =>
        {
            foreach (SpawnerInfo spawner in Spawners.All())
                Log.Info(spawner.TypeName + " " + spawner.Id + ": " + spawner.SpawnedCount + " spawned");
        });
    }
}
```

`Spawned` is an event: the framework calls your code each time it happens. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.
