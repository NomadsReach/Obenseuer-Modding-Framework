---
title: Details
parent: Audio
grand_parent: Screen and UI
nav_order: 10
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.audio.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- It is safe to call any of it at any time, even on the main menu or before a save is loaded. If the game part that a member reads is missing, you get `0`, `null` or an empty list. A play call gives `false`.
- This API has no events. It only reads the game's sound state and plays sounds.

## What to check for and what comes back

- Wrong arguments throw an error. A name that is `null` or empty, a clip that is `null`, an unknown pickup or library name, or a volume below 0 or above 1 throws `ArgumentException`. Everything else gives its plain default (`false`, `0`, `null` or an empty list).
- If the game itself fails while the framework works, you get that plain default instead of an error. The first failure of each member is written to the log as an error. Later failures of that member are not logged again.
- Your sound goes through the game's volume channels. `PlayClip`, `PlayClipAt`, `PlayLibrary` and `PlayLibraryAt` use the game's `AudioMixer`, so the player's Master, Sound and Music sliders apply to them.

## If something fails to set up

This API does not change any of the game's methods, so there is no feature group to turn off. It has nothing to fail at start.
