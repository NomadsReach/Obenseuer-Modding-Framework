---
title: Details
parent: Recipes and crafting
grand_parent: Items and crafting
nav_order: 6
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts, so it can tell your mod when something is crafted, harvested or unlocked. This page calls each named set of changes a feature group. This page uses three: `obenseuer.moddingframework.crafting` (the craft events), `obenseuer.moddingframework.growing` (the harvest and pick events) and `obenseuer.moddingframework.recipes` (the `Recipes.Unlocked` event). These are the names you will see in the log. The recipe reads and `Recipes.Unlock` need no change to the game.

## When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- **It is safe to call at any time, even on the main menu.** With no save loaded, reads return an empty list, `null`, `0` or `false`.
- **The only call that changes the game is `Recipes.Unlock`.** It returns `false` while a save is loading or being written.
- **Nothing here throws an error to your code.** Each member catches its own errors and logs the first failure of each member.
- **Your event handlers run inside the game's own call, after the game method has run far enough to know the result.** If a handler throws an error, the framework logs it and still runs the other handlers.

## What to check for

- **`Crafting.IsAvailable`, `Plants.IsAvailable` and `Recipes.IsAvailable` tell you whether the events of each part work.** Each is `false` when the framework could not set up its events, or when its feature group was turned off. The reads are not affected.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If either craft change cannot be made, only `CraftStarted` and `Crafted` stop.** The two craft changes are made together, so one missing turns off the whole `obenseuer.moddingframework.crafting` group. The reads and `Crafting.All()` keep working.
- **`Plants.Harvested` and `Plants.Picked` share one feature group.** If its change cannot be made, both events stop and the rest keeps working.
- **`Recipes.Unlocked` has a feature group of its own.** If its change cannot be made, only that event stops. The recipe reads and `Unlock` keep working.
