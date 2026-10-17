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

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

Every value has an id. The id is the id the level designers gave the object, or its place in the scene when it has none. These are the same ids that `SceneLogic` uses. Every change takes your mod's id as `modId` (see [Mod id](../glossary.md#mod-id)).

### Reading and changing the scene's values

```csharp
public static class SceneState
{
    public static List<BranchInfo> Branches();                                   // every true or false switch in the scene
    public static bool SetBranch(string modId, string id, bool value);           // sets a switch; true when it is now the value you asked for
    public static List<CounterInfo> Counters();                                  // every counter in the scene
    public static bool SetCounter(string modId, string id, int value);           // sets a counter to a value; false when refused
    public static bool AddCounter(string modId, string id, int amount);          // adds to a counter; false when refused
    public static List<StateMachineInfo> States();                               // every state machine in the scene
    public static bool SetState(string modId, string id, string stateName);      // moves a state machine to a named state
    public static List<SequenceInfo> Sequences();                                // every sequence in the scene
    public static bool StartSequence(string modId, string id);                   // starts a sequence from its first entry
    public static bool StopSequence(string modId, string id);                    // stops a sequence
    public static List<SceneTimerInfo> Timers();                                 // every timer in the scene
    public static bool StartTimer(string modId, string id);                      // starts a timer
    public static bool SetInterval(string modId, string id, float seconds);      // changes how long a timer waits between ticks
}

public sealed class BranchInfo    // a true or false switch
{
    public string Id { get; }       // the switch's id
    public bool Value { get; }      // true or false
}

public sealed class CounterInfo    // a counter
{
    public string Id { get; }       // the counter's id
    public int Value { get; }       // its value now
    public int Max { get; }         // the value at which it fires its reactions
}

public sealed class StateMachineInfo    // something that is in one of several named states
{
    public string Id { get; }               // the machine's id
    public string Current { get; }          // the name of the state it is in now
    public List<string> Names { get; }      // the names of all its states
}

public sealed class SequenceInfo    // a list of steps that run one after another
{
    public string Id { get; }       // the sequence's id
    public int Entries { get; }     // how many steps it has
    public bool Loop { get; }       // true when it starts again after the last step
}

public sealed class SceneTimerInfo    // a timer
{
    public string Id { get; }           // the timer's id
    public float Interval { get; }      // how many seconds it waits between ticks
    public bool Running { get; }        // true when it is running
    public bool Disabled { get; }       // true when the scene has switched it off
}
```

- **The read calls give you a list of what is in the scene you are in.** If a read fails inside the game, the failure is written to the log once and you get an empty list.
- **A change call returns `false` when it did nothing.** That happens for an empty mod id, an id that is not in the scene, a value the call refuses, and while a save is loading or being written.
- **`SetBranch` flips the switch the way the game does, so everything that is wired to it reacts.** The scene's tests and listeners run, and they can set off the scene's other reactions. It returns `true` when the switch has the value you asked for.
  - Technical: it uses the game's own `logic_branch.IsTrue` setter.
- **`SetCounter` and `AddCounter` change a counter the way the game does.** A counter that reaches its maximum fires its reactions, as it would in the game. `SetCounter` refuses a negative value, and writes that refusal to the log once. `AddCounter` needs an amount of at least 1.
  - Technical: they use the game's own `CounterRelay.SetCounterValue` and `IncreaseCounter`. Its reactions are its relays.
- **A counter's value is saved with the player's save.** The game keeps and saves it, so a change you make stays after a reload.
  - Technical: `CounterRelay` saves its value.
- **`SetState` only changes which state the machine says it is in.** It refuses a name the machine does not have (it returns `false`), and an empty name. It does not fire that state's own reactions.
  - Technical: it calls `logic_state.ChangeState`, and does not fire the state's relay.
- **`StartSequence` starts the sequence from its first entry, and `StopSequence` stops it.** Both return `true` when the sequence exists. They do not tell you whether it was running.
- **`StartTimer` starts the timer.** `SetInterval` changes the number of seconds it waits, and does not restart it. The seconds must be above 0.
- **Writing a branch or a counter can set off the scene's reactions and move quests on.** Use it with the same care as `SceneLogic.Fire`.

### What you can't do (yet)

- **You can't be told when one of these values changes.** The game raises no event for them, so the only way to notice a change is to read the values again later.
  - Technical: the framework would need a patch on every setter.
- **You can't tell whether a sequence is running.** The game keeps that fact private.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.scenestate.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

### When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- **It is safe to call any of it at any time, even on the main menu or while a save is loading.** If there is nothing to read you get an empty list, and a change call gives `false`.
- **This API has no events.**

### If something fails to set up

This API does not change any of the game's methods, so there is no feature group to turn off. It has nothing to fail at start.

### Saving

The values are the game's own, and the game's scene scripting saves them. The framework keeps nothing of its own.
