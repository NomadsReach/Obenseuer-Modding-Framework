---
title: Gameplay and economy
parent: Guides
nav_order: 3
---

# Gameplay and economy

This guide shows the parts of the game your mod can read and change: the player, survival, NPCs, time and weather, money, shops and jobs. Each section names the main calls and what they do in plain words. For short recipes you can copy, see the [cookbook](cookbook/index.md). For every member, see the [API reference](../API.md).

## Try it first

This small mod pays the player 10 OC at the start of every new day and counts how many days it has done that. It shows the three habits the rules below ask for: set things up in `OnLoad`, keep your own data in the save, and check what a call returns. Use the project from [Your first mod](getting-started/first-mod.md) and put this in `Plugin.cs`.

```csharp
using System;
using ObenseuerCore;

[Serializable]
public class PayState   // the data this mod keeps in each save
{
    public int daysPaid;   // how many days the mod has paid the player
}

[ModInfo("example.dailypay", "Daily Pay", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class DailyPay : FrameworkMod
{
    private PayState state = new PayState();   // the current data

    protected override void OnLoad()   // runs once when the framework starts your mod
    {
        SaveData.Register<PayState>(Info.Id, () => state, s => state = s ?? new PayState());   // what to save, and what to do with the loaded data
        Events.DayPassed += day =>   // runs when a new day starts
        {
            if (!Cash.Give(MoneyCurrency.OC, 10)) return;   // false means the game could not take it now, so try again tomorrow
            state.daysPaid++;
            Log.Info("paid 10 OC, days paid so far: " + state.daysPaid);
        };
    }
}
```

Build it, start the game and load a save. When a new day starts you get 10 OC, and the line `paid 10 OC` appears in `BepInEx\logs\example.dailypay.log`. The payment only happens when `Cash.Give` returns `true`.

## Rules for every call

These rules hold for almost every call on this page.

- **Call the framework from your mod's normal code, and set up in the right place.** Normal code means `OnLoad`, event handlers and `Update`, not a background thread you started (see [main thread](../glossary.md#main-thread)). Subscribe to events and register save data once, when your mod starts: in `OnLoad` for a framework mod, or in `Awake` for a BepInEx plugin. Registering save data later also works, and its data is loaded at once if a save is already open. A second registration with the same id throws.
  - Technical: `SaveData.Register` loads the entry immediately when `SaveData.Folder` is set and no load is running; otherwise it waits for the load that is coming.
- **Calls that change money, banking, invoices, the inventory or movement return `false` while the game saves or loads.** Check the result and try again on a later hour or event.
- **Most calls that change survival, health, the clock, the weather or the wallet also return `false` while a save loads or saves.** The same condition is `SaveData.Busy`, which is `true` during that time. `Survival.Decay.Set` is the one that does not check it. Check the result, or test `SaveData.Busy` first.
- **A bad argument throws `ArgumentException` or `ArgumentNullException`.** Most other problems return a fallback value (such as `false`, 0 or `null`) and do not throw.
- **Lists such as `Shops.All()` and `Jobs.All()` show only what is loaded right now.**
- **You can use newer C# than the framework itself.** The framework is built as C# 5, and the samples avoid `?.`, `$""` and `nameof`. Your project from the Starter uses the latest C# version.
- **Tell BepInEx or the framework which framework version you need.** A framework mod has `[ModDependency("obenseuer.moddingframework", "0.11.0")]`, as in [Your first mod](getting-started/first-mod.md). A BepInEx plugin has `[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]` instead. On BepInEx 5, write `"0.11.0"` without `>=`, because BepInEx 5 cannot read a version range. See [Supporting BepInEx 5 too](getting-started/packaging.md#supporting-bepinex-5-too).

Item ids are explained in [Finding item ids](content.md#finding-item-ids).

## The player

- **`PlayerState` reads the player.** `Health`, `IsAlive`, `Position` and `FirstName` do what their names say.
- **`PlayerState.Damage(amount, reason)` hurts the player the way the game does.** It returns `false` when the player is already dead.
- **`PlayerState.Heal(amount)` adds to the pool the game heals from.** Health comes back over time, not at once.
  - Technical: it adds to the game's regeneration pool.
- **`PlayerMovement.WalkSpeedMultiplier` and `JumpMultiplier` change how fast and how high the player moves.** They stay between 0.25 and 4, and they are applied again after each scene load.
- **`PlayerMovement.Lock(modId)` and `Unlock(modId)` stop and free the player's movement.** Each mod counts on its own, so each `Lock` needs one `Unlock`. A load, or a scene change that replaces the player's controller, releases every lock.
- **`PlayerInventory` reads and changes the main inventory and the equipment slots.** The calls are `Has`, `Count`, `Give`, `Take`, `FreeSlots` and `Items()`.
- **`Give` can refuse.** It refuses item ids 0 and 1, refuses when the amount does not fit in the main grid, and refuses during a save. `FreeSlots` also counts the backpack and equipment, so it can be above 0 while `Give` still returns `false`.
- **`Has`, `Give` and `Take` throw `ArgumentException` when the count is 0 or less.**
- **`ItemAdded` and `ItemRemoved` fire when the game adds or removes an item.**

## Survival

- **`Survival.Get(Stat)` and `Survival.Set(Stat, value)` read and write a need.** Hunger runs from -100 (full) to 100 (starving).
- **`Survival.Decay.Set(stat, modId, multiplier, offsetPerHour)` changes how fast a need grows.** It accepts `Hunger`, `Thirst`, `Hygiene`, `AlcoholNeed`, `MushroomNeed`, `SmokingNeed` and `GamblingNeed`. Each mod has one entry per need, and a second call replaces the first.
- **`Survival.Consumables.Intercept(modId, handler)` lets you step in before the player uses food, drink or another consumable.** An interceptor is a function the framework calls before the action happens. Set `args.Cancel = true` to stop the use. A stopped use does nothing, not even the sound. Handlers run in the order the mods registered them, any `Cancel` wins, and the same handler registered twice by one mod runs once. `Survival.Consumables.Remove(modId, handler)` removes one handler.
- **Decay settings are not saved.** They live in memory, so set them again each time the game starts.

## NPCs

- **`Npcs` gives you information about characters in the game.** `Npcs.All()`, `Npcs.Get(id)` and `Npcs.Find(name)` return `NpcInfo` objects, which are copies of an NPC's data at that moment. They do not change afterwards. `Npcs.Spawned`, `Despawned` and `Died` are events that carry an `NpcInfo`.
- **`NpcSchedules.CurrentActivity(npcId)` returns the name of what the NPC is doing now, or `null`.**
- **`NpcSchedules.ActivityChanged` fires when an NPC changes activity.** It carries `NpcId`, `From` and `To`.
- **`NpcSchedules.Timetable(npcId)` returns the NPC's daily plan as data.** It only reads the plan. It does not change it.
- **`Npcs.All()` is empty until the game has loaded its NPCs.** Call it from an event handler, not from your startup code.

## Time, weather and power

- **`GameTime` reads the game clock.** `GameTime.IsDaytime` is `true` from hour 6 through 20.
- **`WorldClock.SetTimeOfDay(hour, minute)` sets the clock.** It throws for an hour outside 0 to 23 or a minute outside 0 to 59. It returns `false` when the clock is not ready, a save is busy, or the time is earlier than the current time (time only moves forward). It raises no event.
- **`WorldWeather.WeatherChanged` fires when the weather changes.** `WorldWeather.Set(index, transitionSeconds)` changes it, and `IndexOf(name)` finds an index from a name. `Set` throws for an index outside `0..Count-1` or a negative transition time.
- **`WorldPower.BlackoutStarted` and `GeneratorToggled` tell you about power changes.** `WorldPower.ScheduleWakeUp` and `WakeUp` return `false` during a save.
- **Water and location are on the [world](../api/world.md) page.**

## Interactions

An interaction is the "Use" prompt that shows when the player looks at an object.

- **`Interactions.Add(target, modId, label, use)` adds a "Use" prompt to a game object.** `target` needs a `Collider` (the invisible shape the game uses to know what the player is looking at). The five-argument `Add` takes a verb in place of "Use". For example `Add(target, modId, "Treadmill", "Run", use)` shows `(Run)`.
- **`Interactions.SetReason(target, modId, reason)` shows a red reason in place of the verb and blocks the use while the reason is set.**
- **`Interactions.Remove(target, modId)` removes only a prompt that your mod added.**
- **`Interactions.Used` fires after the player uses any interaction.**
- **`Interactions.Looking()` returns what the crosshair is on, or `null`.**
- **`Add` can say no.** It returns `false` while a save is busy, when the target has no `Collider`, when it already has a game interaction, when it belongs to another mod, or when patching is not allowed. It throws `ArgumentNullException` for a `null` target, label or `use`.

## Tweaks

Tweaks are built-in gameplay settings: multipliers and on/off switches, such as the game's time speed. The player sets them in the config file `BepInEx\config\obenseuer.moddingframework.cfg`, or in the Mods menu. **Your mod cannot set them from code, and it should not edit the player's config file.**

- **Each tweak is a section of the config file,** for example `[Tweaks.Time]` with `TimeScale = 2`. Multipliers are kept between 0.01 and 100.
- **When a value is changed to fit those limits, the change is written to `BepInEx\logs\obenseuer.moddingframework.tweaks.log`.**
- **`TweakState.RenovationsMoneyOnly` is the only tweak value you can read from code.**
- **Tweak values live in the config file, not in the save.**

## Money

The game has two currencies. OC is Open Sewer Coins, the everyday money. RM is Real Money (see the glossary entries [OC](../glossary.md#oc) and [RM](../glossary.md#rm)). Use `Cash` for either one. `Wallet` covers OC only.

- **`Cash` reads and changes the player's money.** The calls are `Cash.Balance(currency)`, `Cash.Has(currency, amount)`, `Cash.Give(currency, amount)` and `Cash.TrySpend(currency, amount)`.
- **`MoneyCurrency` is `OC` or `RM`.** OC amounts are whole numbers. RM amounts are rounded to 0.1.
- **`Cash.MoneyChanged` fires when a balance changes.**
- **`Give` and `TrySpend` return `false` during a save or load.** `Wallet.Balance` and `Cash.Balance` return 0 on the main menu.

Bank accounts:

- **`Banking.Accounts()` lists the player's bank accounts.** Each has a `BankName` and a `Frozen` flag.
- **`Banking.Refund(bankName, currency, amount)` adds money to an account.** It works without the bank window open. Its ledger entry has the kind `Refund`.
- **`Banking.Charge(bankName, currency, amount)` takes money from an account.**
- **`Banking.DepositWithdraw` works only while the bank's window is open.** Do not use it for payments you schedule.
- **`BankLedger.Entries(bankName, max, beforeId)` lists ledger entries, newest first, up to 500 per call.** The ledger is the account's list of past transactions. `BankLedger.Added` fires for each new entry.

Invoices:

- **`Invoices.Create(debtor, currency, amount, days)` makes a bill the player pays.** `days` is how long they have, and it defaults to 7. It returns a `bool`, not an id, so get the id from `Invoices.Issued` if you may cancel the invoice later.
- **`Invoices.Paid` fires when the player pays.** `Invoices.CanPay(id)` checks whether they can. `Invoices.Cancel(id)` cancels one.
- **`Create` throws for an empty debtor, an unknown currency, an amount outside `1..16000000`, or days outside `0..36500`.**

Shops:

- **`Shops.All()` lists the shops.** `Shops.Open()` returns the trader whose panel is open. `Shops.Stock(shopId)` lists items and prices.
- **`Shops.PriceQuery` lets a handler set `args.Price` for a trade.** It is not raised for vending machines. It runs on every price update, so keep the handler quick.
- **`Shops.Bought` and `Shops.Sold` fire after a trade.**
- **Your mod can make a trade itself.** `Traders.Buy(modId, traderId, itemId, count, out refusal)` and `Traders.Sell` trade with a trader ([traders](../api/traders.md)). At a furniture shop, `BaseShops.Buy(id, furnitureId, count)` and `BaseShops.Sell(id, furnitureId, count)` do the same ([furniture shops](../api/baseshops.md)). The player uses the game's own trade panel.

## Jobs

- **`Jobs.All()` and `Jobs.Get(id)` list the job stations,** the places where the player waits to earn pay.
- **`Jobs.Worked` fires after the game has paid for a finished wait.** `WorkedArgs` carries `Id`, `Hours`, `PayOc` and `PayRm`. It only reports the pay. It cannot change it.
- **The framework has no call to set pay.** To change a wage, change the rule, as the next section describes.

## Changing a game rule

Use the least powerful tool that does the job. Go down this list and stop at the first one that works.

1. **A tweak in the config,** when one exists for the rule.
2. **A framework call that sets, cancels or scales the rule,** such as `Survival.Decay.Set`, `Survival.Consumables.Intercept`, `PlayerMovement.WalkSpeedMultiplier` or `Shops.PriceQuery`.
3. **An event that tells you the rule ran,** such as `Events.DayPassed`, `Cash.MoneyChanged` or `Invoices.Paid`. You react after the fact.
4. **`CorePatcher.Patch`,** only when nothing above covers the rule. It changes a game method directly. See [Patching](getting-started/patching.md).

Check the method name in the game's code before you patch it.

## Common mistakes

- **Ignoring the result.** Money, inventory and survival calls return `false` during a save or load. If you ignore the result, your change quietly does not happen.
- **Calling `Npcs.All()` at startup.** It is empty until the NPCs have loaded. Call it from an event handler.
- **Setting a need's decay and expecting it to last.** Decay settings are not saved. Set them again each time the game starts.
- **Calling from a background thread.** Run framework calls from your mod's normal code. See [main thread](../glossary.md#main-thread).

## Where to go next

- Player: [player](../api/player.md), [player-movement](../api/player-movement.md), [inventory](../api/inventory.md), [items](../api/items.md).
- Survival: [API reference](../api-core.md), section "Survival".
- NPCs: [npcs](../api/npcs.md), [npc-schedules](../api/npc-schedules.md).
- World: [world](../api/world.md). Interactions: [interaction](../api/interaction.md).
- Money: [economy](../api/economy.md), [invoices](../api/invoices.md). Jobs: [jobs](../api/jobs.md).
- Rule changes: [API reference](../api-core.md), section `CorePatcher`.
- Smaller areas: [saunas](../api/saunas.md), [dishwashers](../api/dishwashers.md), [showers](../api/showers.md), [hotels](../api/hotels.md), [breeding](../api/breeding.md), [casino](../api/casino.md), [cardgames](../api/cardgames.md), [trains](../api/trains.md), [traintickets](../api/traintickets.md), [fuel](../api/fuel.md), [medical](../api/medical.md), [weapons](../api/weapons.md), [doors](../api/doors.md), [lights](../api/lights.md), [televisions](../api/televisions.md), [mailboxes](../api/mailboxes.md), [parcellockers](../api/parcellockers.md), [janitors](../api/janitors.md), [cleaning](../api/cleaning.md), [global-flags](../api/global-flags.md).
- When your mod works, read [Using the framework alongside other mods](multiple-mods.md) before you share it.
