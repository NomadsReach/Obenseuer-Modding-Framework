---
title: Gameplay and economy
parent: Guides
nav_order: 3
---

# Gameplay and economy

This page lists the game areas the framework lets your mod read and change. Each section names the main calls. For code you can copy, see the [cookbook](cookbook/index.md). For every member, see the [API reference](../API.md).

## Rules for every call

- Call the framework on the main thread. Subscribe to events and register save data in `Awake`.
- Money, banking, invoice, inventory and movement calls return `false` while a save or load runs. Check the result and try again on a later hour or event.
- Most survival, health, clock, weather and wallet writes return `false` while a save loads or saves (`SaveData.Busy` is `true`). `Survival.Decay.Set` is not gated. Check the result, or test `SaveData.Busy` first.
- A bad argument throws `ArgumentException` or `ArgumentNullException`. Most other problems return a fallback value and do not throw.
- Lists such as `Shops.All()` and `Jobs.All()` show only what is loaded now.
- The framework builds as C# 5. The samples avoid `?.`, `$""` and `nameof`, and the Starter project uses the latest C# version.
- Add `[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]` to your plugin. On BepInEx 5, write `"0.11.0"` instead, because BepInEx 5 cannot parse a version range. See [Supporting BepInEx 5 too](getting-started/packaging.md#supporting-bepinex-5-too).

## The player

- `PlayerState.Health`, `IsAlive`, `Position` and `FirstName` read the player.
- `PlayerState.Damage(amount, reason)` uses the game's damage path. It returns `false` when the player is dead.
- `PlayerState.Heal(amount)` adds to the game's regeneration pool. Health comes back over time.
- `PlayerMovement.WalkSpeedMultiplier` and `JumpMultiplier` are clamped to 0.25 to 4. They are applied again after each scene load.
- `PlayerMovement.Lock(modId)` and `Unlock(modId)` count per mod. Each `Lock` needs one `Unlock`. A load, or a scene change that replaces the controller, releases every lock.
- `PlayerInventory.Has`, `Count`, `Give`, `Take`, `FreeSlots` and `Items()` read and change the main inventory and the equipment slots.
- `Give` refuses item ids 0 and 1, refuses when the amount does not fit in the main grid, and refuses during a save. `FreeSlots` also counts the backpack and equipment, so it can be above 0 while `Give` still returns `false`.
- `Has`, `Give` and `Take` throw `ArgumentException` when the count is 0 or less.
- `ItemAdded` and `ItemRemoved` fire when the game adds or removes an item.

Item ids are explained in [Finding item ids](content.md#finding-item-ids).

## Survival

- `Survival.Get(Stat)` and `Survival.Set(Stat, value)` read and write a stat. Hunger runs from -100 (full) to 100 (starving).
- `Survival.Decay.Set(stat, modId, multiplier, offsetPerHour)` changes how fast a stat moves. It accepts `Hunger`, `Thirst`, `Hygiene`, `AlcoholNeed`, `MushroomNeed`, `SmokingNeed` and `GamblingNeed`. Each mod has one entry per stat, and a second call replaces it.
- `Survival.Consumables.Intercept(modId, handler)` runs before a consumable is used. Set `args.Cancel = true` to stop the use. A cancelled use does nothing, not even the sound. Handlers run in registration order, any `Cancel` wins, and the same handler registered twice by one mod runs once. `Survival.Consumables.Remove(modId, handler)` removes one handler.
- Decay settings are kept in memory and are not saved. Set them again on each start.

## NPCs

- `Npcs.All()`, `Npcs.Get(id)` and `Npcs.Find(name)` return `NpcInfo` snapshots. `Npcs.Spawned`, `Despawned` and `Died` carry an `NpcInfo`.
- `NpcSchedules.CurrentActivity(npcId)` returns the activity name, or `null`.
- `NpcSchedules.ActivityChanged` carries `NpcId`, `From` and `To`.
- `NpcSchedules.Timetable(npcId)` returns the daily plan as data. It does not change the plan.
- `Npcs.All()` is empty until the NPC database loads. Call it from a handler, not from `Awake`.

## Time, weather and power

- `GameTime` reads the clock. `GameTime.IsDaytime` is true from hour 6 through 20.
- `WorldClock.SetTimeOfDay(hour, minute)` sets the clock. It throws for an hour outside 0 to 23 or a minute outside 0 to 59. It returns `false` when the clock is not ready, a save is busy, or the time is earlier than the current time, and it raises no event.
- `WorldWeather.WeatherChanged` fires when the weather changes. `WorldWeather.Set(index, transitionSeconds)` changes it. Use `IndexOf(name)` to find an index. `Set` throws for an index outside `0..Count-1` or a negative transition time.
- `WorldPower.BlackoutStarted` and `GeneratorToggled` report power changes. `WorldPower.ScheduleWakeUp` and `WakeUp` return `false` during a save.
- Water and location: see [world](../api/world.md).

## Interactions

- `Interactions.Add(target, modId, label, use)` adds a "Use" prompt to a game object. The target needs a `Collider`. The five-argument `Add` takes a verb in place of "Use", for example `Add(target, modId, "Treadmill", "Run", use)` shows `(Run)`. `Interactions.SetReason(target, modId, reason)` shows a red reason in place of the verb and blocks the use while the reason is set.
- `Interactions.Remove(target, modId)` removes only a prompt that your mod added.
- `Interactions.Used` fires after the player uses any interaction.
- `Interactions.Looking()` returns what the crosshair is on, or `null`.
- `Add` returns `false` while a save is busy, when the target has no `Collider`, already has a game interaction, belongs to another mod, or when patching is not allowed. `Add` throws `ArgumentNullException` for a null `target`, `label` or `use`.

## Tweaks

Tweaks are built-in gameplay multipliers and flags. Set them in `BepInEx\config\obenseuer.moddingframework.cfg`, or in the Mods menu. A mod cannot set them from code, and it should not edit the player's config.

- Each tweak is a section, for example `[Tweaks.Time]` with `TimeScale = 2`. Multipliers are clamped to 0.01 to 100.
- Clamps are logged to `BepInEx\logs\obenseuer.moddingframework.tweaks.log`.
- `TweakState.RenovationsMoneyOnly` is the only tweak value you can read from code.
- Tweak values live in the config file, not in the save.

## Money

Use `Cash` for OC and RM. `Wallet` covers OC only.

- `Cash.Balance(currency)`, `Cash.Has(currency, amount)`, `Cash.Give(currency, amount)` and `Cash.TrySpend(currency, amount)`.
- `MoneyCurrency` is `OC` or `RM`. OC amounts are whole units. RM amounts round to 0.1.
- `Cash.MoneyChanged` fires when a balance changes.
- `Give` and `TrySpend` return `false` during a save or load. `Wallet.Balance` and `Cash.Balance` return 0 on the main menu.

Bank accounts:

- `Banking.Accounts()` lists the player's accounts, each with `BankName` and `Frozen`.
- `Banking.Refund(bankName, currency, amount)` adds money to an account. It works without the bank window open. Its ledger entry has the kind `Refund`.
- `Banking.Charge(bankName, currency, amount)` takes money from an account.
- `Banking.DepositWithdraw` works only while the bank's window is open. Do not use it for scheduled payments.
- `BankLedger.Entries(bankName, max, beforeId)` lists entries, newest first, up to 500 per call. `BankLedger.Added` fires for each new entry.

Invoices:

- `Invoices.Create(debtor, currency, amount, days)` makes an invoice the player pays. `days` defaults to 7. It returns a `bool`, so get the id from `Invoices.Issued` if you may cancel later.
- `Invoices.Paid` fires when the player pays. `Invoices.CanPay(id)` checks whether they can. `Invoices.Cancel(id)` cancels one.
- `Create` throws for an empty debtor, an unknown currency, an amount outside `1..16000000`, or days outside `0..36500`.

Shops:

- `Shops.All()` lists the shops. `Shops.Open()` returns the trader whose panel is open. `Shops.Stock(shopId)` lists items and prices.
- `Shops.PriceQuery` lets a handler set `args.Price` for a trade. It is not raised for vending machines, and it runs on every price update, so keep the handler cheap.
- `Shops.Bought` and `Shops.Sold` fire after a trade.
- The framework can make a trade. `Traders.Buy(modId, traderId, itemId, count, out refusal)` and `Traders.Sell` trade with a trader ([traders](../api/traders.md)). At a furniture shop, `BaseShops.Buy(id, furnitureId, count)` and `BaseShops.Sell(id, furnitureId, count)` do the same ([furniture shops](../api/baseshops.md)). The player uses the game's trade panel.

## Jobs

- `Jobs.All()` and `Jobs.Get(id)` list the job stations.
- `Jobs.Worked` fires after the game has paid for a finished wait. `WorkedArgs` carries `Id`, `Hours`, `PayOc` and `PayRm`. It cannot change the pay.
- The framework has no pay API. To change a wage, change the rule, as described below.

## Changing a game rule

Use the least powerful tool that works:

1. A tweak in the config, when one exists for the rule.
2. A helper that sets, cancels or scales the rule, such as `Survival.Decay.Set`, `Survival.Consumables.Intercept`, `PlayerMovement.WalkSpeedMultiplier` or `Shops.PriceQuery`.
3. An event that tells you the rule ran, such as `Events.DayPassed`, `Cash.MoneyChanged` or `Invoices.Paid`.
4. `CorePatcher.Patch`, only when nothing above covers the rule. See [Patching](getting-started/patching.md).

Check the method name in the game's code before you patch it.

## Reference pages

- Player: [player](../api/player.md), [player-movement](../api/player-movement.md), [inventory](../api/inventory.md), [items](../api/items.md).
- Survival: [API reference](../api-core.md), section "Survival".
- NPCs: [npcs](../api/npcs.md), [npc-schedules](../api/npc-schedules.md).
- World: [world](../api/world.md). Interactions: [interaction](../api/interaction.md).
- Money: [economy](../api/economy.md), [invoices](../api/invoices.md). Jobs: [jobs](../api/jobs.md).
- Rule changes: [API reference](../api-core.md), section `CorePatcher`.
- Smaller areas: [saunas](../api/saunas.md), [dishwashers](../api/dishwashers.md), [showers](../api/showers.md), [hotels](../api/hotels.md), [breeding](../api/breeding.md), [casino](../api/casino.md), [cardgames](../api/cardgames.md), [trains](../api/trains.md), [traintickets](../api/traintickets.md), [fuel](../api/fuel.md), [medical](../api/medical.md), [weapons](../api/weapons.md), [doors](../api/doors.md), [lights](../api/lights.md), [televisions](../api/televisions.md), [mailboxes](../api/mailboxes.md), [parcellockers](../api/parcellockers.md), [janitors](../api/janitors.md), [cleaning](../api/cleaning.md), [global-flags](../api/global-flags.md).
