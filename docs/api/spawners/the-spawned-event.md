---
title: The Spawned event
parent: Spawners
grand_parent: Objects and devices
nav_order: 5
---

# The Spawned event

The `Spawned` event tells your mod each time a spawner spawns something.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Spawners</code> <span>the spawn event</span></p>

`Spawned` is the event to subscribe to. It fires each time a spawner spawns, including the spawns the game makes on its own.

<div class="api-pair" markdown="1">

```csharp
public static class Spawners
{
    public static event Action<SpawnerArgs> Spawned;
}
```

| Event | Gives you | When it fires |
|---|---|---|
| `Spawned` | `SpawnerArgs` | A spawner spawned something. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SpawnerArgs</code> <span>what you get when a spawner spawns</span></p>

The `Spawned` event gives you one of these. `SpawnedCount` is counted after this spawn.

<div class="api-pair" markdown="1">

```csharp
public sealed class SpawnerArgs
{
    public string Id { get; }
    public string TypeName { get; }
    public int SpawnedCount { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The id of the spawner, as `SpawnerInfo.Id`. |
| `TypeName` | `string` | What kind of spawner it is. |
| `SpawnedCount` | `int` | How many things it has spawned, counted after this spawn. |

</div>
</div>

## Things to know

- `Spawned` fires every time a spawner spawns, including the spawns the game makes by itself. That covers the spawn when a scene starts and the spawn on a map change, not just the ones you trigger. <span class="tech">(it is raised from a postfix on `Spawner.TrySpawn(bool)` whenever the method returns `true`.)</span>

## Limits

- `Spawned` can fire even though nothing new appeared. See the notes on `TriggerNow` in [Spawning now](spawning-now.md).
