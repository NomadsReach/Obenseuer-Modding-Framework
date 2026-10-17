---
title: Details
parent: UI
grand_parent: Screen and UI
nav_order: 8
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore.Ui`. Errors from a control go to the log of the mod that owns it, as `ui callback failed: ...`. A control that belongs to no mod logs to `BepInEx\logs\ObenseuerCore.log`.

Everything not listed in this reference is internal and may change without notice.

## If something fails to set up

The framework changes none of the game's methods for this part of the API, so there is no feature group for it and nothing that can fail to set up.

## When you can call it

- **Build and change your controls from your mod's normal code (OnLoad, event handlers, Update), not from a background thread you started.** The controls are Unity objects, and Unity only lets the main thread change them (see the [glossary](../../glossary.md#main-thread)).
