---
title: Details
parent: Screen effects
grand_parent: Screen and UI
nav_order: 9
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts. This page calls each group of changes a feature group, and a feature group has a name that shows up in the log. This page uses two: `obenseuer.moddingframework.screenfx` for the field of view override, and `obenseuer.moddingframework.screenfx.blur` for the blur.

## When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- **It is safe to call any of it at any time, even on the main menu.** A call that needs a part of the game that is not there yet returns `false`.

## What to check for and what comes back

- **`ScreenFx.IsAvailable` tells you whether the field of view override works.** It is `false` when the framework could not set it up, or when the feature group was turned off. Shake, the fades, the death message, the vignette and the blur do not depend on it, and keep working.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the framework cannot change the game's camera field of view code, the field of view override stops and the rest keeps working.** `IsAvailable` becomes `false`.
  - Technical: it changes `SetCameraFOV.Update` and `SetCameraFOV.SetFOV`. If either is missing, the feature group `obenseuer.moddingframework.screenfx` is removed.
- **If the framework cannot change one of the game's blur methods, `Blur` returns `false`.** `ClearBlur` still runs, and the field of view override is not affected.
  - Technical: the feature group is `obenseuer.moddingframework.screenfx.blur`.
