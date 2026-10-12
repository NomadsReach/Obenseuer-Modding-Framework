# Gameplay and economy

This guide shows how to read and change the player, survival needs, NPCs, the world, interactions, tweaks, money and jobs with the Obenseuer Modding Framework 0.9.0. It targets Obenseuer 0.5.05 with BepInEx 5.

## What you will build

A mod called NightStipend. It pays the player 50 OC into their bank account once per game day. At night, from 22:00 to 06:00, hunger rises twice as fast. The paid day is saved, so a save and reload does not pay twice on the same day. If the game is saving or loading when the payment is due, the mod tries again on the next game hour.

You need:

- Obenseuer 0.5.05 with BepInEx 5 and the framework installed under `BepInEx\plugins\ObenseuerCore\`.
- The .NET Framework 4 compiler, `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.
- A mod id for your own mod. Use your own prefix. The examples use `example.`.
- The player must already have a bank account. The stipend pays into an existing account.

## Before you start

- Put the built DLL in `BepInEx\plugins\<YourMod>\`.
- Check that the mod loaded. Look for it in `BepInEx\LogOutput.log`. `CoreLog` messages go to `BepInEx\logs\<modid>.log`, one file per mod id. The file is replaced each time the game starts. Framework messages and errors from event handlers go to `BepInEx\logs\ObenseuerCore.log`.
- The compiler is C# 5. Do not use `?.`, `$""` or `nameof`.
- `MoneyCurrency` has two values, `OC` and `RM`. OC amounts are whole units. RM amounts are rounded to 0.1. See [economy.md](../api/economy.md).
- `Awake` runs before the game's objects exist. Read and write game state from an event handler, such as `Events.Loaded` or `Events.HourPassed`.
- Event handlers run on the main thread. A handler that throws is logged, and the other handlers still run.
- `Shops.All()`, `Banking.All()` and `Jobs.All()` list only what is loaded now. A shop, bank or job in a scene that is not loaded is missing.

## Rules for every snippet

- Call every API on the main thread.
- Register save data and subscribe to events in `Awake`.
- Most calls return a fallback value and never throw into the game. A bad argument is different: it throws `ArgumentException` or `ArgumentNullException` to your code.
- Write to game state only when `SaveData.Busy` is false. Money, banking, invoice, inventory give and take, `PlayerMovement.Lock` and `WorldPower` wake-up calls return `false` while a save or load runs. Many other areas check too, for example doors, locks, containers and trains. Survival, player health, clock, weather, wallet and interaction calls do not check, so test `SaveData.Busy` yourself.
- Use `[BepInDependency("obenseuer.moddingframework", "0.9.0")]` on your plugin.
- Compile with the command in `Examples\SlowHunger\build.cmd`. Copy that file, change the names, and fix the `..\..\bin\ObenseuerCore.dll` path so it points at your copy of the framework. Add a `/r:` for `UnityEngine.UI.dll` from `Obenseuer_Data\Managed` when your mod uses `Survival.Consumables.Intercept`. Without it the compiler reports CS0012.

## 1. The player

### Stats

`PlayerState` reads health and life state, and changes health through the game's own damage and healing paths.

- `PlayerState.Health`, `IsAlive`, `Position` and `FirstName` read the player.
- `PlayerState.Damage(amount, reason)` uses the game's normal damage path. It returns `false` when the player is dead.
- `PlayerState.Heal(amount)` adds to the game's regeneration pool. Health comes back over time.
- `Damage` and `Heal` throw `ArgumentException` for an amount that is zero, negative, NaN or infinite. `Damage` also throws it for an unknown reason.

```csharp
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.firstaid", "FirstAid", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class FirstAid : BaseUnityPlugin
{
    private void Awake()
    {
        Events.HourPassed += hour =>
        {
            if (PlayerState.IsAlive && PlayerState.Health < 20f) PlayerState.Heal(10f);
        };
    }
}
```

The handler heals the player once an hour when health is below 20. `Heal` only adds to the regeneration pool, so the effect is gradual.

### Movement

`PlayerMovement` changes walk speed and jump height, and locks player control.

- `WalkSpeedMultiplier` and `JumpMultiplier` are clamped to 0.25 to 4. A NaN value throws.
- The value you set is kept. It is applied again after every scene load.
- `Lock(modId)` and `Unlock(modId)` count per mod. Each `Lock` needs one matching `Unlock`.
- `Lock` returns `false` while a save is busy or when the game controllers are missing. Check the result.

```csharp
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.slowwalk", "SlowWalk", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class SlowWalk : BaseUnityPlugin
{
    private const string ModId = "example.slowwalk";

    private void Awake()
    {
        PlayerMovement.WalkSpeedMultiplier = 0.5f;
        PlayerMovement.JumpMultiplier = 0.8f;
        Events.HourPassed += hour =>
        {
            if (hour == 0) PlayerMovement.Lock(ModId);
            if (hour == 1) PlayerMovement.Unlock(ModId);
        };
    }
}
```

The snippet locks movement for one game hour, from 00:00 to 01:00. A real mod locks around a scripted event and unlocks when the event ends. A save load, or a scene change that replaces the game controller, releases every lock.

### Inventory

`PlayerInventory` reads and changes the player's main inventory and equipment slots. Items are identified by their numeric id. `Items.Find(name)` returns the catalogue entry for an item title.

- `Has`, `Count`, `Give`, `Take`, `FreeSlots` and `Items()` read or change the inventory.
- `ItemAdded` and `ItemRemoved` fire when the game adds or removes an item.

```csharp
using ObenseuerCore;

public static class Supplies
{
    public static bool GiveByName(string itemName)
    {
        ItemDef item = Items.Find(itemName);
        return item != null && PlayerInventory.Give(item.Id, 1);
    }
}
```

Common mistakes:

- `Give` refuses item ids 0 and 1. It also refuses when the whole amount does not fit in the main grid, and when a save is in progress.
- `Take` refuses when a save is in progress, or when the player does not hold the whole amount.
- `Has`, `Give` and `Take` throw `ArgumentException` when `count` is 0 or less. They do not return `false` for that.
- `FreeSlots` counts the backpack and equipment slots. `Give` only uses the main grid, so `FreeSlots` can be above zero when `Give` still returns `false`.

## 2. Survival needs

`Survival` reads and changes the player's needs through `Stat`. Use these three calls first:

- `Survival.Get(Stat)` and `Survival.Set(Stat, value)` read and write a stat. `Set` clamps to the stat's range. Hunger is -100 to 100. 100 is starving and damages the player, -100 is full.
- `Survival.Decay.Set(stat, modId, multiplier, offsetPerHour)` changes how fast a stat moves. The effective rate is the game's base rate times the product of all multipliers, plus the sum of all offsets.
- `Survival.Consumables.Intercept(modId, handler)` runs before a consumable is used. Set `args.Cancel = true` to stop the use.

`Decay.Set` only accepts `Hunger`, `Thirst`, `Hygiene`, `AlcoholNeed`, `MushroomNeed`, `SmokingNeed` and `GamblingNeed`. For any other stat it returns `false`.

```csharp
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.hungrywalk", "HungryWalk", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class HungryWalk : BaseUnityPlugin
{
    private const string ModId = "example.hungrywalk";

    private void Awake()
    {
        Survival.Decay.Set(Stat.Hunger, ModId, 1.5f, 0f);
        Events.HourPassed += hour =>
        {
            if (Survival.Get(Stat.Hunger) > 90f) Survival.Set(Stat.Hunger, 90f);
        };
    }
}
```

This makes hunger rise 50% faster. Once an hour it sets hunger back to 90 if it is above 90. The check runs once an hour, so hunger can pass 90 between checks.

```csharp
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.noearlyuse", "NoEarlyUse", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class NoEarlyUse : BaseUnityPlugin
{
    private void Awake()
    {
        Survival.Consumables.Intercept("example.noearlyuse", (itemData, itemConsumable, args) =>
        {
            if (GameTime.Hour < 12) args.Cancel = true;
        });
    }
}
```

The handler runs for every consumable use, not only for drinks. It is not called while a trade panel is open. Every handler runs, even after one sets `Cancel`. A cancelled use does nothing. The game does not remove the item, play the sound, start the cooldown or change any stat.

Common mistakes:

- `Decay.Set` returns `false` when `Survival.IsAvailable` is false, or when the stat is not in the list above. It also returns `false` for a multiplier that is negative, NaN or infinite, and for an offset that is NaN or infinite.
- Decay modifiers are kept in memory and are not saved. Set them again on every start. Setting them in an hourly handler does this, because the first `HourPassed` after a load runs on the first game minute.
- A second `Decay.Set` from the same mod replaces the first one for that stat. It does not add to it.
- Offsets are not absolute amounts. The game scales the rate after the patch, for example sleeping hunger is multiplied by 0.33.

## 3. NPCs and their schedules

`Npcs` lists NPCs and reports when they spawn, despawn or die. `NpcSchedules` reads what an NPC is doing now and fires when that changes.

- `Npcs.All()`, `Npcs.Get(id)` and `Npcs.Find(name)` return `NpcInfo` snapshots. `Find` throws on an empty name.
- `Npcs.Spawned`, `Npcs.Despawned` and `Npcs.Died` are events that carry an `NpcInfo`.
- `NpcSchedules.CurrentActivity(npcId)` returns the activity name, or `null`.
- `NpcSchedules.ActivityChanged` carries `NpcId`, `From` and `To`.

```csharp
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.npcwatch", "NpcWatch", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class NpcWatch : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.npcwatch");

    private void Awake()
    {
        NpcSchedules.ActivityChanged += args => Log.Info("npc " + args.NpcId + " " + args.From + " -> " + args.To);
        Npcs.Spawned += info => Log.Info("spawned " + info.Name);
    }
}
```

Common mistakes:

- `Npcs.All()` returns an empty list when the game's NPC database is not loaded yet. Call it from a handler, not from `Awake`.
- `ActivityChanged` only fires when the activity object changes.
- `NpcSchedules.Timetable(npcId)` returns the daily plan as data. It does not change the plan.

## 4. The world

### Time

`GameTime` reads the game clock. `Events.HourPassed` and `Events.DayPassed` tell you when it changes. `WorldClock.SetTimeOfDay` sets the clock.

```csharp
using System;
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.mondaystart", "MondayStart", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class MondayStart : BaseUnityPlugin
{
    private void Awake()
    {
        Events.HourPassed += hour =>
        {
            if (hour == 0 && GameTime.DayOfWeek == DayOfWeek.Monday && !GameTime.IsPaused) WorldClock.SetTimeOfDay(7, 0);
        };
    }
}
```

`SetTimeOfDay` throws for an hour outside 0 to 23 or a minute outside 0 to 59. It returns `false` when the clock is not ready. `SetTimeOfDay` fires nothing itself. `HourPassed` fires for the new hour on the next game minute, and the hours you skipped never fire.

### Weather

`WorldWeather` reads the current weather and changes it. Weather is identified by an index. Use `IndexOf(name)` to find the index for a weather name.

```csharp
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.weatherlog", "WeatherLog", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class WeatherLog : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.weatherlog");

    private void Awake()
    {
        WorldWeather.WeatherChanged += args => Log.Info("weather " + args.Index + " " + WorldWeather.CurrentName + ", raining " + WorldWeather.IsRaining);
    }
}
```

`WorldWeather.Set(index, transitionSeconds)` changes the weather. It throws for an index outside `0..Count-1` or a negative transition time. It returns `false` when the weather system is not ready.

### Power and water

`WorldPower` reads blackouts and generators. `BlackoutStarted` and `GeneratorToggled` tell you when they change.

```csharp
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.blackoutlog", "BlackoutLog", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class BlackoutLog : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.blackoutlog");

    private void Awake()
    {
        WorldPower.BlackoutStarted += () => Log.Info("blackout, generator running " + WorldPower.AnyGeneratorRunning);
        WorldPower.GeneratorToggled += on => Log.Info("generator " + (on ? "on" : "off"));
    }
}
```

`WorldPower.ScheduleWakeUp` and `WakeUp` return `false` while a save is running. `WorldWater.IsPlayerSwimming` and `WorldWater.Amount` read the water state. `WorldLocation.CurrentScene` returns the name of the scene the player is in. See [world.md](../api/world.md) for these.

## 5. Interaction hooks

`Interactions` adds a "Use" prompt to a game object, and reports when the player uses any interaction.

- `Interactions.Add(target, modId, label, use)` adds a prompt. The prompt reads `label` and `(Use)`.
- `Interactions.Remove(target, modId)` removes it, only for the mod that added it.
- `Interactions.Used` fires after the player uses any interaction, including the ones you add.
- `Interactions.Looking()` returns what the crosshair is on, or `null`.

```csharp
using BepInEx;
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.inspect", "Inspect", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class Inspect : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.inspect");

    private void Awake()
    {
        Interactions.Used += args => Log.Info("used " + args.Name + " at " + args.Distance + " m");
    }

    public static bool MakeInspectable(GameObject target)
    {
        return Interactions.Add(target, "example.inspect", "Inspect", () => Log.Info("inspected " + target.name));
    }
}
```

Common mistakes:

- `Add` returns `false` when the target has no `Collider`, when it already has a game interactable (its prompt would win), or when another mod owns the target's interaction.
- `Add` returns `false` when Core is not allowed to patch (`Core.PatchingAllowed` is false).
- `Remove` returns `false` when the interaction was added by another mod. Calling `Remove` from inside the interaction's own action removes it at the end of the frame, so `Add` in the same call returns `false`.
- `Add` throws `ArgumentNullException` for a null `target`, `label` or `use`. It does not return `false` for those.

## 6. Tweaks and TweakState

Tweaks are built-in gameplay multipliers and flags. They are set in `BepInEx\config\obenseuer.moddingframework.cfg`, or in the in-game configuration window (F1). They have no C# write API. A mod must not edit the player's config to change a tweak.

Each setting is one key in a section named after its tweak. For example, `[Tweaks.Time]` with `TimeScale = 2` makes the game clock run twice as fast. Multipliers are clamped to 0.01 to 100. A NaN value becomes 1. Each clamp is logged to `BepInEx\logs\obenseuer.moddingframework.tweaks.log`.

`TweakState` gives read-only access to one tweak value, `RenovationsMoneyOnly`:

```csharp
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.renocheck", "RenoCheck", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class RenoCheck : BaseUnityPlugin
{
    private void Awake()
    {
        Events.Loaded += () => Logger.LogInfo("renovations money only: " + TweakState.RenovationsMoneyOnly);
    }
}
```

Common mistakes:

- Tweak values are stored in the config file, not in the game save.
- `TweakState` has only `RenovationsMoneyOnly`. Do not look for other tweak values there.
- A tweak at its default value installs no patch. A tweak's patch is installed the first time its value is changed from the default.

## 7. Money

### Cash

`Cash` is the money API for OC and RM. `Wallet` covers only OC. New code should use `Cash`.

- `Cash.Balance(currency)` returns the balance.
- `Cash.Has(currency, amount)` checks it.
- `Cash.Give(currency, amount)` adds money.
- `Cash.TrySpend(currency, amount)` takes money only when the player has enough.
- `Cash.MoneyChanged` fires when the balance changes.

```csharp
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.parkingfee", "ParkingFee", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class ParkingFee : BaseUnityPlugin
{
    private void Awake()
    {
        Cash.MoneyChanged += args => Logger.LogInfo("OC " + args.Before + " -> " + args.After);
    }

    public static bool ChargeFee(int oc)
    {
        return Cash.TrySpend(MoneyCurrency.OC, oc);
    }
}
```

Common mistakes:

- OC amounts are whole units. The fraction is dropped.
- `Give` and `TrySpend` return `false` while a save or load is running. `Wallet.Give` does not check this, so use `Cash`.
- `Wallet.Balance` and `Cash.Balance` return 0 on the main menu.

### Bank accounts and the ledger

Bank accounts are changed with `Banking`. The ledger records each change.

- `Banking.Accounts()` lists the player's accounts. Each one has `BankName` and `Frozen`.
- `Banking.Refund(bankName, currency, amount)` adds money to an account. It does not need the bank window to be open.
- `Banking.Charge(bankName, currency, amount)` takes money from an account.
- `Banking.DepositWithdraw` only works while the player has that bank's window open. Do not use it for scheduled payments.
- `BankLedger.Entries(bankName, max, beforeId)` returns the newest entries first, up to 500 per call.
- `BankLedger.Added` fires for each new entry.

```csharp
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.ledgerlog", "LedgerLog", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class LedgerLog : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.ledgerlog");

    private void Awake()
    {
        BankLedger.Added += entry => Log.Info(entry.BankName + " " + entry.Kind + " " + entry.OcDelta + " OC");
    }
}
```

Ledger kinds come from the game or from the framework call that made the change. `Banking.Refund` records the kind `Refund`.

### Invoices

`Invoices` creates and cancels invoices the player can pay. The game takes the payment.

- `Invoices.Create(debtor, currency, amount, days)` makes an invoice. `days` defaults to 7.
- `Invoices.Paid` fires when the player pays.
- `Invoices.CanPay(id)` checks whether the player can pay now.
- `Invoices.Cancel(id)` cancels an invoice. It returns `false` for invoices the game has hooks on.

```csharp
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.repairbill", "RepairBill", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class RepairBill : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.repairbill");

    private void Awake()
    {
        Invoices.Paid += args => Log.Info("paid " + args.Invoice.Amount + " " + args.Invoice.Currency + " from " + args.Invoice.Debtor);
    }

    public static bool Bill(int oc)
    {
        return Invoices.Create("Example Client", MoneyCurrency.OC, oc, 7);
    }
}
```

Common mistakes:

- `Create` throws for an empty debtor, an unknown currency, an amount outside `1..16000000`, or days outside `0..36500`.
- `Create` and `Cancel` return `false` while a save is running.
- `Create` returns `false` when the mail controller is not ready, because the game adds the invoice before it sends the mail.
- `Create` returns only a `bool`. To cancel later, take the id from `Invoices.Issued` (`args.Invoice.Id`) or from `Invoices.All()`, before you call `Cancel(id)`.

### Shops

`Shops` reads trader and vending stock and prices. You change prices with `PriceQuery`.

- `Shops.All()` lists the shops. `Shops.Open()` returns the trader whose panel is open.
- `Shops.Stock(shopId)` lists the items and their prices.
- `Shops.PriceQuery` lets a handler change the price the game uses. The handler sets `args.Price`.
- `Shops.Bought` and `Shops.Sold` fire after a trade.

```csharp
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.loyalty", "Loyalty", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class Loyalty : BaseUnityPlugin
{
    private void Awake()
    {
        Shops.PriceQuery += args =>
        {
            if (args.PlayerBuys) args.Price = args.Price * 0.9f;
        };
    }
}
```

Common mistakes:

- The framework has no write API for trades. Use the game's trade panel.
- `PriceQuery` is not raised for vending machine prices.
- `PriceQuery` runs on every price recalculation, so keep the handler cheap and free of side effects. A price read made inside a handler does not raise it again.
- A price of 0 makes the item free. The result is clamped to 0 to 16000000. A NaN or infinite price is ignored, and the original price is kept.

## 8. Jobs

`Jobs` reads the job stations and reports completed work. The game pays the wages. The framework has no pay API.

- `Jobs.All()` and `Jobs.Get(id)` list the stations.
- `Jobs.Worked` fires after the game completes a wait at a station and has paid it. `WorkedArgs` carries `Id`, `Hours`, `PayOc` and `PayRm`.

```csharp
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.jobpay", "JobPay", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class JobPay : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.jobpay");

    private void Awake()
    {
        Jobs.Worked += args => Log.Info(args.Id + " " + args.Hours + " h, " + args.PayOc + " OC, " + args.PayRm + " RM");
    }
}
```

Common mistakes:

- `Worked` does not fire for a wait of zero time.
- `Worked` runs after the pay, so it cannot change the pay.
- To change a wage, you change a game rule. See section 9.

## 9. Changing a game rule safely

Use the least powerful tool that works, in this order:

1. A tweak in the config (section 6), when one exists for the rule.
2. A facade call that sets, cancels or scales the rule. Examples: `Survival.Decay.Set`, `Survival.Consumables.Intercept`, `PlayerMovement.WalkSpeedMultiplier`, `Shops.PriceQuery`.
3. A facade event that tells you the rule ran. Examples: `Events.DayPassed`, `Cash.MoneyChanged`, `Invoices.Paid`.
4. `CorePatcher.Patch`, only when none of the above covers the rule.

Do not write a raw Harmony patch in a mod. `CorePatcher` checks the game build (`Core.PatchingAllowed`), keeps one Harmony instance per owner, refuses patches from a disabled owner, and logs to `BepInEx\logs\<owner>.log`. A raw patch skips all four.

### CorePatcher rules

- The owner is your mod id. It must not be empty. It must not start with `obenseuer.moddingframework` or `nomad.obenseuer.core`, in any letter case. Otherwise the call throws `ArgumentException`.
- Write `prefix`, `postfix` and `finalizer` as static methods, the form the framework's own patches use. Harmony binds their parameters by name, so use the game's parameter names, for example `__instance` for the instance and `__result` for the return value.
- A patch that fails returns `false`. Check both the return value and `CorePatcher.IsDisabled(owner)`.
- Use `core: false` for a mod patch. A failure then only skips that patch. Use `core: true` only when the mod does not work without the patch. A failure with `core: true` removes every patch from that owner and disables it.
- On a game build other than 0.5.05, the default allows the patch and logs a warning. With `[Safety] RefuseUnknownBuild = true`, every patch is refused.

```csharp
using System;
using System.Reflection;
using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.dayhook", "DayHook", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class DayHook : BaseUnityPlugin
{
    private const string ModId = "example.dayhook";
    private static readonly CoreLog Log = CoreLog.For(ModId);

    private void Awake()
    {
        MethodInfo postfix = typeof(DayHook).GetMethod("AfterDayChanged", BindingFlags.Static | BindingFlags.NonPublic);
        bool ok = CorePatcher.Patch(typeof(TimeOfDayAzure), "IncreaseDayCounter", Type.EmptyTypes, null, postfix, null, false, ModId);
        if (!ok || CorePatcher.IsDisabled(ModId)) Log.Warn("day hook not applied");
    }

    private static void AfterDayChanged()
    {
        Log.Info("day counter increased to " + GameTime.Day);
    }
}
```

This snippet shows the shape of a `CorePatcher` patch. For day changes, use `Events.DayPassed`, which already wraps this event. Use `CorePatcher` for a rule that no facade covers, and check the method name in the game's decompile before you use it.

## Common mistakes across the guide

- A write during a save or load is refused. Check the return value. Do not retry in a tight loop; try again on the next hour or event.
- Keep your mod's saved state in `SaveData.Register`. Register it in `Awake`, before the first load. Use a `[Serializable]` class.
- A handler on an object that lives in a scene must unsubscribe in `OnDestroy`. Otherwise the framework calls it after the object is destroyed.
- `Interactions.Remove`, `PlayerMovement.Lock` and `PlayerMovement.Unlock` reject a mod id that is not 1 to 64 letters, digits, `.`, `_` or `-`. Use the `example.` prefix in examples.
- A decay modifier stays set for the whole session. A load does not reset it, so you do not need to set it again on `Events.Loaded`.

## Reference pages

- Player: [player.md](../api/player.md), [player-movement.md](../api/player-movement.md), [inventory.md](../api/inventory.md), [items.md](../api/items.md).
- Survival: [API.md](../API.md), section "Survival". There is no separate survival page.
- NPCs: [npcs.md](../api/npcs.md), [npc-schedules.md](../api/npc-schedules.md).
- World: [world.md](../api/world.md).
- Interaction: [interaction.md](../api/interaction.md).
- Tweaks: [API.md](../API.md), section "Tweaks" and "TweakState".
- Money: [economy.md](../api/economy.md), [invoices.md](../api/invoices.md).
- Jobs: [jobs.md](../api/jobs.md).
- Rule changes: [API.md](../API.md), section "CorePatcher".

## More areas

These areas are narrower. Each has a reference page.

- Saunas: [saunas.md](../api/saunas.md)
- Dishwashers: [dishwashers.md](../api/dishwashers.md)
- Showers: [showers.md](../api/showers.md)
- Hotels: [hotels.md](../api/hotels.md)
- Breeding and traps: [breeding.md](../api/breeding.md)
- Casino: [casino.md](../api/casino.md)
- Card games: [cardgames.md](../api/cardgames.md)
- Trains and tickets: [trains.md](../api/trains.md), [traintickets.md](../api/traintickets.md)
- Fuel stations: [fuel.md](../api/fuel.md)
- Medical: [medical.md](../api/medical.md)
- Weapons: [weapons.md](../api/weapons.md)
- Doors and locks: [doors.md](../api/doors.md)
- Lights and televisions: [lights.md](../api/lights.md), [televisions.md](../api/televisions.md)
- Mailboxes and parcel lockers: [mailboxes.md](../api/mailboxes.md), [parcellockers.md](../api/parcellockers.md)
- Janitors and cleaning: [janitors.md](../api/janitors.md), [cleaning.md](../api/cleaning.md)
- Global flags: [global-flags.md](../api/global-flags.md)

## Example: NightStipend

This mod pays 50 OC into the player's first bank account that is not frozen, once per game day. At night, from 22:00 to 06:00, hunger rises twice as fast. The paid day is saved.

```csharp
using System;
using System.Collections.Generic;
using BepInEx;
using ObenseuerCore;

[Serializable]
public class StipendState
{
    public int lastPaidDay = -1;
}

[BepInPlugin("example.nightstipend", "NightStipend", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class NightStipend : BaseUnityPlugin
{
    private const string ModId = "example.nightstipend";
    private const int DailyOc = 50;
    private static readonly CoreLog Log = CoreLog.For(ModId);
    private StipendState state = new StipendState();
    private bool warnedNoAccount;

    private void Awake()
    {
        SaveData.Register<StipendState>(ModId, () => state, s => state = s ?? new StipendState());
        Events.HourPassed += OnHourPassed;
    }

    private void OnHourPassed(int hour)
    {
        bool night = hour >= 22 || hour < 6;
        Survival.Decay.Set(Stat.Hunger, ModId, night ? 2f : 1f, 0f);
        if (state.lastPaidDay != GameTime.Day) PayStipend();
    }

    private void PayStipend()
    {
        List<AccountSummary> accounts = Banking.Accounts();
        if (accounts.Count == 0)
        {
            if (!warnedNoAccount) Log.Info("no bank account yet");
            warnedNoAccount = true;
        }
        foreach (AccountSummary account in accounts)
        {
            if (account.Frozen) continue;
            if (Banking.Refund(account.BankName, MoneyCurrency.OC, DailyOc) != BankResult.Ok) return;
            state.lastPaidDay = GameTime.Day;
            Log.Info("stipend " + DailyOc + " OC to " + account.BankName + " on day " + GameTime.Day);
            return;
        }
    }
}
```

How it works:

- `Events.HourPassed` runs every game hour. Each hour it sets the hunger decay multiplier to 2 at night, so hunger rises twice as fast, and to 1 during the day. `Decay.Set` replaces the entry for this mod, so calling it every hour is safe.
- Each hour it also checks whether today is paid. If not, it tries to pay. If the payment is refused (a save is running), the day stays unpaid and the next hour tries again.
- `Banking.Accounts()` lists the player's accounts. The first account that is not frozen gets the money.
- `Banking.Refund` adds the money to the account. Its ledger entry has the kind `Refund`, and that label cannot be changed. For plain pocket money, `Cash.Give` is simpler.
- `SaveData` keeps `lastPaidDay`. After a load, the mod does not pay again for the same day.

Build it with a copy of `Examples\SlowHunger\build.cmd`, renamed to `NightStipend`.
