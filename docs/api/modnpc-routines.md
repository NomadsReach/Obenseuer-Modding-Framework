---
title: Obenseuer Modding Framework mod NPC routines API (modnpcroutines)
parent: API reference
nav_order: 102.1
---

# Obenseuer Modding Framework mod NPC routines API (modnpcroutines)

Status: experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.modnpcroutines`. Logs go to `BepInEx\logs\obenseuer.moddingframework.modnpcroutines.log`.

Ids are the `npcId` you gave to `ModNpcs.Spawn` or `ModNpcs.SpawnSaved`. Orders work only on NPCs that your mod spawned. They do not work on vanilla NPCs.

## Rules

- **Main thread only.** Call every member on Unity's main thread.
- **Mod NPCs only.** An order works on a live NPC that the framework spawned for your mod. A vanilla NPC, or an unknown id, returns `false`.
- **Loaded scene only.** The NPC must be in the loaded scene, active and alive. Otherwise the order returns `false`.
- **Owner.** Every order takes the `modId` of your mod. An invalid `modId` throws `ArgumentException`, as the other owner calls do.
- **Bad arguments throw.** A non-finite value, a radius outside 0.5 to 50, fewer than 2 or more than 32 patrol points, or a null list throws `ArgumentException`.
- **Refusals return false.** Otherwise an order returns `false` and nothing throws. Refusals are: a save is loading or saving, `NpcMovement.SetActivityEnabled` holds the NPC, the scene has no A* graph, the NPC has no `RichAI` agent, or (for `Sit` and `Sleep`) no chair or bed is in range.
- **Failures are logged once.** A game failure inside an order is logged once per member and ends the order. A `WalkTo` order's `done` gets `false`.
- **Scene load ends every order.** Each open order ends when a Single scene load runs, not on the additive load the game uses to preload a neighbouring level. A `WalkTo` order's `done` gets `false`.
- **Pauses.** While the NPC is talked to, held by `NpcMovement`, or running another activity, a walk waits and its timers stop. The order is not ended. `Sit` and `Sleep` are different: a hold by `NpcMovement` stops the game's sit or sleep, so the order ends once the hold is lifted. Chairs that hide the NPC while seated are never picked.

## Orders

```csharp
public static partial class ModNpcRoutines
{
    public static bool IsAvailable { get; }
    public static bool WalkTo(string modId, string npcId, Vector3 point, float yaw, bool run, Action<bool> done = null);
    public static bool Wander(string modId, string npcId, Vector3 center, float radius);
    public static bool Patrol(string modId, string npcId, IList<Vector3> points, bool run);
    public static bool Sit(string modId, string npcId, Vector3 seat);
    public static bool Sleep(string modId, string npcId, Vector3 bed);
    public static bool Stop(string modId, string npcId);
}
```

- **IsAvailable** is `ModNpcs.IsAvailable`. When it is false, every order returns `false`.
- **One order per NPC.** An order replaces the previous order for the same NPC. The replaced order's `done` is called with `false`, and the NPC's current game activity is stopped before the new order starts.
- **WalkTo** walks the NPC to `point` with the game's own path search. It ends on arrival within 0.6 m, turns the NPC to `yaw`, and calls `done(true)`. `run` uses the game's Run speed for the walk. The game's SlowWalk speed is set again when the order ends. The walk times out after its distance divided by the agent speed (at least 0.5 m/s), times three, plus 10 seconds. On a timeout the NPC stays where it is and `done(false)` is called. A point at exactly (0, 0, 0) is moved 5 cm along x, because the game reads that point as "no target".
- **Wander** picks a random point in the disc of `radius` around `center`, snaps it to the A* graph, walks there, waits 4 to 15 seconds, and repeats. A pick that snaps more than 1.5 times the radius from the centre is retried up to 5 times. After that the NPC waits and tries again.
- **Patrol** walks the points in order and loops. `run` works as in WalkTo. A walk that times out moves on to the next point.
- **Sit** uses the nearest chair within 1.5 m of `seat` and the game's sit activity. **Sleep** uses the nearest unoccupied bed within 2 m of `bed` and the game's sleep activity. Both run until `Stop`, despawn, a Single scene load, or until the game stops the activity itself (for example its stuck handling). They have no timeout of their own. The framework does not check whether a chair is occupied, because the game does not either. The framework does not filter beds by owner, so a point at the player's bed seats an NPC there.
- **Stop** ends the current order. The NPC stands up, stops walking, and the framework stops driving it. It returns `false` when the mod has no order on that NPC.
- **done** is called once for a `WalkTo` order, with `true` on arrival or `false` otherwise. A `done` that throws is logged once.

## Routines

A routine is a weekly timetable for one mod NPC. While a step is in effect, the framework spawns the NPC in the scene that the step names, and it walks, stands, patrols, sits or sleeps there as the step says. When the clock moves to another step, the NPC leaves that scene. Routines last for the game session only. They are not written to the save, so the framework does not store anything for them.

```csharp
[Flags]
public enum NpcDays
{
    None = 0, Monday = 1, Tuesday = 2, Wednesday = 4, Thursday = 8, Friday = 16, Saturday = 32, Sunday = 64,
    Weekdays = 31, Weekend = 96, Every = 127
}

public enum NpcStepKind { Away, Stand, Wander, Patrol, Sit, Sleep }

public sealed class NpcRoutine
{
    public NpcRoutine();
    public int Count { get; }
    public NpcRoutine Stand(NpcDays days, TimeSpan from, TimeSpan to, string scene, Vector3 point, float yaw);
    public NpcRoutine Wander(NpcDays days, TimeSpan from, TimeSpan to, string scene, Vector3 center, float radius);
    public NpcRoutine Patrol(NpcDays days, TimeSpan from, TimeSpan to, string scene, IList<Vector3> points);
    public NpcRoutine Sit(NpcDays days, TimeSpan from, TimeSpan to, string scene, Vector3 seat);
    public NpcRoutine Sleep(NpcDays days, TimeSpan from, TimeSpan to, string scene, Vector3 bed);
    public NpcRoutine Door(string scene, Vector3 point);
}

public sealed class NpcStepArgs
{
    public string ModId { get; }
    public string NpcId { get; }
    public NpcStepKind From { get; }
    public NpcStepKind To { get; }
    public string Scene { get; }
    public int CharacterId { get; }
}

public static partial class ModNpcRoutines
{
    public static event Action<NpcStepArgs> StepChanged;
    public static bool Set(string modId, string npcId, int sourceCharacterId, string name, string lastName, NpcRoutine routine, bool keepSourceDialogue = false);
    public static bool Remove(string modId, string npcId);
    public static bool Clear(string modId);
    public static NpcStepKind Current(string modId, string npcId);
}
```

### Building a routine

- **Steps.** Each step method (`Stand`, `Wander`, `Patrol`, `Sit`, `Sleep`) adds one step and returns the routine, so calls can be chained. A step covers the minutes from `from` up to `to` on each day in `days`. Seconds are dropped, so the step works in whole minutes.
- **Days** is a set of days, and the week starts on Monday, as the game's own calendar does. `None` is refused.
- **Times.** `from` must be 00:00 to 23:59 and `to` must be 00:01 to 24:00. They must differ. A `to` earlier than `from` wraps past midnight. The part after midnight belongs to the start day: a Sunday 23:00 to 02:00 step is active on Sunday night only, and its part after midnight covers Monday 00:00 to 02:00.
- **Overlaps.** Where steps overlap, the step added first wins, as in the game's own timetable.
- **Gaps.** A minute that no step covers is `Away`. The NPC is in no scene at that time.
- **Limits.** A scene name must not be empty. Points, centres, seats, beds, doors and yaw must be finite numbers. `radius` must be 0.5 to 50. A patrol needs 2 to 32 points. A routine holds at most 64 steps. Bad arguments throw `ArgumentException`.
- **Door.** A door point is kept for each scene. A second call for the same scene replaces the first. A door is not a level change. A routine NPC walks to the door, disappears, and appears in the other scene when that scene's step says so.

### Setting and removing

- **Source.** A routine spawns its NPC with the same rules as `ModNpcs.Spawn`. When the source has no prefab, the spawn uses the default body (see [mod NPC API](modnpcs.md#spawn)).
- **Set** stores a copy of the routine for the mod and the id. Later changes to the builder do not reach the stored copy. Calling `Set` again with the same id replaces the routine. It can be called at any time, including on the main menu. The NPC appears once a save is loaded and its scene is active.
- **Set** returns `false` when `ModNpcRoutines.IsAvailable` is false, when the mod already has 64 routines, when the same id is in use by an NPC that no routine started, or when a saved mod NPC (`ModNpcs.SpawnSaved`) holds the id, even if that NPC is not in the loaded scene. Do not use one id for both `ModNpcs.SpawnSaved` and a routine.
- **Remove** forgets the routine and despawns its NPC if a routine spawned it. **Clear** does the same for every routine of the mod. Both return `false` when there was nothing to remove.
- **Current** returns the step kind in effect by the game clock, whether or not the NPC is in the loaded scene. It returns `Away` for an unknown id.
- **StepChanged** fires once when the step in effect changes. A scene load alone does not fire it. The first check after `Set` always fires, so `From` and `To` can both be `Away`. The arguments give the NPC's character id, or 0 when it is not in the loaded scene.

### What the framework does each minute

The framework checks each routine once per in-game minute, after a scene load, and after `Set`. For the scene that is loaded:

| Step in effect | NPC in this scene | What happens |
|---|---|---|
| This scene | No | The NPC spawns at the step's point. If the step changed while the player was already in this scene and the scene has a door point, the NPC spawns at the door and walks in. |
| This scene | Yes | The step's order starts if neither a one-off order nor the routine's own order is running. A new step ends the routine's previous order first. |
| Another scene, or `Away` | Yes, spawned by a routine | The NPC waits while it is talked to. If this scene has a door point, it walks there and is then despawned. Otherwise it is despawned at once. |
| Another scene, or `Away` | No | Nothing. |

- **Orders.** A routine step uses the same walking rules as the orders above. A one-off order from `WalkTo`, `Wander`, `Patrol`, `Sit` or `Sleep` pauses the routine until it ends; a step change does not end it, and the NPC does not leave its scene while it runs. `Stop` ends any order, including the routine's own; the routine starts its step again at its next check (the next in-game minute).
- **Timeouts.** A routine walk that times out teleports the NPC to the step's point, so the routine stays on time.
- **Seats and beds.** A `Sit` or `Sleep` step with no chair or bed in range stands the NPC at the step's point and logs once.
- **Death.** A routine NPC that dies stays dead until its scene loads again. Then it spawns alive at the step's point. Call `Remove` to stop it for good.
- **Locked doors.** A routine NPC cannot pass a locked door, as in orders.
- **Saves.** Nothing is saved for a routine. After a reload the NPC is placed where the clock says.
- **Start delay.** The 300 frames count from the spawn. If the NPC has not started its first order by then, each in-game minute tries again.
- **New game.** Routines run on a new game once its first level has loaded, as they do on a loaded save. Nothing is saved for a routine, so a new game needs no save folder.
