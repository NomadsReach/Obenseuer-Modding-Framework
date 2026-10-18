---
title: Details
parent: Renovations
grand_parent: Places and travel
nav_order: 7
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.renovations.log`.

Everything not listed in this reference is internal and may change without notice.

## If something fails to set up

The framework changes one of the game's methods when it starts, so it can tell your mod when a renovation is finished. If a game update breaks it, only the `Finished` event turns off, and the framework writes error lines and a warning to the log. The reads and `Start` keep working. The change belongs to a feature group, `obenseuer.moddingframework.renovations`, the name you will see in the log.

- `Renovations.IsAvailable` tells you whether `Finished` works. It is `false` when the change could not be made or the feature group was turned off. <span class="tech">(The changed method is `TenementUpgradeProgress.FinishUpgrade`.)</span>

## When you can call it

- Call everything from your mod's normal code (OnLoad, event handlers, Update), not from a background thread you started. The game only runs safely on the main thread (see the [glossary](../../glossary.md#main-thread)).
- It is safe to call any of it at any time, even on the main menu. With no save loaded, `All()` gives an empty list and the other reads give `0` or `null`.
- `Start` gives `false` instead of throwing, for every failure.
- Your `Finished` handlers run inside the game's own call, after the game has finished the renovation, one at a time. If a handler throws an error, the framework logs it and still runs the other handlers.

## Save loads

- The framework does not hold back the `Finished` event while a save loads. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is `true` while a save is loading or being written.
