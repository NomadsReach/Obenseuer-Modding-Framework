---
title: Tutorial
parent: Framework basics
grand_parent: API reference
nav_order: 12
---

# Tutorial

Status: experimental.

## What it is

When a new game starts, the game teaches the player the basics in an opening tutorial: look around, move, use something, and open the menu. This API lets your mod see which of those steps the player is on and know when it changes. It can only read. It cannot start, skip or change the tutorial, and it cannot show the game's key-binding tutorial prompts.

The class is called `TutorialGuide`, because `Tutorial` is already the name of a class in the game.

## When to use it

- Wait until the opening tutorial is over before your mod shows its own messages.
- Run code when the player moves on from one tutorial step to the next.
- Check if a tutorial prompt is on the screen right now.

## Quick example

This mod writes a log line each time the opening tutorial moves to a new step. It also logs the step the player is on once a save is loaded.

```csharp
using ObenseuerCore;

[ModInfo("example.tutorialwatch", "Tutorial Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TutorialWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        TutorialGuide.StepChanged += args => Log.Info("The tutorial went from " + args.Previous + " to " + args.Step + ".");
        Events.WhenReady(() => Log.Info("The tutorial step is " + TutorialGuide.Step + "."));
    }
}
```

`StepChanged` is an event: the framework calls your code each time it happens. `TutorialStep.None` means no opening step is running. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.

## Common tasks

Wait for the opening tutorial to be over before your mod does something. When `Step` is `None`, no opening step is running.

```csharp
if (TutorialGuide.Step == TutorialStep.None) Hud.ShowHint(Mod.Id, "Welcome", new[] { "The tutorial is over." });
```

Check whether a tutorial prompt is showing right now.

```csharp
bool prompt = TutorialGuide.Active;
```

Check that the step event works in this game version.

```csharp
if (!TutorialGuide.IsAvailable) Log.Warn("Tutorial step events are off in this game version.");
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

### Tutorial steps

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TutorialGuide</code> <span>read the opening tutorial step and listen for changes</span></p>
Use `Step` to read the opening tutorial step, and `StepChanged` to hear when it moves on.

<div class="api-pair" markdown="1">

```csharp
public static class TutorialGuide
{
    public static bool IsAvailable { get; }
    public static bool Active { get; }
    public static TutorialStep Step { get; }
    public static event Action<TutorialStepArgs> StepChanged;
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when the `StepChanged` event works (see Details). |
| `Active` | `bool` | `true` while a tutorial prompt is showing. |
| `Step` | `TutorialStep` | The opening tutorial step the player is on. |

| Event | Gives you | When it fires |
|---|---|---|
| `StepChanged` | `TutorialStepArgs` | The tutorial has just moved to another step. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TutorialStep</code> <span>the opening tutorial steps</span></p>
`Step` and `StepChanged` use this for the opening tutorial. The framework works the step out from the game's own look, move, interact and menu flags.

<div class="api-pair" markdown="1">

```csharp
public enum TutorialStep
{
    None = 0,
    LookAround = 1,
    Move = 2,
    Interact = 3,
    Menu = 4
}
```

| Value | Meaning |
|---|---|
| `None` | No opening step is running. |
| `LookAround` | The player is learning to look around. |
| `Move` | The player is learning to move. |
| `Interact` | The player is learning to use things. |
| `Menu` | The player is learning to open the menu. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TutorialStepArgs</code> <span>what changed</span></p>
`StepChanged` gives you this. `Previous` is the step before, and `Step` is the step now.

<div class="api-pair" markdown="1">

```csharp
public sealed class TutorialStepArgs
{
    public TutorialStep Previous { get; }
    public TutorialStep Step { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Previous` | `TutorialStep` | The step before the change. |
| `Step` | `TutorialStep` | The step after the change. |

</div>
</div>

- `Step` is `None` before the tutorial starts, after it is over, and when there is no tutorial at all. There is no tutorial on the main menu, or when no save is loaded. <span class="tech">(It is `None` when `Tutorial.instance` is null. Otherwise it reads the game's opening-tutorial flags in the order look around, move, use, menu.)</span>
- `Active` is `true` while a tutorial prompt is on the screen. The game turns it off when the prompt is hidden, so it is `false` between prompts, even while `Step` is not `None`. <span class="tech">(It reads the game's `Tutorial.TutorialActive` flag.)</span>
- The game's other prompts make `Active` `true`, but they are not steps. These are the prompts for building, cleaning, reloading, the headlight, the inventory, the map, tasks and the tenement. `Step` does not change for them.
- `StepChanged` fires once for each change of step. It can fire a little after the player did the action, because the game waits a short moment before it moves on. <span class="tech">(The game changes its flags inside a coroutine (`ChangeTutorial`) after a short wait. The framework checks the flags at the end of `Tutorial.Update`, so the event comes from there.)</span>
- You do not get an event for a change that happened before you subscribed. Your first handler starts from the step the tutorial is on at that moment. <span class="tech">(When the first handler is added, the framework records the current `Step`. With no handler, the check is skipped.)</span>

### What you can't do (yet)

- You can't start, skip or finish the tutorial. The game has no call that finishes it. Each step ends when the player does what it asks, and the game moves to the next step by itself. <span class="tech">(`Tutorial.HideTutorial` hides the prompt and clears the building, cleaning, reload and headlight flags, but it does not touch the opening steps, so the framework has no call for it.)</span>
- You can't show or hide a tutorial prompt. The framework offers no call to display one.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.tutorial.log`.

Everything not listed in this reference is internal and may change without notice.

### If something fails to set up

The framework changes one of the game's methods when it starts, so it can tell your mod when the tutorial step changes. If a game update breaks it, the `StepChanged` event turns off, and the framework writes error lines and a warning to the log. The change belongs to a feature group, `obenseuer.moddingframework.tutorial`, the name you will see in the log.

- `TutorialGuide.IsAvailable` tells you whether `StepChanged` works. It is `false` when the framework could not set up the change, or when the feature group was turned off. `Step` and `Active` do not depend on it, but `StepChanged` does not fire unless it is `true`. <span class="tech">(The framework also needs five of the game's private `Tutorial` fields. If any of them is missing, `IsAvailable` is `false`, and a read that cannot find its field falls back to `None` or `false`.)</span>

### When you can call it

- Call everything from your mod's normal code (OnLoad, event handlers, Update), not from a background thread you started. The game only works safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call any of it at any time. Nothing throws an error, because no call takes an argument. If the game fails while the framework reads, you get `None` or `false`.
- Your `StepChanged` handlers run inside the game's own update, one at a time. If a handler throws an error, the framework logs it and still runs the other handlers.
