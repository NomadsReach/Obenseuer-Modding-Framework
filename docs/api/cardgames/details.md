---
title: Details
parent: Card games
grand_parent: Money and shops
nav_order: 4
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.cardgames.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts, so it can tell your mod when a card game starts and ends and change the payout. This page calls each set of changes a feature group. Each group has a name that shows up in the log. This page uses two: `obenseuer.moddingframework.cardgames` (the `Started` and `Ended` events) and `obenseuer.moddingframework.cardgames.payout` (the payout factors).

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only runs safely on the main thread (see the [glossary](../../glossary.md#main-thread)).
- It is safe to call any of it at any time. On the main menu or with no save loaded, `Tables()` gives an empty list and `Active()` gives `null`. Nothing throws an error to you. A failure inside the framework is caught, and the first failure of each member goes to the log.
- Your event handlers run inside the game's own call, one at a time. If one of your handlers throws an error, the framework logs it and still runs the other handlers.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework turns off the [feature group](../../glossary.md#feature-group) that depends on it, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the event changes fail, only `Started` and `Ended` stop. `CardGames.IsAvailable` becomes `false`, and `Tables()` and `Active()` keep working. Its feature group is `obenseuer.moddingframework.cardgames`.
- If the payout change fails, only the payout factors stop. `CardGames.PayoutFactorsAvailable` becomes `false`, and the events are not affected. Its feature group is `obenseuer.moddingframework.cardgames.payout`. <span class="tech">(`CorePatcher.IsDisabled("obenseuer.moddingframework.cardgames")` becomes true when one of the event changes is missing. The payout change edits the compiled body of the game's `OnGameFinished` and needs to find exactly one `Mathf.RoundToInt` call in it.)</span>
