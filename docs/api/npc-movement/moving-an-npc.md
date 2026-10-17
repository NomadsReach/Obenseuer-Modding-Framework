---
title: Moving an NPC
parent: NPC movement
grand_parent: People
nav_order: 2
---

# Moving an NPC

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

Every call takes `modId`, the id of your mod. The framework uses it to remember which mod holds which NPC (see [Owner](../../glossary.md#owner) in the glossary). Taking over an NPC's movement is called holding it.

```csharp
public static class NpcMovement
{
    public static bool SetActivityEnabled(string modId, int npcId, bool enabled);   // false holds the NPC still; true gives your hold back
    public static bool MoveTo(string modId, int npcId, Vector3 position);           // walks the NPC to a spot in the world
    public static bool Patrol(string modId, int npcId, string nodeGroupId);         // makes the NPC patrol a group of waypoints
    public static bool Stand(string modId, int npcId, string nodeGroupId);          // makes the NPC stand at a group of waypoints
    public static bool ForceStopActivity(string modId, int npcId);                  // stops what the NPC is doing now, whoever started it
    public static bool Clear(string modId, int npcId);                              // ends everything your mod has on this NPC
}
```

- **`SetActivityEnabled(modId, npcId, false)` holds the NPC still.** The game stops what the NPC is doing. While the hold lasts, the schedule does not start or change the NPC's activity. It returns `false` for an NPC that is not in the loaded scene.
  - Technical: the NPC's controller does not update while the hold lasts.
- **`SetActivityEnabled(modId, npcId, true)` gives your hold back.** It returns `false` when your mod holds nothing. The NPC's schedule goes on again once no mod holds it any more. The framework removes your own claim before it checks, so the release does not wait for itself.
- **`MoveTo` walks the NPC to `position`, by the game's own pathfinding.** It also holds the NPC, so the schedule can't pull it back. It returns `false` for an NPC that can't walk, because it has no path-following part.
  - Technical: it needs a `RichAI` agent. The walk path was read from the code, not observed in the game.
- **`Patrol` and `Stand` start the game's own patrol or stand activity for a group of waypoints.** `nodeGroupId` is the id written on the group in the scene. A group with no id can't be named, and the framework has no call that lists the groups. A patrol needs a group that has waypoints. A stand needs a group that points at a waypoint in the loaded scene.
  - Technical: `nodeGroupId` is the `GUID` field of the group's `NodeGroup` component. A stand needs a `nodeReference` that points at a node in the loaded scene.
- **`Patrol` and `Stand` refuse while any mod holds the NPC.** Release the hold first. The game may still interrupt a patrol or stand that it marks as interruptible. That is the game's own schedule rule.
- **`ForceStopActivity` stops the NPC's current activity, whoever started it.** The schedule takes over again at its next check. It returns `false` when the NPC has no current activity.
- **`Clear(modId, npcId)` ends everything your mod has on that NPC.** It stops the patrol or stand your mod started, if that is still what the NPC is doing, and gives your hold back. It returns `false` when your mod has no claim on the NPC.
- **`Clear` does not cancel a walk that `MoveTo` started.** The NPC finishes the walk, or the schedule's next activity sets a new target.
