---
title: NPC schedules
parent: People
grand_parent: API reference
nav_order: 10
---

# NPC schedules

Status: experimental.

## What it is

The game gives its NPCs a weekly timetable: at certain days and hours an NPC goes to a place and does something, such as working, eating or sleeping. This API lets a mod read what an NPC is doing right now, read its whole weekly timetable, and get told when an NPC starts a different activity. It only reads. It does not move NPCs. To move an NPC around, use [NPC movement](npc-movement.md).

NPCs are given by their id. You get the ids from the [NPC API](npcs.md) (`NpcInfo.Id`, `Npcs.All()`). This API does not list NPCs itself.

NPCs that your own mod spawned have no timetable of the game's kind. To give those a timetable, use [Mod NPC routines](modnpc-routines.md).

## When to use it

- Show or log what an NPC is doing, for example "Sam is at work".
- React when an NPC starts or stops an activity.
- Work out where an NPC will be at a certain time by reading its timetable.

## Quick example

This mod writes a log line each time any NPC changes activity. Once a save is loaded, it also logs what the first NPC the game knows is doing, and how many entries that NPC's timetable has.

```csharp
using ObenseuerCore;

[ModInfo("example.schedulewatch", "Schedule Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ScheduleWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        NpcSchedules.ActivityChanged += args => Log.Info("NPC " + args.NpcId + " went from " + (args.From ?? "nothing") + " to " + (args.To ?? "nothing"));
        Events.WhenReady(() =>
        {
            foreach (NpcInfo npc in Npcs.All())
            {
                Log.Info(npc.Name + " is doing " + (NpcSchedules.CurrentActivity(npc.Id) ?? "nothing") + ", timetable entries: " + NpcSchedules.Timetable(npc.Id).Count);
                break;
            }
        });
    }
}
```

`ActivityChanged` is an event: the framework calls your code each time it happens. `From` and `To` are `null` when the NPC had no activity before, or has none now, which is why the example writes "nothing" in their place. `WhenReady` runs your code once a save is loaded (see [Using events](../guides/getting-started/using-events.md)).

## Common tasks

Find out what one NPC is doing now. You get `null` when the NPC has no activity or the id is not known.

```csharp
string activity = NpcSchedules.CurrentActivity(npcId);
```

Print an NPC's whole timetable.

```csharp
foreach (ScheduleInfo entry in NpcSchedules.Timetable(npcId))
    Log.Info(entry.Name + ": " + entry.ActivityName + " at " + entry.Place + ", day " + entry.WeekDay + ", from hour " + entry.StartHour + " for " + entry.DurationMinutes + " minutes");
```

Check that the change event works before you rely on it.

```csharp
if (!NpcSchedules.IsAvailable) Log.Warn("Activity changes are not reported");
```

## Reference

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Schedules

`NpcSchedules` reads an NPC's current activity and its timetable. Each timetable entry is a `ScheduleInfo`.

```csharp
public sealed class ScheduleInfo    // one entry in an NPC's timetable
{
    public string Name { get; }             // the entry's name in the game's data
    public string ActivityName { get; }     // what the NPC does; null when the entry has no activity
    public string Place { get; }            // the scene the activity takes place in; null when unknown
    public int WeekDay { get; }             // the day of the week the entry starts on, as the game numbers it
    public float StartHour { get; }         // when it starts, from 0 to 24 (6.5 means half past six)
    public float DurationMinutes { get; }   // how long it lasts, in minutes
    public bool Disabled { get; }           // true when the game's data switches this entry off
}

public static class NpcSchedules
{
    public static bool IsAvailable { get; }                  // false when the activity change event could not be set up
    public static string CurrentActivity(int npcId);         // what the NPC is doing now; null when it has no activity
    public static List<ScheduleInfo> Timetable(int npcId);   // the NPC's timetable; an empty list when the NPC is not known
}
```

- **`CurrentActivity(npcId)` tells you what the game says the NPC is doing, even when the NPC is not in the scene you are in.** You get `null` when the NPC has no activity or no NPC has that id.
  - Technical: it reads the current activity of the NPC's game data. The name is the activity's name, or the activity's class name when the activity has no name.
- **`Timetable(npcId)` lists the fixed timetable only, in the order the game stores it.** Random activities that the game picks on its own are not in it. You get an empty list when the id is not known.
- **`ActivityName` is the activity's name, or the name of its code class when it has no name.** It is `null` when the entry has no activity.
- **`IsAvailable` is `false` when the activity change event could not be set up.** The reads keep working when it is `false`. See [If something fails to set up](#if-something-fails-to-set-up).

### Activity changes

`ActivityChanged` tells you when an NPC starts doing something different.

```csharp
public sealed class ActivityChangedArgs    // what you are told when an NPC's activity changes
{
    public int NpcId { get; }        // the NPC's id
    public string From { get; }      // what it was doing; null when it had no activity
    public string To { get; }        // what it does now; null when its activity was cleared
}

public static class NpcSchedules
{
    public static event Action<ActivityChangedArgs> ActivityChanged;   // an NPC has started a different activity
}
```

- **`ActivityChanged` fires when the game gives an NPC a different activity, and also when the game clears it.** When the activity is cleared, `To` is `null`. It does not fire when the game sets the same activity again.
  - Technical: it is raised from a postfix on the game's `NPCDataState.CurrentActivity` setter.
- **Loading a save does not fire it.** A save load replaces the NPC's whole state at once, so no single change happens. Changes the game makes right after the load do fire it.
- **The framework does not poll.** It runs no timer. The game sets an idle NPC's activity to nothing every frame, so the change is checked very often, but with no handler, or when nothing changed, it only reads one value and stops. It looks the NPC up only for a real change when you have a handler.
  - Technical: the game assigns `null` to an idle NPC's `CurrentActivity` every frame.
- **Do not start or stop NPC activities from inside your handler.** Your handler runs inside the game's own call, so wait until the game has finished, for example with a timer.
- **`ActivityChanged` also fires while a save is loading, if an activity changes then.** If you want to ignore those, check `SaveData.Busy`, which is `true` while a save is loading or being written.

### What you can't do (yet)

- **You can't pause an NPC's timetable.** There is no single switch that works for every NPC. The game has a flag that is meant to pause an NPC that is away, but it does not stop an NPC that is in the scene, and the game saves it, so a flagged NPC would not appear when its scene loads and would seem to vanish. The game has another switch for an NPC in the scene, but it needs the live NPC and it stops the activity that is running. For a pause in the scene you are in, use `NpcMovement.SetActivityEnabled` (see [NPC movement](npc-movement.md)).
  - Technical: the saved flag is `NPCDataState.schedulerDisabled`. The scheduler keeps starting activities for in-scene NPCs. The in-scene switch is `NPCController.DisableActivity`.
- **You can't send an NPC to a place from here.** The game's path search only works out a route and does not give the NPC a target. The call that gives an NPC a target only works in the loaded scene, and the timetable replaces that target as soon as the next activity starts. Use [NPC movement](npc-movement.md) instead.
  - Technical: `NPCPathfinding` only computes a path (`SearchForPath`). The NPC call is `NPCController.SetTargetPosition`.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.npcschedules.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

### When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This includes the code that handles `ActivityChanged`.
- **It is safe to call any of it at any time, even on the main menu or while a save is loading.** If there is no NPC data for the id, you get `null` or an empty list.
- **Your event handlers run inside the game's own call, one at a time.** If one of your handlers throws an error, the framework logs it and still runs the other handlers. The event is only raised when at least one handler is listening.

### What to check for and what comes back

- **Nothing here throws.** An id that is not known gives `null` or an empty list.
- **If the game itself fails while the framework reads, you get that plain default instead of an error.** The first failure of each member is written to the log as an error. Later failures of that member are not logged again.

### If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when an activity changes. If a game update breaks one of them, only that feature turns off, and the framework writes error lines and a warning to the log. The rest of the API keeps working.

- **The change event depends on the feature group `obenseuer.moddingframework.npcschedules`.** This is the name you will see in the log. It has one game change in it: the setter for an NPC's current activity. If the framework can't make it in your game version, it turns the group off, writes error lines and a warning to its log file, and `ActivityChanged` never fires. `IsAvailable` is then `false`. `CurrentActivity` and `Timetable` keep working.
  - Technical: the game method is `NPCDataState.set_CurrentActivity`.
