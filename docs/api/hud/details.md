---
title: Details
parent: HUD
grand_parent: Screen and UI
nav_order: 9
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.hud.log`.

Everything not listed in this reference is internal and may change without notice. This page is experimental and may change too.

The framework changes some of the game's methods when it starts, so it can add console commands and tell your mod about task item changes. This page calls each group of those changes a feature group. Each group has a name that shows up in the log. This page uses two: `obenseuer.moddingframework.hud` (the console commands and the task item event) and `obenseuer.moddingframework.difficulty` (the difficulty factors).

## When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)). That includes the code that handles `TaskItems.Changed`, and the `run` code of a console command.
- **It is safe to call at any time, even on the main menu, while a save is loading, or with no save loaded.** If there is nothing to read yet, you get a neutral answer: `null`, `0`, an empty array, an empty list or `false`. A call that changes something gives back `false` when the part of the game it needs is missing.
- **Some calls throw an error when you pass a bad value.**
  - An invalid mod id, or a null or empty name, text, title or body, throws an `ArgumentException`. A mod id is 1 to 64 characters of letters, digits, `.`, `_` or `-`. The exception is `Print`, which only throws for a null `text`.
  - A null `lines` or `run` throws an `ArgumentNullException`.
  - A negative fade time throws an `ArgumentException`.
  - Ids that name game things, such as an achievement id or a note id, never throw. An unknown id gives `null`, `0` or `false`.
- **If the game itself fails during a call, you get the plain default answer instead of an error.** The first failure of each member goes to the log. Later failures of that member are not logged again.
- **Unlocking an achievement cannot be undone from a mod.** `Achievement.Trigger`, `AddProgress` and `SetProgress` unlock the real Steam achievement and store its stat. They give back `false` while a save is loading or being written. Everything else on this page only reads, or changes the screen for this session.
  - Technical: they go through the game's own trigger, and the save check is `SaveData.Busy`.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the console setup fails, `ConsoleCommands.IsAvailable` is `false` and `Register` gives back `false`.** `Print`, `Open` and `IsOpen` talk to the game's console directly, so they do not depend on that setup.
  - Technical: the methods involved are `DeveloperConsole.HandleInput` and `DeveloperConsole.PrintValidCommands`.
- **If the task item setup fails, `TaskItems.Changed` stops firing.** `All`, `Has`, `Amount`, `Add` and `Remove` keep working.
  - Technical: the methods involved are `TaskItemsManager.AddTaskItem` and `TaskItemsManager.RemoveItem`.
- **If the difficulty setup fails, `GameDifficulty.FactorsAvailable` is `false` and `SetFactor` gives back `false`.** `Current()` keeps working.
