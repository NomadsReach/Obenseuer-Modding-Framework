---
title: Details
parent: Interaction and locks
grand_parent: Places and travel
nav_order: 4
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.interaction.log`.

Everything not listed in this reference is internal and may change without notice.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only works safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- It is safe to call any of it at any time. Off a save, or on the main menu, reads give `null`, `false` or `0`.
- Calls that change something return `true` or `false`. The only errors are `ArgumentNullException` (a kind of `ArgumentException`) for a null object, label or action, and `ArgumentException` for a bad mod id or a bad difficulty. `Remove` and `SetReason` require a mod id of 1 to 64 characters of letters, digits, `.`, `_` or `-`. `Add` only requires a non-empty one, so an id that `Remove` refuses can still be added.
- Your event handlers run inside the game's own call, one at a time, after the game's method has finished. If one of your handlers throws an error, the framework logs it and still runs the other handlers.
- Events fire during a save load too. The framework does not hold them back. A handler that should ignore them can check whether the game is busy loading or saving. <span class="tech">(`SaveData.Busy` is true while a save is loading or being written.)</span>

## If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when something is used or a lock changes. If a game update breaks one of them, the framework turns off all the events on this page, and writes error lines and a warning to the log. Its feature group is `obenseuer.moddingframework.interaction`, the name you will see in the log. A feature group is the named set of the framework's changes for one feature.

- `Interactions.IsAvailable` is `false` when the events are switched off, and `Locks.IsAvailable` gives the same value. Reading and changing things (`Add`, `Remove`, `Lock`, `Unlock` and the rest) keep working. Only the events stop. <span class="tech">(The changes are `InteractObjects.UseInteractable` and the lock methods (`Lock.Use`, `Lock._Lock`, `Lock._Unlock`, `Door.Lock`, `Door.Unlock`, `Door.ToggleLock`, `Lock.LockpickSuccessCallback`, `Lock.AbortLockpicking`). If any one is missing, all of them are removed and `CorePatcher.IsDisabled("obenseuer.moddingframework.interaction")` becomes true.)</span>
