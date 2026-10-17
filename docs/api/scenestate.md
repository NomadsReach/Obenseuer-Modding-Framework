---
title: Scene state
parent: Places and travel
grand_parent: API reference
nav_order: 12
---

# Scene state

Status: experimental.

## What it is

Each scene (a level of the game) keeps some values that its own scripting uses to remember things: switches that are true or false (branches), counters, state machines that are in one of several named states, sequences of steps, and timers. This API lists those values in the scene you are in and lets a mod change them. A scene's quest steps and doors are often built from these values, so changing one can set off other things in the scene. For the relays that pass signals between them, see [SceneLogic](scenelogic.md).

## When to use it

- Read a scene's counter or switch to find out how far something has got.
- Flip a switch, add to a counter or change a state to make the scene react.
- Start or stop a sequence that a level designer built, or start one of the scene's timers.

## Quick example

This mod adds 1 to the first counter in the scene once a save is loaded, and logs the counter's value before and after.

```csharp
using ObenseuerCore;

[ModInfo("example.scenecounter", "Scene Counter", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class SceneCounter : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            var counters = SceneState.Counters();
            if (counters.Count == 0) return;
            CounterInfo counter = counters[0];
            bool added = SceneState.AddCounter(Mod.Id, counter.Id, 1);
            Log.Info(counter.Id + " was " + counter.Value + " of " + counter.Max + ", add worked: " + added);
        });
    }
}
```

`WhenReady` runs your code once a save is loaded (see [Using events](../guides/getting-started/using-events.md)). Adding to a counter can set off the scene's own reactions, as it does in the game, so try this on a save you can lose.

## Common tasks

Flip a branch to true.

```csharp
foreach (BranchInfo branch in SceneState.Branches())
    Log.Info(branch.Id + " is " + branch.Value);
bool set = SceneState.SetBranch(Mod.Id, "some-branch-id", true);
```

Move a state machine to another state. The name must be one of the machine's own state names.

```csharp
foreach (StateMachineInfo machine in SceneState.States())
    Log.Info(machine.Id + " is in " + machine.Current + " (it can be: " + string.Join(", ", machine.Names.ToArray()) + ")");
bool moved = SceneState.SetState(Mod.Id, "some-state-id", "SomeStateName");
```

Start or stop a sequence, and start a timer or change how long it waits.

```csharp
SceneState.StartSequence(Mod.Id, "some-sequence-id");
SceneState.StopSequence(Mod.Id, "some-sequence-id");
SceneState.StartTimer(Mod.Id, "some-timer-id");
SceneState.SetInterval(Mod.Id, "some-timer-id", 30f);
```

## Reference

This is the full list of what the API gives you. Every value has an id: the id the level designers gave the object, or its place in the scene when it has none. These are the same ids that `SceneLogic` uses, and every change takes your mod's id as `modId` (see [Mod id](../glossary.md#mod-id)). Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

### Reading and changing the scene's values

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SceneState</code> <span>read and change the scene's values</span></p>

This is the class to start from. The five list methods read the scene's values, and the other methods change one of them by its id.

<div class="api-pair" markdown="1">

```csharp
public static class SceneState
{
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
}
```

| Method | Returns | What it does |
|---|---|---|
| `Branches()` | `List<BranchInfo>` | Every true or false switch in the scene. |
| `SetBranch(modId, id, value)` | `bool` | Sets a switch. Returns `true` when it is now the value you asked for. |
| `Counters()` | `List<CounterInfo>` | Every counter in the scene. |
| `SetCounter(modId, id, value)` | `bool` | Sets a counter to a value. Returns `false` when refused. |
| `AddCounter(modId, id, amount)` | `bool` | Adds to a counter. Returns `false` when refused. |
| `States()` | `List<StateMachineInfo>` | Every state machine in the scene. |
| `SetState(modId, id, stateName)` | `bool` | Moves a state machine to a named state. |
| `Sequences()` | `List<SequenceInfo>` | Every sequence in the scene. |
| `StartSequence(modId, id)` | `bool` | Starts a sequence from its first entry. |
| `StopSequence(modId, id)` | `bool` | Stops a sequence. |
| `Timers()` | `List<SceneTimerInfo>` | Every timer in the scene. |
| `StartTimer(modId, id)` | `bool` | Starts a timer. |
| `SetInterval(modId, id, seconds)` | `bool` | Changes how long a timer waits between ticks. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BranchInfo</code> <span>a true or false switch</span></p>

`SceneState.Branches()` gives you one of these for each switch in the scene. Pass its `Id` to `SetBranch`. <span class="tech">(It is filled from the game's `logic_branch` objects.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class BranchInfo
{
    public string Id { get; }
    public bool Value { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The switch's id. |
| `Value` | `bool` | `true` or `false`. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CounterInfo</code> <span>a counter</span></p>

`SceneState.Counters()` gives you one of these for each counter. `Max` is the value at which the counter fires its reactions, and `SetCounter` and `AddCounter` take its `Id`. <span class="tech">(It is filled from the game's `CounterRelay` objects.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class CounterInfo
{
    public string Id { get; }
    public int Value { get; }
    public int Max { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The counter's id. |
| `Value` | `int` | Its value now. |
| `Max` | `int` | The value at which it fires its reactions. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>StateMachineInfo</code> <span>something in one of several named states</span></p>

`SceneState.States()` gives you one of these for each state machine. Pass one of its `Names` to `SetState` to move the machine to that state. <span class="tech">(It is filled from the game's `logic_state` objects.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class StateMachineInfo
{
    public string Id { get; }
    public string Current { get; }
    public List<string> Names { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The machine's id. |
| `Current` | `string` | The name of the state it is in now. |
| `Names` | `List<string>` | The names of all its states. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SequenceInfo</code> <span>a list of steps that run one after another</span></p>

`SceneState.Sequences()` gives you one of these for each sequence. Pass its `Id` to `StartSequence` or `StopSequence`. <span class="tech">(It is filled from the game's `Logic_Sequence` objects.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class SequenceInfo
{
    public string Id { get; }
    public int Entries { get; }
    public bool Loop { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The sequence's id. |
| `Entries` | `int` | How many steps it has. |
| `Loop` | `bool` | `true` when it starts again after the last step. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SceneTimerInfo</code> <span>a timer</span></p>

`SceneState.Timers()` gives you one of these for each timer. Pass its `Id` to `StartTimer` or `SetInterval`. <span class="tech">(It is filled from the game's `logic_timer` objects.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class SceneTimerInfo
{
    public string Id { get; }
    public float Interval { get; }
    public bool Running { get; }
    public bool Disabled { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The timer's id. |
| `Interval` | `float` | How many seconds it waits between ticks. |
| `Running` | `bool` | `true` when it is running. |
| `Disabled` | `bool` | `true` when the scene has switched it off. |

</div>
</div>

#### Things to know

- The read calls give you a list of what is in the scene you are in. If a read fails inside the game, the failure is written to the log once and you get an empty list.
- A change call returns `false` when it did nothing. That happens for an empty mod id, an id that is not in the scene, a value the call refuses, and while a save is loading or being written.
- `SetBranch` flips the switch the way the game does, so everything that is wired to it reacts. The scene's tests and listeners run, and they can set off the scene's other reactions. It returns `true` when the switch has the value you asked for. <span class="tech">(It uses the game's own `logic_branch.IsTrue` setter.)</span>
- `SetCounter` and `AddCounter` change a counter the way the game does. A counter that reaches its maximum fires its reactions, as it would in the game. `SetCounter` refuses a negative value, and writes that refusal to the log once. `AddCounter` needs an amount of at least 1. <span class="tech">(They use the game's own `CounterRelay.SetCounterValue` and `IncreaseCounter`. Its reactions are its relays.)</span>
- A counter's value is saved with the player's save. The game keeps and saves it, so a change you make stays after a reload. <span class="tech">(`CounterRelay` saves its value.)</span>
- `SetState` only changes which state the machine says it is in. It refuses a name the machine does not have (it returns `false`), and an empty name. It does not fire that state's own reactions. <span class="tech">(It calls `logic_state.ChangeState`, and does not fire the state's relay.)</span>
- `StartSequence` starts the sequence from its first entry, and `StopSequence` stops it. Both return `true` when the sequence exists. They do not tell you whether it was running.
- `StartTimer` starts the timer. `SetInterval` changes the number of seconds it waits, and does not restart it. The seconds must be above 0.
- Writing a branch or a counter can set off the scene's reactions and move quests on. Use it with the same care as `SceneLogic.Fire`.

### What you can't do (yet)

- You can't be told when one of these values changes. The game raises no event for them, so the only way to notice a change is to read the values again later. <span class="tech">(The framework would need a patch on every setter.)</span>
- You can't tell whether a sequence is running. The game keeps that fact private.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.scenestate.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

### When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call any of it at any time, even on the main menu or while a save is loading. If there is nothing to read you get an empty list, and a change call gives `false`.
- This API has no events.

### If something fails to set up

This API does not change any of the game's methods, so there is no feature group to turn off. It has nothing to fail at start.

### Saving

The values are the game's own, and the game's scene scripting saves them. The framework keeps nothing of its own.
