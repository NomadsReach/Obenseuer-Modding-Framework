---
title: Pursuit
parent: Law and crime
grand_parent: API reference
nav_order: 2
---

# Pursuit

Status: experimental.

## What it is

When the player commits a crime in front of the police, officers can chase them. This API lets your mod check whether the police are chasing the player right now, list the police officers who are out on the map, call the police on the player, stop every chase, and get told when a chase starts and when it ends. Fines and arrests are not here: they are in the Law part of the [story API](../api-story.md).

## When to use it

- Show a warning or play a sound when a police chase starts.
- Pause something in your mod while the player is being chased.
- Call the police on the player, for example as a trap in a quest.
- Stop a chase that your mod wants to end.

## Quick example

This mod writes a log line when a police chase starts and when it ends. When the game is ready, it also logs whether the player is being chased right now.

```csharp
using ObenseuerCore;

[ModInfo("example.chasewatch", "Chase Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ChaseWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Pursuit.Started += () => Log.Info("the police are chasing the player");
        Pursuit.Ended += () => Log.Info("the chase is over");
        Events.WhenReady(() => Log.Info("pursued right now: " + Pursuit.IsPursued));
    }
}
```

`Started` and `Ended` are events: the framework calls your code each time they happen. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.

## Common tasks

Find out how many police officers are out on the map.

```csharp
Log.Info(Pursuit.ActivePolice().Count + " police officers are out");
```

Call the police on the player so that every officer on the map starts a chase. `CallPolice` returns true when at least one officer reacted.

```csharp
if (Pursuit.CallPolice(true, true)) Log.Info("the police are coming");
```

Ask nearby officers to question the player, which is not a chase.

```csharp
Pursuit.CallPolice(false, false);
```

Stop every chase.

```csharp
if (Pursuit.StopAll()) Log.Info("all chases stopped");
```

## Reference

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Reads

`IsPursued` and `ActivePolice()` tell you what the police are doing. They are safe to call at any time.

```csharp
public static class Pursuit
{
    public static bool IsPursued { get; }          // true while a police officer is chasing the player
    public static List<int> ActivePolice();        // the NPC ids of the police officers who are out on the map
}
```

- **`IsPursued` is `true` while a police officer is chasing the player.** Being questioned or searched does not count, and neither does a police conversation.
  - Technical: it is true while any officer on the map is in the chase state (`Act_Police.State.ChasePlayer`).
- **`ActivePolice()` lists the police officers who are out on the map, but not all of them are chasing.** Each id works with `Npcs.Get(id)` on the [NPCs page](npcs.md), and gives an NPC. To know whether the player is being chased, use `IsPursued`.
  - Technical: the ids are the NPC ids of the officers the game has registered as active (`Crime.activePolices`).

### Writes

`CallPolice` and `StopAll` change what the police are doing.

```csharp
public static class Pursuit
{
    public static bool CallPolice(bool severe, bool anyDistance);   // makes the police react to the player; true when at least one officer reacted
    public static bool StopAll();                                   // stops every chase and every questioning; true when the call ran
}
```

- **`CallPolice(severe, anyDistance)` makes the police on the map react to the player, the same way the game does when a crime is reported.** With `severe` set to `true`, officers start a chase. With `severe` set to `false`, they only start questioning the player, which is not a chase, so `IsPursued` stays `false`. With `anyDistance` set to `false`, only officers within 15 metres of the player react. With `anyDistance` set to `true`, every officer on the map reacts. A chase that it starts raises `Started` like any other.
  - Technical: it calls `Act_Police.StartChase` (severe) or `Act_Police.StartChaseQuestion`. It works on a copy of the police list, so an officer removed during the call is skipped, and an officer that throws is logged once while the others still react.
- **`CallPolice` returns `true` when at least one officer reacted, and `false` when nobody was alerted.** It alerts nobody while a save is loading or being written, while an arrest is happening, while the game is changing level or showing the loading screen, while the player is in prison, when the game's crime system is not ready, or when no officer is in range.
  - Technical: the checks are `SaveData.Busy`, no `Crime.instance`, `Crime.ArrestInProgress`, `Changelevel.ChangeLevelActive`, the loading screen not done or fading, `Law.InPrison()`, and no officer within range.
- **Do not use `CallPolice` in multiplayer unless you are the host.** The call is not shared with other players, so use it on the host or in single player only.
- **`StopAll()` ends every chase and every questioning, for every police officer.** It does the same thing the game does when the player is arrested or sent to prison. It does not change crime records, fines or the arrest state. It returns `true` when the call ran, even if no one was chasing, and `false` when the game's crime or NPC data is not ready yet, or while a save is loading or being written.
  - Technical: it calls the game's own `Crime.StopPolicePursuits`, which covers every police officer in the NPC database.
- **After `StopAll()`, the police ignore the player's existing crime record until it grows.** The game marks each officer as having already arrested the player, so they only react again when a new crime adds more than 100 to the record, or when a chase is started.

### Events

`Started` and `Ended` tell you when a chase begins and when it finishes.

```csharp
public static class Pursuit
{
    public static event Action Started;   // the police started chasing the player
    public static event Action Ended;     // the police stopped chasing the player

    public static bool IsAvailable { get; }   // false when the chase events could not be set up
}
```

- **`Started` fires once when the first officer starts a chase.** It does not fire again for each extra officer who joins. It fires when `IsPursued` changes from `false` to `true`.
- **`Ended` fires when the last chase stops.** It fires when `IsPursued` changes from `true` to `false`.
- **The framework only checks for a change at certain moments, and only while someone is listening.** It checks when a police officer changes state, when an officer arrests the player or stops, and after a save finishes loading. If you add your handler while a chase is already going on, you do not get a `Started` for that chase. Nothing is checked while no handler is subscribed.
- **Loading a save ends every chase first.** A player who was being chased gets `Ended` when the load begins. If the save you load is in the middle of a chase, `Started` follows.
  - Technical: the game stops every officer when a load begins.

### What you can't do (yet)

- **You can't get told when the police spot the player.** The game checks this every frame for as long as the player stays in view, so it has no single moment to report. To react to a crime that someone saw, use `Law.Spotted` in the [story API](../api-story.md).
  - Technical: the police field-of-view callback repeats every frame while the player is visible.
- **Robbers are not covered.** They are not police, and they have their own chase states.
  - Technical: `Act_Robber` has its own `Chase` and `Aggro` states.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.pursuit.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes some of the game's methods when it starts, so it can tell your mod when a chase starts or ends. These changes are grouped as a feature group. This page's feature group is `obenseuer.moddingframework.pursuit`, the name you will see in the log.

### When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for every member on this page and for the code that handles each event.
- **It is safe to call any of it at any time, even on the main menu, while loading, or with no save loaded.** If there is nothing to read, you get `false` or an empty list.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **All the changes behind `Started` and `Ended` are needed.** If any one of them cannot be made, the whole feature group turns off, `Pursuit.IsAvailable` becomes `false`, error lines and a warning go to the log, and `Started` and `Ended` never fire. `IsPursued`, `ActivePolice`, `CallPolice` and `StopAll` keep working.
  - Technical: the changes are on `Act_Police.StartState`, `OnPlayerArrested` and `StopActivity`. They also need the private `Crime.activePolices` field and the private `Crime.StopPolicePursuits` method to be found.
