---
title: Changing respawn settings
parent: Spawners
grand_parent: Objects and devices
nav_order: 3
---

# Changing respawn settings

These two calls change whether a spawner respawns and how long it waits.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Spawners</code> <span>respawn on or off, and the wait</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Spawners
{
    public static bool SetRespawn(string id, bool enabled, float chance);
    public static bool SetRespawnTiming(string id, RespawnTime time, bool useCustomSpawnTime, int minSeconds, int maxSeconds);
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetRespawn(string id, bool enabled, float chance)` | `bool` | Turns respawning on or off and sets the chance. `true` when written. |
| `SetRespawnTiming(string id, RespawnTime time, bool useCustomSpawnTime, int minSeconds, int maxSeconds)` | `bool` | Sets the wait. `true` when written. |

</div>
</div>

## Things to know

- `SetRespawn` sets the chance of a spawn. The chance is limited to 0 through 1. <span class="tech">(it sets `spawnSettings.respawnEnabled` and `spawnSettings.spawnChance`.)</span>
- `SetRespawnTiming` sets how long a spawner waits. The two seconds values are game seconds, and they only count when `useCustomSpawnTime` is `true`. <span class="tech">(it sets `spawnSettings.spawnTime`, `useCustomSpawnTime`, `minRespawnTime` and `maxRespawnTime`.)</span>
- A new wait only applies from the next spawn, not to the countdown already running. The game picks the next countdown once and keeps it until that spawn happens. `SecondsToSpawn` is that countdown, and your mod can't change it. <span class="tech">(the countdown is drawn in `SetSpawnTime`, which returns early while a countdown is running. `SecondsToSpawn` is `timetospawn`, and the framework does not write it.)</span>
- Changes last for the scene session. The game does not save these settings, so loading the scene again brings back the values the map was built with. The game does save the countdown. <span class="tech">(`respawnEnabled`, `spawnChance`, `spawnTime`, `useCustomSpawnTime`, `minRespawnTime` and `maxRespawnTime` are marked `fsIgnore` in `SpawnSettings`, so the save does not store them. `timetospawn` is not `fsIgnore`, so the save keeps the timer.)</span>

## Limits

- `SetRespawn` returns `false` for an unknown id, or while a save is loading or being written.
- Turning respawn on may not start the timer. The game only starts the respawn timer when a spawner starts with respawn already on. A spawner that started with respawn off gets the new setting, but its timer does not run until its scene loads again.
- `SetRespawnTiming` returns `false` for an unknown id, a `time` that is not in `RespawnTime`, `minSeconds` below 0, `maxSeconds` below `minSeconds`, or while a save is loading or being written.
