---
title: Details
parent: Contests
grand_parent: Player life and skills
nav_order: 5
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.contests.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts, so it can tell your mod when a contest starts and ends and change a rat fight's winnings. This page calls each set of changes for one feature a feature group. Each group has a name that shows up in the log. This page uses two: `obenseuer.moddingframework.contests` (the `Started` and `Finished` events) and `obenseuer.moddingframework.contests.payout` (the rat fight payout factors).

## When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game only works safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)). This includes `OpponentTolerance` and `GiveUp`.
- **It is safe to call any of it at any time.** On the main menu or with no save loaded, `Active()` and `Participants()` give empty lists and `LastResult()` gives `null`. Nothing throws an error to you. A failure inside the framework is caught, and the first failure of each member goes to the log.
- **`SetPayoutFactor` and `Clear` also work inside `Events.SceneLoaded` and while a save loads or saves.**
- **Your event handlers run inside the game's own call, one at a time.** If one of your handlers throws an error, the framework logs it and still runs the other handlers.
- **Events fire during a save load too.** The framework does not hold them back. A handler that should ignore them can check whether the game is busy loading or saving.
  - Technical: `SaveData.Busy` is true while a save is loading or being written.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the event changes fail, only `Started` and `Finished` stop.** `Contests.IsAvailable` becomes `false`, and `Active()`, `Participants()` and `LastResult()` keep working. Its feature group is `obenseuer.moddingframework.contests`.
  - Technical: this also happens when the private field the framework needs on `ContestController` is missing or has a different type.
- **If the payout change fails, only the payout factors stop.** `Contests.PayoutFactorsAvailable` becomes `false` and `SetPayoutFactor` returns `false`. Nothing else changes. Its feature group is `obenseuer.moddingframework.contests.payout`.
