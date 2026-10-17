---
title: Changing respawn settings
parent: Spawners
grand_parent: Objects and devices
nav_order: 3
---

# Changing respawn settings

```csharp
public static class Spawners
{
    public static bool SetRespawn(string id, bool enabled, float chance);   // turns respawning on or off and sets the chance; true when written
    public static bool SetRespawnTiming(string id, RespawnTime time, bool useCustomSpawnTime, int minSeconds, int maxSeconds);   // sets the wait; true when written
}
```

- **`SetRespawn` turns respawning on or off and sets the chance of a spawn.** The chance is limited to 0 through 1. It returns `true` when the values were written. It returns `false` for an unknown id, or while a save is loading or being written.
  - Technical: it sets `spawnSettings.respawnEnabled` and `spawnSettings.spawnChance`.
- **Turning respawn on may not start the timer.** The game only starts the respawn timer when a spawner starts with respawn already on. A spawner that started with respawn off gets the new setting, but its timer does not run until its scene loads again.
- **`SetRespawnTiming` sets how long a spawner waits.** It returns `false` for an unknown id, a `time` that is not in `RespawnTime`, `minSeconds` below 0, `maxSeconds` below `minSeconds`, or while a save is loading or being written. The two seconds values are game seconds, and they only count when `useCustomSpawnTime` is `true`.
  - Technical: it sets `spawnSettings.spawnTime`, `useCustomSpawnTime`, `minRespawnTime` and `maxRespawnTime`.
- **A new wait only applies from the next spawn, not to the countdown already running.** The game picks the next countdown once and keeps it until that spawn happens. `SecondsToSpawn` is that countdown, and your mod can't change it.
  - Technical: the countdown is drawn in `SetSpawnTime`, which returns early while a countdown is running. `SecondsToSpawn` is `timetospawn`, and the framework does not write it.
- **Changes last for the scene session.** The game does not save these settings, so loading the scene again brings back the values the map was built with. The game does save the countdown.
  - Technical: `respawnEnabled`, `spawnChance`, `spawnTime`, `useCustomSpawnTime`, `minRespawnTime` and `maxRespawnTime` are marked `fsIgnore` in `SpawnSettings`, so the save does not store them. `timetospawn` is not `fsIgnore`, so the save keeps the timer.
