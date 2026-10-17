---
title: Details
parent: Economy
grand_parent: Money and shops
nav_order: 10
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.economy.log`.

Status: experimental. Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

- The framework changes some of the game's methods when it starts, so it can tell your mod about money changes, trades, mail and the like.
- These changes are grouped into feature groups. If a game update breaks one change in a group, the framework removes all the changes of that group, turns the group off, writes error lines to the group's log file in `BepInEx\logs`, and the rest of the framework keeps working.
- The bank ledger also writes a warning. This page uses these feature groups: `obenseuer.moddingframework.economy` (the money change), `obenseuer.moddingframework.shops.trade`, `.vending` and `.price` (the shop events), and `obenseuer.moddingframework.bankledger` (the ledger).

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)). This goes for every member on this page and for the code that handles each event.
- Your event handlers run inside the game's own call, one at a time, in the order they were added. If one of your handlers throws an error, the framework logs it and still runs the other handlers. <span class="tech">(they run synchronously, as postfixes on the game's methods.)</span>
- Read the price before you change it. `PriceQuery` handlers run inside the game's price call. See [Shops](shops.md#shops).

## If something fails to set up

- The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.
- `Cash.IsAvailable`, `Shops.IsAvailable` and `Mail.IsAvailable` are `false` when their changes could not be set up or their feature group was turned off. Reads still work where they only read game state. Events do not fire while a part is unavailable.
- Each part has its own feature group, so a failure only turns off its own part. The money change is in `obenseuer.moddingframework.economy`.
- The shop events have three groups of their own, `obenseuer.moddingframework.shops.trade`, `.vending` and `.price`. A failed shop change turns off only the shop events and leaves `Cash.MoneyChanged` working. No shop event fires while `Shops.IsAvailable` is `false`, even when only one shop group failed.
- `Mail` and `Banking` install no changes of their own and log under the economy group. The bank ledger has its own group, `obenseuer.moddingframework.bankledger`, so a failed ledger change turns off only the ledger and banking keeps working.

## What happens with bad values

- A null or empty mail name throws an `ArgumentException`. Everything else gives back a plain default (`0`, `null`, an empty list or `false`).
- Amounts outside the range above 0 up to 16,000,000 are refused. So are numbers that are not real numbers.
- If a call into the game throws an error, your call gives back its plain default. The first error of each member is logged. Later errors of that member are not logged again.

## Handlers that call back into the framework

- A handler for `Cash.MoneyChanged`, `Mail.Received`, `Mail.PackageReceived`, `Mail.Sent`, `BankLedger.Added` or `PlayerShops.Sold` runs right away, inside the call that caused it. If that handler calls `Give`, `TrySpend`, `SendItem`, `SendMoney`, `PlayerShops.Sell` or a `Banking` write, it raises the same event again, and so on without end.
- The framework stops this after 8 levels of handlers inside handlers. Past that, the call returns `false` (a `Banking` write returns `Busy`), and the first refusal is logged once. Calls made outside a handler are never refused by this rule.
- The rule covers framework calls only. A handler that calls the game's own money methods directly is not stopped.
- During a trade with a trader, the framework's money writes are refused instead. That covers `Cash` writes, banking writes and mail sends. See [Traders](../traders.md).

## What you can't do (yet)

- Bottle recycling is not available. The machine that pays money for bottles pays through a long-running step with no single payout method to attach to. How much each machine owes is in [Recycling](../recycling.md), but the payout itself can't be read or changed. <span class="tech">(`BottleRecyclingPanel` pays through the coroutine `StartGiveMoney`.)</span>
- Furniture shops and train shops are only partly available. Buying a piece of furniture is `BaseShops.Buy`, and selling one is `BaseShops.Sell` (see [Base shops](../baseshops.md)). The shop's money and restock are in `BaseShops`. <span class="tech">(`BaseShops.Buy` follows `ShopBaseUI.Buy` through public calls. `BaseShops.Sell` repeats the game's sell steps because `ShopBaseUI.Sell` is private. The classes involved are `ShopController`, `BaseShop` and `ShopBaseUI`.)</span>
- Buying from a train shop is not available, because its delivery needs the train screen's preview.
- Market shops have no buy call. The stall's lock, the till payment and the price and currency (which you can only read) are in [Market shops](../marketshops.md).
- The stall's stock is not available, because the items on the conveyor are the player's own goods. The `Market` itself is not available. <span class="tech">(the classes are `MarketShop` and `Market`. `Market.checkInventory` throws `NotImplementedException`.)</span>

## While a save is loading

- The framework holds back some of these events while a save is loading. `Mail.Sent` is dropped while a save loads, so a mail the old session sent that arrives during a load is not raised.
- `Mail.Received` and `Mail.PackageReceived` are not dropped as a whole: the framework drops only the game's replay of the mail that was already stored when the load finished, matched by name over the load's first three frames. A letter that arrives in those frames after that snapshot is not in it, so the game's replay of that letter is not dropped and `Received` can fire twice for it.
- A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is `true` while a save is loading or being written.
