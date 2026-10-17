---
title: Details
parent: Levels
grand_parent: Places and travel
nav_order: 4
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.levels.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes one game method when it starts, so it can tell your mod when the game is about to change level. This page calls that a feature group: its name is `obenseuer.moddingframework.levels`, the name you will see in the log.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)). This goes for the code that handles `Changing` too.
- It is safe to call any of it at any time, even on the main menu or while a save is loading. With no scene to look in, `All()` gives an empty list, `Get` gives `null`, and `SetLocked`, `Use` and `ChangeTo` give `false`. Those three also give `false` while a save is loading or being written.

## What to check for and what comes back

- `Levels.IsAvailable` tells you whether `Changing` works. It is `false` when the framework could not set up the event, or when the feature group was turned off. The reads, `SetLocked`, `Use` and `ChangeTo` keep working.
- Nothing here throws an error to you. A `null` or unknown id finds nothing.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the framework cannot change the game's level change method, `Changing` stops and the rest keeps working. `IsAvailable` becomes `false`. <span class="tech">(The method is `Changelevel.ChangeLevel`.)</span>
