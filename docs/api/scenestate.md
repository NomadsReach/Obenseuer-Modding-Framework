---
title: SceneState API
parent: API reference
nav_order: 97.2
---

# SceneState API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. File: `src/Api/SceneState.cs`.

This area reads and sets the scene variables that the scene's own logic keeps: branches (true or false), counters, state machines, sequences and timers. Ids are the authored GUIDs, or scene paths where a GUID is missing, the same as `SceneLogic`.

## API

```csharp
public static List<BranchInfo> Branches();
public static bool SetBranch(string modId, string id, bool value);
public static List<CounterInfo> Counters();
public static bool SetCounter(string modId, string id, int value);
public static bool AddCounter(string modId, string id, int amount);
public static List<StateMachineInfo> States();
public static bool SetState(string modId, string id, string stateName);
public static List<SequenceInfo> Sequences();
public static bool StartSequence(string modId, string id);
public static bool StopSequence(string modId, string id);
public static List<SceneTimerInfo> Timers();
public static bool StartTimer(string modId, string id);
public static bool SetInterval(string modId, string id, float seconds);
```

- Reads return lists. A read that fails is logged once and returns an empty list.
- Writes return `false` for an empty mod id, an unknown id, a refused value, or a save that is loading or saving.
- `AddCounter` takes an amount of at least 1.
- `SetInterval` takes a positive number of seconds.

## Info classes

```csharp
public sealed class BranchInfo
{
    public string Id { get; }
    public bool Value { get; }
}

public sealed class CounterInfo
{
    public string Id { get; }
    public int Value { get; }
    public int Max { get; }
}

public sealed class StateMachineInfo
{
    public string Id { get; }
    public string Current { get; }
    public List<string> Names { get; }
}

public sealed class SequenceInfo
{
    public string Id { get; }
    public int Entries { get; }
    public bool Loop { get; }
}

public sealed class SceneTimerInfo
{
    public string Id { get; }
    public float Interval { get; }
    public bool Running { get; }
    public bool Disabled { get; }
}
```

## Example

```csharp
CounterInfo counter = SceneState.Counters()[0];
SceneState.AddCounter("com.example.mymod", counter.Id, 1);
```

## Behaviour

- `SetBranch` uses the game's own setter, so the branch's tests and listeners run, and they may fire the scene's outputs.
- `SetCounter` and `AddCounter` use the game's own counter methods. A counter that reaches its maximum fires its relays, as in the game.
- `SetState` only changes the current state name. It refuses a name the state machine does not have. It does not fire the state's relay.
- `StartSequence` starts the sequence from its first entry. `StopSequence` stops it.
- `StartTimer` starts the timer. `SetInterval` changes the interval and does not reset the timer.

## Not exposed

- Change events. The game raises no event for these values. A patch on each setter would be needed.
- Whether a sequence is running. The game keeps that flag private.

## Ceilings and risks

- Writing a branch or a counter can fire the scene's relays and move quests on. Use it with the same care as `SceneLogic.Fire`.
- The counter value is written into the save. The game's `CounterRelay` saves it. `SetCounter` refuses a negative value and logs the refusal once.

## Save loads

The values are the game's own and are saved by the game's scene logic. The framework keeps nothing.
