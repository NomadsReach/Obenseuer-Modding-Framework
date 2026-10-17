---
title: Details
parent: World sky
grand_parent: Places and travel
nav_order: 7
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Source: `src/Api/WorldSky.cs`, `src/Api/WorldSkyPatches.cs`, `src/Api/SkyApply.cs` and `src/Api/SkyChannel.cs`.

Everything not listed in this reference is internal and may change without notice.

- The framework changes some of the game's methods when it starts, so it can write your sky values at the right moment.
- This page calls that set of changes a feature group. Its feature group is `obenseuer.moddingframework.sky`, the name you will see in the log.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only works safely on its main thread (see the [glossary](../../glossary.md#main-thread)).
- `WorldSky.IsAvailable` tells you whether your values will be applied. It is `false` when the framework could not set up the sky changes, or when the feature group was turned off. Then `Set` returns `false`.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If any of the changes fails, the feature group turns off and `WorldSky.IsAvailable` becomes `false`. `Current` can still be read. <span class="tech">(the framework patches `AzureSkyRenderController.UpdateShaderUniforms()` with a prefix, and each game writer with a prefix and a finalizer (the methods listed above). It subscribes to `Application.onBeforeRender` only while an override is set or a value is still held.)</span>
