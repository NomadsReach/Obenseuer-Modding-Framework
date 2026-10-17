---
title: Details
parent: Trains
grand_parent: Places and travel
nav_order: 7
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Files: `src/Api/Trains.cs`, `src/Api/TrainsPatches.cs`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes some of the game's methods when it starts, so it can tell your mod when a train sets off, stops or crashes. The changes belong to two feature groups. `obenseuer.moddingframework.trains` covers `Departed` and `Arrived`, and `obenseuer.moddingframework.trains.crash` covers `Crashed`. These are the names you will see in the log.

## When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)). This goes for every member on this page, and for the code that handles the events.
- **It is safe to call any of it at any time, even on the main menu or while a save is loading.** If there is nothing to read yet, you get an empty list or `null`.
- **Your event handlers run inside the game's own call, right after the change.** If one of your handlers throws an error, the framework logs it and still runs the other handlers.
- **The framework does not hold back the events while a save is loading or being written.** A handler that should ignore those moments can check `SaveData.Busy`, which is `true` during a load or a save.

## What to check for and what comes back

- **The reading calls never throw an error.** `GetBranch` is the one exception: it throws an `ArgumentNullException` for a `null` object.
- **The writing calls throw an error only for a `null` object or a negative branch.** See the switches and loaders section above.
- **If the game itself fails while the framework works, you get an empty answer instead of an error.** The answer is the plain default (an empty list, `null`, `-1` or `false`), and the failure goes to the log as an error.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the set-off and stop events can't be set up, `Departed` and `Arrived` stop.** The feature group `obenseuer.moddingframework.trains` turns off, error lines and a warning go to the log, and `IsAvailable` is `false`. The reads and the writing calls keep working.
  - Technical: the game method involved is `TrackTrain.Update`.
- **If the crash event can't be set up, only `Crashed` stops.** The feature group `obenseuer.moddingframework.trains.crash` turns off.
  - Technical: the game method involved is `TrackTrain.Crash(GameObject, bool)`.
