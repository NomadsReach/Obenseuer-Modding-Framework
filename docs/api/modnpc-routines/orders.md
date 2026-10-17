---
title: Orders
parent: Mod NPC routines
grand_parent: People
nav_order: 2
---

# Orders

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

Every call takes your mod's id (`modId`, see [Mod id](../../glossary.md#mod-id)) and the id you gave the NPC when you spawned it with `ModNpcs.Spawn` or `ModNpcs.SpawnSaved`.

An order is a one-off command for an NPC. An order only works on a live NPC that your mod spawned, in the scene that is loaded. It returns `true` when the NPC started the order.

```csharp
public static partial class ModNpcRoutines
{
    public static bool IsAvailable { get; }    // false when mod NPCs are off, so every order returns false
    public static bool WalkTo(string modId, string npcId, Vector3 point, float yaw, bool run, Action<bool> done = null);   // walks to a point, then turns to face yaw
    public static bool Wander(string modId, string npcId, Vector3 center, float radius);                                   // walks around a spot, resting between walks
    public static bool Patrol(string modId, string npcId, IList<Vector3> points, bool run);                                // walks the points in order, again and again
    public static bool Sit(string modId, string npcId, Vector3 seat);                                                      // sits on the nearest chair
    public static bool Sleep(string modId, string npcId, Vector3 bed);                                                     // lies down in the nearest free bed
    public static bool Stop(string modId, string npcId);                                                                   // ends the current order
}
```

- **`IsAvailable` tells you whether mod NPCs work at all.** When it is `false`, every order returns `false`.
  - Technical: it is the same value as `ModNpcs.IsAvailable`.
- **Orders only work on mod NPCs that are standing in the loaded scene.** A game NPC, or an id that was never spawned, makes the order return `false`. So does an NPC that is switched off or dead.
- **Wrong values throw an error.** A `modId` that is not valid throws an `ArgumentException`. So does a number that is not a real number, a radius below 0.5 or above 50, a patrol of fewer than 2 or more than 32 points, and a points list that is `null`.
- **Some things make an order return `false` instead of throwing.** These are: a save is loading or being written, another call is holding the NPC still (see [NPC movement](../npc-movement.md)), the scene has no walking map, the NPC cannot walk, or, for `Sit` and `Sleep`, no chair or bed is close enough. Nothing is thrown for these.
  - Technical: the checks are `SaveGate.Busy`, `NpcMovement.SetActivityEnabled` holding the NPC, a missing A* graph (`AstarPath.active`), an NPC with no `RichAI` movement component, and no chair or bed in range.
- **An NPC has one order at a time.** A new order replaces the old one. The old order's `done` callback is called with `false`, and the NPC stops whatever it was doing in the game before the new order starts.
- **`WalkTo` walks the NPC to `point` along the game's own paths.** It counts as arrived within 0.6 metres, turns the NPC to face `yaw` and calls `done(true)`. If `run` is `true`, the NPC runs. When the order ends the NPC goes back to the game's slow walk.
  - Technical: `run` uses the game's Run speed, and the game's SlowWalk speed is set again when the order ends.
- **`WalkTo` gives up after a while.** The time allowed is three times the distance divided by the NPC's speed (at least 0.5 metres a second), plus 10 seconds. If it runs out, the NPC stays where it is and `done(false)` is called.
- **A point of exactly (0, 0, 0) is moved 5 centimetres.** The game reads that point as "no target", so the framework nudges it along the x direction.
- **`done` is called once.** It gets `true` if the NPC arrived and `false` otherwise (a timeout, a new order, a scene change or a failure). If your `done` throws an error, it is logged once.
- **`Wander` picks a random spot within `radius` metres of `center`, walks there, waits 4 to 15 seconds, then does it again.** If a spot snaps to somewhere more than 1.5 times the radius from the centre, it picks another, up to 5 times. After that the NPC waits and tries again.
  - Technical: each spot is snapped to the A* graph.
- **`Patrol` walks the points in order and starts again at the first.** `run` works as in `WalkTo`. If one walk times out, the NPC goes on to the next point.
- **`Sit` uses the nearest chair within 1.5 metres of `seat`, and `Sleep` uses the nearest free bed within 2 metres of `bed`.** Both last until you call `Stop`, the NPC is removed, a new scene loads, or the game stops the activity itself. They have no time limit of their own.
  - Technical: they use the game's sit and sleep activities. The framework does not check whether a chair is already taken, because the game does not either. It does not check who owns a bed, so a point at the player's bed seats the NPC there. A chair that hides the NPC while seated is never chosen.
- **`Stop` ends the current order.** The NPC stands up, stops walking, and the framework stops controlling it. It returns `false` when your mod has no order on that NPC.
- **A scene change ends every order.** Each open order ends when a new scene loads in place of the old one, and a `WalkTo` order's `done` gets `false`. Loading the next level in the background (which the game does ahead of time) does not end them.
  - Technical: the orders end on a Single scene load, not on the additive load the game uses to preload a neighbouring level.
- **An order waits while the NPC is busy with something else.** While the player talks to the NPC, another call holds the NPC, or the NPC is doing another activity, a walk waits and its timers stop. The order does not end. `Sit` and `Sleep` are different: if another call holds the NPC, the game stops the sit or sleep, so the order ends once the hold is lifted.
- **If the game fails during an order, the order ends.** The failure is written to the log once for each call, and a `WalkTo` order's `done` gets `false`.
