---
title: Common tasks
parent: Spawners
grand_parent: Objects and devices
nav_order: 1
---

# Common tasks

Make a spawner respawn with a 50 percent chance.

```csharp
SpawnerInfo spawner = Spawners.Get("some-spawner-id");
if (spawner != null) Spawners.SetRespawn(spawner.Id, true, 0.5f);
```

Make a spawner spawn right now, with a sure spawn. The chance roll still applies, so set the chance to 1 first.

```csharp
Spawners.SetRespawn("some-spawner-id", true, 1f);
bool spawned = Spawners.TriggerNow("some-spawner-id");
Log.Info(spawned ? "it spawned" : "nothing spawned");
```

Set a custom respawn wait of 60 to 120 game seconds.

```csharp
Spawners.SetRespawnTiming("some-spawner-id", RespawnTime.Default, true, 60, 120);
```
