---
title: Details
parent: NPC movement
grand_parent: People
nav_order: 5
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.npcmovement.log`.

Everything not listed in this reference is internal and may change without notice.

This page changes none of the game's methods, so it has no feature group and nothing here can turn off after a game update. `obenseuer.moddingframework.npcmovement` is only the name the log file uses.

## When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).

## If something fails to set up

- **There is nothing to set up.** The framework does one thing in the background for this page: once a frame it retries any release that had to wait, and drops claims whose NPC controller was destroyed.
  - Technical: `NpcMovement.Tick` is internal. The framework calls it once per frame from its update loop.
