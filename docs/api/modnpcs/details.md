---
title: Details
parent: Mod NPCs
grand_parent: People
nav_order: 10
---

# Details

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

- Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
- Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.modnpcs.log`.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only runs safely on the main thread (see the [glossary](../../glossary.md#main-thread)).

## What to check for and what comes back

- A call that can't do its job returns its plain "no" value and does not throw. `Spawn`, `SpawnSaved`, `SpawnWithLook` and `CharacterIdOf` return `0`, `SpawnExtra` and `ExtraObject` return `null`, and `Despawn`, `Clear` and `DespawnExtra` return `false`.
- The only errors are `ArgumentException` for a bad `modId`, `npcId`, `extraId`, `name`, `scene`, `position` or `yaw`.
- If the game itself fails while the framework works, you get the same plain "no" value. The first failure of each member goes to the log as an error. Nothing half-built is left behind.

## If something fails to set up

- This API does not change any game method when it starts.
- Its feature group, the name you will see in the log, is `obenseuer.moddingframework.modnpcs`. A feature group is the named set of the framework's changes for one feature.
- If it has been switched off, `ModNpcs.IsAvailable` is `false` and `Spawn` returns `0`.
