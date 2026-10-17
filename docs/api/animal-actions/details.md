---
title: Details
parent: Animal actions
grand_parent: Animals and plants
nav_order: 5
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.animalactions.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

## When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game only works safely on its main thread (see the [glossary](../../glossary.md#main-thread)).

## If something fails to set up

- **This part of the framework does not change any game methods, so there is no feature group that can turn off.** It only listens for scene loads and registers its save file. If the save file cannot be registered, tame choices last until the player quits the game, and an error goes to the log.
