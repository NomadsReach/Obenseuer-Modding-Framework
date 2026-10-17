---
title: Details
parent: Mod NPC routines
grand_parent: People
nav_order: 4
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.modnpcroutines.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

## When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- **It is safe to call any of it at any time, even on the main menu or while a save is loading.** If the call can't be done yet, it returns `false` (or `Away` for `Current`) and changes nothing.
- **Your `StepChanged` handler runs inside the framework's own check, one at a time.** If one of your handlers throws an error, the framework logs it and still runs the other handlers.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **Orders and routines depend on the same feature group as mod NPCs, `obenseuer.moddingframework.modnpcs`.** This is the name you will see in the log. If that group is turned off, `ModNpcRoutines.IsAvailable` is `false`, every order and `Set` return `false`, and nothing is spawned. The routines' own messages go to `BepInEx\logs\obenseuer.moddingframework.modnpcroutines.log`.
  - Technical: `ModNpcRoutines.IsAvailable` returns `ModNpcs.IsAvailable`. Routines install no game changes of their own.
