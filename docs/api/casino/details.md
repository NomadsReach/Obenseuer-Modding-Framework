---
title: Details
parent: Casino
grand_parent: Money and shops
nav_order: 6
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.casino.log`. Source files: `src/Api/Casino.cs` and `src/Api/CasinoPatches.cs`.

Everything not listed in this reference is internal and may change without notice.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game only works safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)). This goes for the code that handles the events too.
- It is safe to call any of it at any time, even on the main menu or with no save loaded. `Machines()` gives an empty list and `LastPayout` gives `0`.
- Nothing throws to you. If the game fails inside a call, the framework catches it and logs the first failure of each member.
- Your event handlers run inside the game's own call, after the game has worked out the result. If one throws an error, the framework logs it and still runs the other handlers.
- The events cost nothing when nobody listens. The coin pusher's changes can run many times a second, so the framework builds nothing when an event has no handler. `LastPayout` is still kept up to date.
- Events are not stopped while a save is loading. If your handler should ignore events raised while a save loads, check `SaveData.Busy` (true while a save is loading or being written).

## If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod about the machines. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working. This page has five feature groups, one for each part.

- The slot machine events use the feature group `obenseuer.moddingframework.casino.slot`, the coin pusher events use `.pajatso`, and the scratchcard event uses `.scratch`. If one is missing, only that part's events stop, and `IsAvailable` is `false`. <span class="tech">(The changed methods are `SlotMachineGameplay.ChargeMoney`, `HandleWinning` and `HandleQuittingProcedure` (slot), `PajatsoGameplay.OnLaunchCoin`, `CoinGateHit` and `OnCollectWins` (coin pusher), and `Scratchcard.Use` (scratchcard). The slot part also needs the private fields `stakes` and `_stakesIndex`.)</span>
- The `Doubled` event uses the feature group `obenseuer.moddingframework.casino.doubling`. It is optional. If it is missing, only `Doubled` stops, and `IsAvailable` does not change. <span class="tech">(The changed method is `SlotMachineGameplay.HandleDoublingResult`.)</span>
- Payout factors use the feature group `obenseuer.moddingframework.casino.payout`. If it is missing, `PayoutFactorsAvailable` is `false` and `SetPayoutFactor` returns `false`. <span class="tech">(The changed methods are `SlotMachineHelper.CalculateReward` and `PajatsoGameplay.CoinGateHit`.)</span>
