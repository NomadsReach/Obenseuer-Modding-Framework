---
title: Following the player
parent: NPC actions
grand_parent: People
nav_order: 4
---

# Following the player

These two calls start and stop an NPC walking after the player.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>NpcActions</code> <span>following the player</span></p>

Call `FollowPlayer` to make the NPC follow the player, and `StopFollowing` to end it.

<div class="api-pair" markdown="1">

```csharp
public static class NpcActions
{
    public static bool FollowPlayer(int npcId, bool run, bool untilStopped);
    public static bool StopFollowing(int npcId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `FollowPlayer(npcId, run, untilStopped)` | `bool` | Makes the NPC follow the player. |
| `StopFollowing(npcId)` | `bool` | Makes the NPC stop following. |

</div>
</div>

## Things to know

- `FollowPlayer` makes the NPC walk after the player, or run when `run` is `true`. Always pass `false` for `untilStopped`. Then the follow can be interrupted, and the NPC's own routine can take over again when it needs to.
- With `untilStopped` set to `true`, the follow cannot be interrupted. It lasts until you call `StopFollowing` or the game ends it. It is saved with the NPC, so it can come back in a later session even after your mod is removed. Use `true` only when your mod must hold the NPC. <span class="tech">(`untilStopped` is the game's `canBeInterrupted` argument turned around. The saved state keeps the activity, and `NPCDirector` loads the saved `NPCDataState`.)</span>
- `StopFollowing` makes the NPC stop whatever follow it is doing, and gives `false` when it is not following. This includes a follow that the game, a quest or a conversation started, such as a chase, and not only one your mod started. <span class="tech">(It calls `NPCController.ForceStopActivity` when the current activity is an `Act_Follow`.)</span>

## Limits

- `FollowPlayer` gives `true` only when following is now what the NPC is doing. The game sometimes refuses to start it, for example when the NPC is doing something more important or something that cannot be interrupted. Then you get `false`. <span class="tech">(The game's `FollowPlayer` returns the new activity even when it refuses, so the framework checks the current activity instead. It checks `Data` and `Data.character` first, because the game's follow logs `Data.character.FullName`.)</span>
- NPC 107 can have its follow stopped by the game after a load. When you load a save, the game stops that NPC's follow in some conditions, so a follow on it can end without you calling `StopFollowing`. <span class="tech">(`NPCDirector` force-stops `Act_Follow` on NPC 107 (the `Resident/Crazy Neighbor` actor) at load when that tenement resident is rented and the Dialogue System's `LastVisitDay` for that actor is 4 or less.)</span>
