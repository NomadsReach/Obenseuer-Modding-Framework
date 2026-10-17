---
title: Details
parent: Custom skills
grand_parent: Player life and skills
nav_order: 6
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts, so it can show your skills' rows and banners. This page calls that a feature group. Its name, the one you will see in the log, is `obenseuer.moddingframework.customskills`.

## When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)). That includes the code that handles `LevelChanged`.
- **It is safe to call at any time.** Calls that change a skill return `false` while a save is loading or being written. Try again later.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the rows or banners cannot be set up, `IsAvailable` is `false` and the feature group is turned off.** The values still work, and `Register`, `Get`, `Set`, `Add` and `Clear` keep working, but no rows appear and `QueueBanner` returns `false`.
