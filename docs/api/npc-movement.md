---
title: Obenseuer Modding Framework NPC movement API (npcmovement)
parent: API reference
nav_order: 95
---

# Obenseuer Modding Framework NPC movement API (npcmovement)

Status: experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.npcmovement`. Logs go to `BepInEx\logs\obenseuer.moddingframework.npcmovement.log`.

Ids are the NPC ids from the `Npcs` facade (`NpcInfo.Id`, `Npcs.All()`).

## Rules

- **Main thread only.** Call every member on Unity's main thread.
- **Live NPCs only.** Every write works only on an NPC that has a loaded scene instance, is active and is alive. An off-screen NPC returns `false`, and the write does not touch the game's NPC database. The one exception is a claim whose NPC goes off-screen while a patrol or stand the mod started is running. The claim is dropped, the activity is stopped, and the saved activity is cleared (`state.CurrentActivity = null`, when it is still that activity).
- **Owner.** Every write takes the `modId` of your mod. A claim belongs to that mod. An invalid `modId` throws `ArgumentException`, as the other owner calls do. The game's behaviour never throws to the caller.
- **Bad arguments and game failures.** Otherwise a write returns `false` and nothing throws. The first failure of each member is logged at Error.
- **Save loads and saves.** Writes return `false` while a save loads or saves.
- **Release is not skipped.** When the last hold on an NPC goes, the NPC's schedule resumes. If its controller is busy (a save is loading or saving) or inactive at that moment, the release waits and is retried each frame. A controller that has been destroyed drops its claim, and a patrol or stand the mod started on it is stopped.
- **Scene load.** A Single scene load releases every claim, one at a time, using the same rule. An additive load does not. A mod-started patrol or stand is stopped, and the NPC goes back to its schedule. Claim again from your own `Events.SceneLoaded` handler if you still need it.

## Movement

```csharp
public static class NpcMovement
{
    public static bool SetActivityEnabled(string modId, int npcId, bool enabled);
    public static bool MoveTo(string modId, int npcId, Vector3 position);
    public static bool Patrol(string modId, int npcId, string nodeGroupId);
    public static bool Stand(string modId, int npcId, string nodeGroupId);
    public static bool ForceStopActivity(string modId, int npcId);
    public static bool Clear(string modId, int npcId);
}
```

- **SetActivityEnabled(modId, npcId, false)** holds the NPC. The game stops its current activity, and while the hold lasts the NPC's controller does not update and the scheduler does not start or change its activity. It returns `false` for an off-screen NPC.
- **SetActivityEnabled(modId, npcId, true)** releases your hold. It returns `false` when you hold nothing. The NPC's schedule resumes when no mod holds it any more. A mod's own claim is removed before the check, so the release does not wait for itself.
- **MoveTo** holds the NPC and walks it to `position` with the game's own path. The hold keeps the schedule from pulling the NPC back. It returns `false` for an NPC with no `RichAI` agent. The walk path is read from the code, not observed.
- **Patrol** and **Stand** start the game's own patrol or stand activity for a `NodeGroup`. `nodeGroupId` is the `GUID` field of the group's `NodeGroup` component. A group with an empty `GUID` cannot be named, and the framework has no lister for node groups. A patrol needs a group with nodes. A stand needs a group whose `nodeReference` points at a node in the loaded scene. Both refuse while any mod holds the NPC, so release the hold first. The game may still interrupt a patrol or stand that it marks as interruptible, which is the game's own schedule rule.
- **ForceStopActivity** stops the NPC's current activity, whoever started it. The schedule takes over at its next check. It returns `false` when the NPC has no current activity.
- **Clear(modId, npcId)** ends everything your mod has on that NPC. It stops the patrol or stand your mod started, if that is still the current activity, and releases your hold. It returns `false` when your mod has no claim on the NPC.

## Level changes

This facade does not walk an NPC between levels. The game moves an NPC through an asynchronous inter-scene path and a level-change door, and the NPC is in transit while it runs. [NPC actions](npc-actions.md) has `ChangeLevel`, which sets the level of an off-screen NPC directly and refuses an NPC that is on a path.

## Dialogue

This facade does not start conversations. [NPC actions](npc-actions.md) has `StartDialogue`, which starts an existing conversation with a live NPC by its id. `Dialogue.Start(title, actor, conversant)` does the same for a conversant you already hold as a `Transform`.

## Not included

- **A pause flag.** See [NPC schedule API](npc-schedules.md). `SetActivityEnabled` works only for a live NPC, because the in-scene switch needs its controller.
- **Walks that are already under way.** `Clear` does not cancel the walk that `MoveTo` started. The NPC finishes it, or the schedule's next activity sets a new target.

## Example

```csharp
void StartWalk(string npcName)
{
    NpcInfo npc = Npcs.Find(npcName);
    if (npc == null) return;
    NpcMovement.MoveTo("my.mod", npc.Id, new Vector3(10f, 0f, 4f));
}

// later, when the scene event is over
void EndWalk(string npcName)
{
    NpcInfo npc = Npcs.Find(npcName);
    if (npc != null) NpcMovement.Clear("my.mod", npc.Id);
}
```

## If a hook cannot be installed

- `NpcMovement.Tick` is internal. The framework calls it once per frame from its update loop, to retry pending releases and drop claims whose controller was destroyed.
