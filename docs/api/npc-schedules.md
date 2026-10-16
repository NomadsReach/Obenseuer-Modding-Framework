---
title: Obenseuer Modding Framework NPC schedule API (npcschedules)
parent: API reference
nav_order: 50
---

# Obenseuer Modding Framework NPC schedule API (npcschedules)

Status: experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.npcschedules`. Logs go to `BepInEx\logs\obenseuer.moddingframework.npcschedules.log`.

Ids are the NPC ids from the `Npcs` facade (`NpcInfo.Id`, `Npcs.All()`). This area does not list NPCs.

Use `NpcSchedules` to read what an NPC is doing now and its weekly timetable, and to get told when its activity changes. It only reads: to move an NPC around, use [NPC movement](npc-movement.md).

## Rules

- **Main thread only.** Call every member and handle every event on Unity's main thread.
- `NpcSchedules.IsAvailable` is false when the activity hook was not installed or its owner was disabled. The reads keep working when it is false.
- **Availability.** Every member is safe at any time: main menu, loading, no save loaded. Reads return a neutral default (`null` or an empty list) when no NPC data exists for the id.
- **Bad arguments.** No member throws. An unknown id returns `null` or an empty list.
- **Game failures.** When a read throws inside the game, it returns its neutral default. The first throw of each member is logged at Error.
- **Events run inside the game's call.** `ActivityChanged` is raised from a postfix on the game's activity setter, on the main thread. It is raised only when there is at least one subscriber. Handlers run one at a time, so a handler that throws is logged and the other handlers still run. Do not start or stop NPC activities synchronously from a handler.
- **No polling.** The framework runs no timer. The event comes from the game's own activity setter. The game assigns `null` to an idle NPC's activity every frame, so the hook runs often, but with no subscriber, or when the activity did not change, it only reads one field and returns. The NPC lookup runs only for a real change with a subscriber.
- **All or nothing.** The activity hook is one patch under the owner `obenseuer.moddingframework.npcschedules`. If it is missing in this build, the owner is disabled, one warning is logged, and `ActivityChanged` never fires. The reads keep working.

## Schedules

```csharp
public sealed class ScheduleInfo
{
    public string Name { get; }             // schedule entry name from the game data
    public string ActivityName { get; }     // activity name, or the activity class name when the data has none. null when the entry has no activity.
    public string Place { get; }            // scene the activity takes place in, null when unknown
    public int WeekDay { get; }             // game weekday index
    public float StartHour { get; }         // 0 to 24, hours plus minutes / 60
    public float DurationMinutes { get; }
    public bool Disabled { get; }           // true when the game data disables the entry
}

public static class NpcSchedules
{
    public static bool IsAvailable { get; }
    public static string CurrentActivity(int npcId);         // activity name, or the class name when unnamed. null when the NPC has no activity.
    public static List<ScheduleInfo> Timetable(int npcId);   // the NPC's timetable in the game's stored order. Empty when unknown.
}
```

- **CurrentActivity** reads the game's current activity for the NPC, whether the NPC is in the loaded scene or not.
- **Timetable** lists only the fixed timetable. The game's random activities are not included.

## Events

```csharp
public sealed class ActivityChangedArgs
{
    public int NpcId { get; }
    public string From { get; }    // previous activity name, null when the NPC had none
    public string To { get; }      // new activity name, null when the activity was cleared
}

public static class NpcSchedules
{
    public static event Action<ActivityChangedArgs> ActivityChanged;
}
```

- **ActivityChanged** fires when the game sets the NPC's current activity to a different activity. It also fires when the game clears the activity (`To` is `null`). It does not fire for an assignment of the same activity.
- **Load.** Loading a save replaces the NPC state object without going through the setter, so it does not fire `ActivityChanged`. Activity changes the game makes right after a load do fire it.

## Not included

- **SetPaused.** Not added. The game's off-screen pause flag (`NPCDataState.schedulerDisabled`) does not stop an NPC that is in the loaded scene. The scheduler keeps starting activities for in-scene NPCs. The in-scene switch (`NPCController.DisableActivity`) interrupts the running activity and needs a live controller. There is no single pause that works for both. The flag is also saved, and a flagged NPC is not spawned when its scene loads, so a pause would hide the NPC. For an in-scene hold, use `NpcMovement.SetActivityEnabled`.
- **GoTo.** Not added. `NPCPathfinding` only computes a path (`SearchForPath`) and does not set an NPC's target. The NPC-level move (`NPCController.SetTargetPosition`) works only in the loaded scene, and the scheduler replaces it with the next scheduled activity.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
