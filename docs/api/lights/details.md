---
title: Details
parent: Lights
grand_parent: Objects and devices
nav_order: 5
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.lights.log` and `BepInEx\logs\obenseuer.moddingframework.lights.multistate.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts, so it can tell your mod when a light is switched. This page calls each named set of changes a feature group. This page uses two: `obenseuer.moddingframework.lights` (simple lights, the `Toggled` event) and `obenseuer.moddingframework.lights.multistate` (multi-state lights, the `StateChanged` event). These are the names you will see in the log.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../../glossary.md#main-thread)).
- It is safe to call at any time, even on the main menu. With no save loaded, lists are empty, `Get` returns `null`, the state reads return `-1`, and the switching calls return `false`.
- Nothing here throws an error to your code. A `null` or empty id finds nothing. If the game itself fails while the framework works, you get the same plain answer, and the first failure of each member goes to the log.
- `SetOn` and `SetState` return `false` while a save is loading or being written.
- Your event handlers run inside the game's own call, one at a time. If a handler throws an error, the framework logs it and still runs the other handlers.

## What to check for

- `Lights.IsAvailable` tells you whether the events work. It is `false` when the framework could not set up one of its light changes, or when the `obenseuer.moddingframework.lights` feature group was turned off. It reads that group only. The multi-state group is turned off only when its own change fails, and that failure already makes `IsAvailable` `false`.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework turns off the [feature group](../../glossary.md#feature-group) that depends on it and writes error lines to its log file. The rest of the framework keeps working. The reads and the switching calls keep working. Only the event of the missing group stops: `Toggled` for the simple lights, `StateChanged` for the multi-state lights. The other event keeps firing.

- Four changes are made in total. Three are on simple lights (`turnLightOn`, `turnLightOff` and `TurnLightOffFade`), and one is on multi-state lights (`SetState`). Each failure is logged to the log of its own group, for example `BepInEx\logs\obenseuer.moddingframework.lights.multistate.log`. <span class="tech">(If an install throws, for example from a mistyped handler name, both groups are turned off.)</span>
