---
title: The Spawned event
parent: Spawners
grand_parent: Objects and devices
nav_order: 5
---

# The Spawned event

```csharp
public static class Spawners
{
    public static event Action<SpawnerArgs> Spawned;   // a spawner spawned something
}

public sealed class SpawnerArgs    // what you get when a spawner spawns
{
    public string Id { get; }            // the id of the spawner, as SpawnerInfo.Id
    public string TypeName { get; }      // what kind of spawner it is
    public int SpawnedCount { get; }     // how many things it has spawned, counted after this spawn
}
```

- **`Spawned` fires every time a spawner spawns, including the spawns the game makes by itself.** That covers the spawn when a scene starts and the spawn on a map change, not just the ones you trigger.
  - Technical: it is raised from a postfix on `Spawner.TrySpawn(bool)` whenever the method returns `true`.
- **`Spawned` can fire even though nothing new appeared.** See the `TriggerNow` bullets above for the cases.
