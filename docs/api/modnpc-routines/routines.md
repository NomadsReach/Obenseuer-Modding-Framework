---
title: Routines
parent: Mod NPC routines
grand_parent: People
nav_order: 3
---

# Routines

A routine is a weekly timetable for one mod NPC. You build it with `NpcRoutine`, then store it with `ModNpcRoutines.Set`. A routine lasts for the game session only. It is not written to the player's save, so you set it again each time the game starts.

```csharp
[Flags]
public enum NpcDays
{
    None = 0,         // no days (not allowed in a step)
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,
    Weekdays = 31,    // Monday to Friday
    Weekend = 96,     // Saturday and Sunday
    Every = 127       // every day of the week
}

public enum NpcStepKind
{
    Away,      // the NPC is in no scene
    Stand,     // standing at a point
    Wander,    // wandering around a spot
    Patrol,    // walking between points
    Sit,       // sitting on a chair
    Sleep      // sleeping in a bed
}

public sealed class NpcRoutine    // a weekly timetable, built one step at a time
{
    public NpcRoutine();                                                   // makes an empty timetable
    public int Count { get; }                                              // how many steps it has
    public NpcRoutine Stand(NpcDays days, TimeSpan from, TimeSpan to, string scene, Vector3 point, float yaw);       // stand at a point, facing yaw
    public NpcRoutine Wander(NpcDays days, TimeSpan from, TimeSpan to, string scene, Vector3 center, float radius);   // wander around a spot
    public NpcRoutine Patrol(NpcDays days, TimeSpan from, TimeSpan to, string scene, IList<Vector3> points);          // patrol along points
    public NpcRoutine Sit(NpcDays days, TimeSpan from, TimeSpan to, string scene, Vector3 seat);                      // sit on the chair near this point
    public NpcRoutine Sleep(NpcDays days, TimeSpan from, TimeSpan to, string scene, Vector3 bed);                     // sleep in the bed near this point
    public NpcRoutine Door(string scene, Vector3 point);                   // where the NPC walks in and out of this scene
}

public sealed class NpcStepArgs    // what you are told when the NPC changes step
{
    public string ModId { get; }          // your mod's id
    public string NpcId { get; }          // the id you gave the NPC
    public NpcStepKind From { get; }      // the step it was in
    public NpcStepKind To { get; }        // the step it is in now
    public string Scene { get; }          // the scene the new step is in
    public int CharacterId { get; }       // the NPC's character number; 0 when it is not in the loaded scene
}

public static partial class ModNpcRoutines
{
    public static event Action<NpcStepArgs> StepChanged;    // an NPC's timetable has moved to a different step
    public static bool Set(string modId, string npcId, int sourceCharacterId, string name, string lastName, NpcRoutine routine, bool keepSourceDialogue = false);   // stores a timetable for an NPC
    public static bool Remove(string modId, string npcId);    // removes one timetable (and its NPC)
    public static bool Clear(string modId);                   // removes all of your mod's timetables
    public static NpcStepKind Current(string modId, string npcId);   // the step the clock says the NPC is in now
}
```

### Building a routine

- **Each step method adds one step and gives the routine back, so you can chain calls.** A step covers the time from `from` up to `to` on each day in `days`. Seconds are dropped, so a step works in whole minutes.
- **`days` is a set of days, and the week starts on Monday, like the game's own calendar.** `NpcDays.None` is not allowed.
- **`from` must be 00:00 to 23:59, and `to` must be 00:01 to 24:00.** They must not be the same. If `to` is earlier than `from`, the step goes past midnight. The part after midnight belongs to the start day: a Sunday 23:00 to 02:00 step is active on Sunday night only, and its part after midnight covers Monday 00:00 to 02:00.
- **When steps overlap, the step you added first wins.** This is how the game's own timetable works.
- **A minute that no step covers is `Away`.** The NPC is in no scene at that time.
- **Wrong values throw an `ArgumentException`.** The scene name must not be empty. Points, centres, seats, beds, doors and `yaw` must be real numbers. `radius` must be 0.5 to 50. A patrol needs 2 to 32 points. A routine holds at most 64 steps.
- **`Door(scene, point)` says where an NPC walks in and out of a scene.** There is one door point for each scene, and calling `Door` again for the same scene replaces the first. A door is not a level change: the NPC walks to the door and disappears, and then appears in the other scene when that scene's step says so.

### Setting and removing

- **A routine spawns its NPC the same way `ModNpcs.Spawn` does.** If the source character has no body of its own, the NPC gets the default body. See [Mod NPCs](../modnpcs/spawn.md#spawn).
- **`Set` stores a copy of the routine for your mod and the NPC id.** Changing your `NpcRoutine` afterwards does not change the stored copy. Calling `Set` again with the same id replaces the routine. You can call it at any time, even on the main menu. The NPC appears once a save is loaded and its scene is active.
- **`Set` throws an `ArgumentException` for wrong values.** That means an invalid `modId`, an `npcId` that is empty or longer than 64 characters, an empty `name`, a `null` routine, or a routine with no steps.
- **`Set` returns `false` when it did not store the routine.** That happens when mod NPCs are off (`IsAvailable` is `false`), when your mod already has 64 routines, when an NPC that no routine started already uses the id, and when a saved mod NPC (from `ModNpcs.SpawnSaved`) uses the id, even if that NPC is not in the loaded scene. So never use the same id for `SpawnSaved` and for a routine.
- **`Remove` forgets the routine and removes its NPC if a routine spawned it.** `Clear` does the same for every routine of your mod. Both return `false` when there was nothing to remove.
- **`Current` tells you the step the game clock puts the NPC in, whether or not the NPC is in the loaded scene.** It gives `Away` for an id it does not know.
- **`StepChanged` fires once when the step in effect changes.** Loading a scene alone does not fire it. The first check after `Set` always fires, so `From` and `To` can both be `Away`.

### What the framework does each minute

The framework checks each routine once for every in-game minute, after a scene loads, and after `Set`. This is what it does for the scene that is loaded:

| The step in effect is | The NPC is in this scene | What happens |
|---|---|---|
| This scene | No | The NPC appears at the step's point. If the step changed while the player was already in this scene, and the scene has a door point, the NPC appears at the door and walks in. |
| This scene | Yes | The step's order starts, unless a one-off order or the routine's own order is already running. A new step ends the routine's previous order first. |
| Another scene, or `Away` | Yes, and a routine spawned it | The NPC waits while someone is talking to it. If this scene has a door point, it walks there and is then removed. Otherwise it is removed at once. |
| Another scene, or `Away` | No | Nothing happens. |

- **A routine step walks by the same rules as the orders above.** A one-off order (`WalkTo`, `Wander`, `Patrol`, `Sit` or `Sleep`) pauses the routine until the order ends. A step change does not end it, and the NPC does not leave its scene while it runs. `Stop` ends any order, including the routine's own. The routine then starts its step again at the next check, one in-game minute later.
- **A routine walk that runs out of time moves the NPC to the step's point.** This keeps the routine on time.
- **A `Sit` or `Sleep` step with no chair or bed in range stands the NPC at the step's point instead.** One line goes to the log.
- **A routine NPC that dies stays dead until its scene loads again.** It then appears alive at the step's point. Call `Remove` to stop it for good.
- **A routine NPC cannot go through a locked door**, the same as for orders.
- **Nothing is saved for a routine.** After a reload the NPC is placed where the clock says it should be. A new game works the same way once its first level has loaded.
- **If the NPC has not started its first order within 300 frames of appearing, the framework tries again each in-game minute.**
