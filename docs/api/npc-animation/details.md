---
title: Details
parent: NPC animation
grand_parent: People
nav_order: 8
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.npcanimation.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

## If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when something happens. This part of the API changes none of them. It uses the framework's own update that runs once each frame, and the scene load event, so there is nothing else to set up. Its name in the log is `obenseuer.moddingframework.npcanimation`.

## When you can call it

- Call everything from your mod's normal code (OnLoad, event handlers, Update), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- While a save is loading or being written, `Play`, `Stop`, `PlayOn` and `StopOn` give `false`. `StopAll` still stops your clips.
