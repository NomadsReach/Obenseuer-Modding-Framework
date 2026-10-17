---
title: Details
parent: Fuel
grand_parent: Money and shops
nav_order: 5
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.fuel.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes one of the game's methods when it starts, so it can tell your mod when the player pays for fuel. This page calls that a feature group: a named set of the framework's changes for one feature. Its feature group is `obenseuer.moddingframework.fuel`, the name you will see in the log.

## When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- **It is safe to call at any time, even on the main menu.** With no save loaded, `FuelStations.All()` returns an empty list.
- **Nothing here changes the game, and nothing throws an error to your code.** If the game itself fails while the framework reads, you get an empty answer, and the first failure of each member goes to the log.
- **Your event handlers run inside the game's own call, one at a time.** If a handler throws an error, the framework logs it and still runs the other handlers.

## What to check for

- **`FuelStations.IsAvailable` tells you whether `FuelBought` works.** It is `false` when the framework could not set up the event, or when its feature group was turned off. `FuelStations.All()` and `FuelTypes` keep working when it is `false`.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the change to `GasStation.PayFuelOc` cannot be made, only `FuelBought` stops.** `FuelStations.IsAvailable` becomes `false` and error lines and a warning go to `BepInEx\logs\obenseuer.moddingframework.fuel.log`. `FuelStations.All()` and `FuelTypes` keep working.
  - Technical: the warning reads "fuel hooks incomplete, fuel events are disabled". `CorePatcher.IsDisabled("obenseuer.moddingframework.fuel")` becomes `true`.
