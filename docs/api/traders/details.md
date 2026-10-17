---
title: Details
parent: Traders
grand_parent: People
nav_order: 6
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.traders.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes one game method when it starts, so it can scale prices. This page calls that a feature group: its name is `obenseuer.moddingframework.traders`, the name you will see in the log.

## When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- **Reading is safe at any time, even on the main menu.** `Get` gives `null` when the trader is not found or the framework is not ready.

## What to check for and what comes back

- **`Traders.IsAvailable` tells you whether reading a trader and changing its money work.** `Traders.PriceFactorsAvailable` tells you whether price factors work. Both are `false` when the framework is not ready.
- **Writes return `false` instead of throwing.** `SetMoney`, `Restock`, `SetPriceFactors` and `ClearPriceFactors` return `false` during a save or a load, while a trade is running, for an unknown shop id, for a mod id that is not valid, or when a number is not a real number or out of range. `Buy` and `Sell` return `false` and give a reason in `refusal`.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the framework cannot change the game's price method, price factors stop working and the rest keeps working.** `PriceFactorsAvailable` becomes `false`.
  - Technical: the method is `Trade.GetItemStackPrice`, the same one the `Shops.PriceQuery` event uses.
