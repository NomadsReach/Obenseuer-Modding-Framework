---
title: Details
parent: Debug
grand_parent: Framework basics
nav_order: 7
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. The debug trace has no public code members. You use it through the config and the console command.

Everything not listed in this reference is internal and may change without notice.

This page makes no changes to the game's methods, so it has no feature group.

## When you can call it

- The trace is a switch, not code you call, so there is no thread rule for it. The console command and the Mods menu run it for you.
- Lines from background threads are marked. A call that comes from a thread other than the main thread starts with `[thread N]`. The main thread is your mod's normal code, such as `OnLoad` and event handlers (see the [glossary](../../glossary.md#main-thread)).
