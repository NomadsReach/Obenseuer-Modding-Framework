---
title: Details
parent: Crafting stations
grand_parent: Items and crafting
nav_order: 10
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log names: `obenseuer.moddingframework.craftingspeed`, `obenseuer.moddingframework.craftingquality` and `obenseuer.moddingframework.craftingstations`.

Everything not listed in this reference is internal and may change without notice. This page is experimental and may change too.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- The calls do not throw errors. A call that cannot do its job returns `false`, and a failure inside the game goes to the log once.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- Speed and quality each have their own feature group. They are `obenseuer.moddingframework.craftingspeed` and `obenseuer.moddingframework.craftingquality`, the names you will see in the log.
- If one of them cannot be set up, its availability flag (`SpeedFactorsAvailable` or `QualityFactorAvailable`) is `false`, error lines and a warning go to the log, and its setter returns `false`.
- `CraftingStations` changes none of the game's methods, so it has no feature group and nothing to set up. It calls the game's own methods and reports whether the station changed.
