---
title: Spawning now
parent: Spawners
grand_parent: Objects and devices
nav_order: 4
---

# Spawning now

This call makes a spawner spawn once, right away.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Spawners</code> <span>spawn once, now</span></p>

`TriggerNow(id)` asks a spawner to spawn once, right away. It returns `true` when the game says it spawned.

<div class="api-pair" markdown="1">

```csharp
public static class Spawners
{
    public static bool TriggerNow(string id);
}
```

| Method | Returns | What it does |
|---|---|---|
| `TriggerNow(id)` | `bool` | Asks the spawner to spawn once. `true` when the game says it spawned. |

</div>
</div>

## Things to know

- `TriggerNow(id)` asks the spawner to spawn once and gives you the game's answer. <span class="tech">(it calls `Spawner.TrySpawn()` once and returns its result.)</span>
- The chance still applies. If the spawner's chance is below 1, a miss returns `false`. To be sure it spawns, set the chance to 1 first.

## Limits

- `TriggerNow` returns `false` for an unknown id, or while a save is loading or being written.
- A spawner with set locations does not spawn while the player is close to them. If the player is inside the spawner's distance, the spawn is skipped and `TriggerNow` returns `false`. The spawner then waits, and spawns on the next map change. A spawner with no locations does not do this check. <span class="tech">(the check is `playerDistance`. The spawner sets `readyToSpawn`. A `TriggerNow` on a spawner with `locations` while the player is near also re-subscribes its respawn timer, in addition to a scene reload.)</span>
- `TriggerNow` can return `true` even though nothing new appeared. A spawner can say it spawned when an object already sits at that spot. A character spawner can also skip the spawn after the chance roll, when a character with that name is already in the scene or all its characters have been used. <span class="tech">(the object case is a non-removing spawner, which returns `true` and spawns nothing when an object already sits at the location. `TrySpawn` returns `true` after the chance roll. `CharacterSpawner`'s authored character and spawn-point arrays are private, so they are not listed.)</span>
