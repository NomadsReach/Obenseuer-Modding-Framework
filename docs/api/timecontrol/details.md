---
title: Details
parent: Time control
grand_parent: Places and travel
nav_order: 4
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.timecontrol.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes one game method when it starts, so it can scale the clock. This page calls that a feature group: its name is `obenseuer.moddingframework.timecontrol`, the name you will see in the log.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)). This goes for the code that handles the events too.
- It is safe to call any of it at any time, even on the main menu. If there is nothing to read, you get `0`, `1` or `false`.

## What to check for and what comes back

- `TimeControl.IsAvailable` tells you whether the clock speed works. It is `false` when the framework could not set it up, or when the feature group was turned off. Then `TimeScale` stays at `1`. `In`, `Cancel`, `MinutePassed` and `SecondsPassed` keep working.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the framework cannot change the game's day length method, the clock speed stops working and the rest keeps working. `IsAvailable` becomes `false`. <span class="tech">(The method is `AzureTimeController.SetNewDayLength(float)`. The framework adds a prefix to it.)</span>
