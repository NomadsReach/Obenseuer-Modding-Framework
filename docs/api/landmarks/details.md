---
title: Details
parent: Landmarks
grand_parent: Places and travel
nav_order: 7
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts, so it can tell your mod when a landmark is added and keep your landmarks across saves. This page calls that a feature group. Its name, the one you will see in the log, is `obenseuer.moddingframework.landmarks`.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../../glossary.md#main-thread)). That includes the code that handles `Added`.
- It is safe to call at any time. With no map scene or no save loaded, `All` lists the landmarks in memory with `Owned` set to `false`, and `Has` and `Unlock` give back `false`.
- Nothing throws. A null or empty `id` finds nothing.
- `Unlock` and `Place` give back `false` while a save is loading or being written. <span class="tech">(They check `SaveData.Busy`.)</span>
- `Added` is not held back while a save loads. If your handler should ignore landmarks added while a save loads, check `SaveData.Busy`, which is `true` while a save is loading or being written.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework turns off the [feature group](../../glossary.md#feature-group) that depends on it, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If one of the three game changes cannot be made, or the landmark save file cannot be registered, the feature group is turned off and `Landmarks.IsAvailable` is `false`. The reads and `Unlock` keep working. `Added` stops, and `Create`, `Mark` and the restore on load stop too. <span class="tech">(The changes are a postfix on `MapController.AddLandMark`, a prefix and finalizer on `SaveController.SaveGame`, and a postfix on `MapController.OnLoadingGame`.)</span>
