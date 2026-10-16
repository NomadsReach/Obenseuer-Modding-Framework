---
title: Obenseuer Modding Framework tutorial API (opening tutorial)
parent: API reference
nav_order: 85
---

# Obenseuer Modding Framework tutorial API (opening tutorial)

Experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.tutorial`. Logs go to `BepInEx\logs\obenseuer.moddingframework.tutorial.log`.

Everything not listed here is internal. Main thread only.

Named `TutorialGuide` because `Tutorial` is a game class.

Scope: the stage of the opening tutorial (look around, move, interact, menu), and whether a tutorial prompt is showing. The game's other prompts (building, cleaning, reload, headlight, inventory, map, tasks, tenement) make `Active` true, but they are not steps.

## Rules

- **Read-only.** The facade does not start, skip or finish a tutorial. It reads the game's own flags.
- **No `Complete()`.** The game has no public method that completes the tutorial. Each stage ends when the player does its action, and the game sets the next flag itself. `Tutorial.HideTutorial` hides the prompt and clears the building, cleaning, reload and headlight flags. It does not touch the opening stages, so the facade does not wrap it.
- **Bad arguments.** None. Every member takes no argument.

## API

```csharp
public static class TutorialGuide
{
    public static bool IsAvailable { get; }
    public static bool Active { get; }                      // a tutorial prompt is showing
    public static TutorialStep Step { get; }                // current opening-tutorial stage
    public static event Action<TutorialStepArgs> StepChanged;
}

public enum TutorialStep { None = 0, LookAround = 1, Move = 2, Interact = 3, Menu = 4 }

public sealed class TutorialStepArgs
{
    public TutorialStep Previous { get; }
    public TutorialStep Step { get; }
}
```

- `Step` is `None` before the tutorial starts, after it ends, and when no `Tutorial` exists (main menu, no save).
- `Active` is the game's prompt flag. The game clears it when the prompt is hidden, so it is false between prompts even while `Step` is not `None`.
- `StepChanged` fires once per change. The game changes its flags inside a coroutine (`ChangeTutorial`), after a short wait, so the facade checks the flags at the end of `Tutorial.Update` and the event can be later than the action that caused it. With no subscriber the check is skipped. The first subscriber starts from the step at that moment, so it gets no event for a change that happened before it subscribed.
- `IsAvailable` is `installed && !CorePatcher.IsDisabled(owner)`. `Step` and `Active` do not depend on it, but `StepChanged` does not fire unless it is true.
