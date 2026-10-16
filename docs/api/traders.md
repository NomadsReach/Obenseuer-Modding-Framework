---
title: Obenseuer Modding Framework trader API (Traders)
parent: API reference
nav_order: 73.1
---

# Obenseuer Modding Framework trader API (Traders)

Experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.traders`. Logs go to `BepInEx\logs\obenseuer.moddingframework.traders.log`.
Files: `src/Api/Traders.cs`, the price hook in `src/Api/ShopsPatches.cs`, and the trade's event batch in `src/Api/Inventory.cs`.

Everything not listed here is internal. Main thread only.

Scope: the money, restock and price of a trader (`Trade`), and a mod's buy from or sell to a trader (`Traders.Buy` and `Traders.Sell`). Find a trader's id with `Shops.All()`. A trader is found by its GUID, as in `Shops.Stock`.

## Rules

- **Money.** `SetMoney` and `Restock` write the trader's money, in the trader's purchase currency (`TraderInfo.Currency`). The game saves that money with the trader, so it survives a save and a load. The money has no per-mod claim, so the last write wins, as with other plain writes. Both refuse while a trade runs. `Restock` draws a random amount between the trader's minimum and maximum and applies the difficulty rule (`DifficultyInfo.GetTraderWealth`), as the game does. It does not reset the trader's own timed restock, so the game still restocks on its timer.
- **Price factors.** `SetPriceFactors(modId, shopId, buy, sell)` sets one mod's factors for one trader. Every mod's factors multiply together. `buy` applies when the player buys from the trader, and `sell` when the player sells to the trader. A factor must be above 0 and at most 4. The factors apply to the game's own price, to `Shops.PriceOf` and `Shops.Stock`, to `Shops.PriceQuery`, and to `Traders.Buy` and `Traders.Sell`. A changed price is never below 1 OC for each unit (or the RM equivalent), the game's own minimum, so a factor or a `PriceQuery` handler cannot make an item free.
- **Session only.** Price factors are not saved. Every Single scene load clears them, so set them again from your `Events.SceneLoaded` handler. Clear your own with `ClearPriceFactors(modId, shopId)`. `SetPriceFactors` and `ClearPriceFactors` work inside `Events.SceneLoaded` and while a save loads or saves.
- **Buy and sell.** `Buy` and `Sell` run one trade through the game's own money, stock and price path, so the price factors and the `Shops.Bought`, `Shops.Sold` and `Shops.PriceQuery` events apply. The game's `Trade.Buy` and `Trade.Sell` move money only, so the framework moves the items as well. `Buy` gives the player the items first, then takes the units from the trader's stock, then takes the money. `Buy` takes only clean stock: units with no owner and no stolen mark that the game prices as a new item. Other stock, such as a stolen unit or one the game prices differently, stays on the shelf. A bought unit keeps the shelf unit's owner and item data (see Item copy). `Sell` is all or nothing, as `Buy` is. It takes `count` of the items from the player's stacks that the trader accepts, one stack at a time. It skips the stacks the trader does not accept, a stolen unit it does not buy or a unit that its allowed or forbidden categories rule out, and those units are not sold and stay with the player. It refuses, and changes nothing, when the accepted units are fewer than `count` (`NotAccepted` when the trader's categories rule out a held unit, otherwise `NotEnoughItems`), and when the trader cannot pay for the sale or has no room for it. Then it puts each stack in the trader's stock (unless the trader destroys purchased items), then pays the player for that stack. Each stack is priced with its own owner and meta, so its wear, quality and stolen mark count, and a sold unit keeps its owner and meta in the trader's stock.
- **Events.** `PlayerInventory.ItemAdded` and `PlayerInventory.ItemRemoved` are held until the trade ends, then raised once per item with the total moved. `Shops.Bought` and `Shops.Sold` are held the same way and raised when the trade commits, so their handlers never run during the trade. A buy raises one `Shops.Bought`, and a sale raises one `Shops.Sold` per stack. `Cash.MoneyChanged` is held the same way and runs only after the trade commits. A refused or rolled-back trade raises none of these events. A bought item's "New item" notice shows once, when the buy commits, with the item and the total count.
- **No trade inside a trade.** A trade cannot start while another trade runs or from inside a `Shops.PriceQuery` handler, and the call refuses with `TradeBusy`. A `Shops.PriceQuery` handler runs during a trade's price checks, so it can read and set the price, but while the trade runs these writes refuse: `Cash` writes (`Give`, `TrySpend`, and the banking and mail writes that check them), `PlayerInventory.Give` and `Take`, and `Traders.SetMoney` and `Restock`. Other framework writes, such as shop stock and equipment, are not gated.
- **Crime.** A trade commits no theft crime, even when it takes an owned unit. Selling stolen goods through `Traders.Sell` is not a crime either, as in the game's own trade screen.
- **Rollback.** A trade that fails after its first item step is undone, and the call returns the reason. Only the money the trade moved goes back, to the player and to the trader. The trader's stock and the player's inventory (the main, backpack and character slots) are written back slot by slot, into the same slots, with the same amount, owner, quality, liquid and other item data. A sale is refused with `NoRoom` when the trader's stock cannot take all of the units, so none is put on the floor. A purchase is refused with `NoRoom` when the player's inventory cannot take all of them. The undo does not take back the game's own callbacks that ran during a step.
- **Rollback limits.** The undo writes slots back directly, so the game's take and drop callbacks do not run again. If a sale of a worn item (hat, backpack, held item) fails after the item is taken, the item is back in its slot but the game may not treat it as equipped until it is moved. In rare slot cases a purchase can put one unit on the floor, and the call then refuses and rolls back, leaving that unit on the floor. No money is lost either way.
- **Saved.** A completed trade changes the game's saved data at the next save, as a trade on the trade screen does: the trader's money and trade balance, the trader's stock, and the player's inventory and money. The framework keeps no record of trades, so keep your own if you need one, in your own save data.
- **Refusals (trades).** `Buy` and `Sell` return `false` and set `refusal` to the reason. A refusal is logged once per trade kind and reason. A refusal before any change shows no game message. Refusals are listed below.
- **Availability.** `Traders.IsAvailable` is true when the trader reads and money writes work. `Traders.PriceFactorsAvailable` is true when the price hook is installed. Both return false when the framework is not ready.
- **Refusals.** A write returns `false` during a save or a load, while a trade runs, when the shop id is unknown, when the mod id is invalid, or when a value is not finite or is out of range. A read returns `null` for an unknown shop.

## Refusal reasons

- `None`: the trade was done.
- `NotReady`: the framework, the money or the inventory is not loaded yet.
- `BadInput`: the mod id is invalid, the count is below 1, or the item id is unknown.
- `SaveBusy`: a save or a load is running.
- `UnknownTrader`: no trader has that id.
- `Closed`: the trader is closed (`TraderInfo.Open` is false).
- `OutOfStock`: the trader has fewer than `count` units that `Buy` can take (buy). Those are units with no owner that the game prices as a new unit, so stolen units, and units priced differently such as worn ones, are skipped.
- `NoRoom`: the player has no room for all of the items (buy), or the trader has no room for all of them (sell).
- `PlayerCannotAfford`: the player cannot pay the game's price (buy).
- `TraderCannotAfford`: the trader cannot pay the game's price (sell).
- `NotEnoughItems`: the player holds fewer than `count` units that `Sell` can take (sell). A stolen unit counts only when the trader buys stolen goods, and never one stolen from that trader. A unit that the trader's categories rule out does not count. A sale that cannot take `count` units is refused and changes nothing. This reason is returned when no held unit is ruled out by the trader's categories, and `NotAccepted` otherwise.
- `NotAccepted`: the sale is refused and changes nothing, because the accepted units are fewer than `count` and the trader's allowed or forbidden categories rule out at least one unit the player holds (sell).
- `Failed`: a step threw or the game refused the money step. The trade was undone (see Rollback).
- `SpecialTrader`: the trader is a special trader (a `TradeSpecial`, which gives and takes certificates). The framework does not trade with it.
- `LiquidItem`: the item is a liquid, or holds one (a container with liquid). The liquid can change in place when stacks merge, and the undo cannot restore that.
- `TradeBusy`: another trade is running, or the call comes from a `Shops.PriceQuery` handler. Nothing changes.

## API

```csharp
public static class Traders
{
    public static bool IsAvailable { get; }
    public static bool PriceFactorsAvailable { get; }
    public static TraderInfo Get(string shopId);                                   // null when unknown
    public static bool SetMoney(string modId, string shopId, float amount);
    public static bool Restock(string modId, string shopId);
    public static bool SetPriceFactors(string modId, string shopId, float buy, float sell);
    public static bool ClearPriceFactors(string modId, string shopId);
    public static bool Buy(string modId, string traderId, int itemId, int count, out TraderTradeRefusal refusal);
    public static bool Sell(string modId, string traderId, int itemId, int count, out TraderTradeRefusal refusal);
}

public enum TraderTradeRefusal
{
    None, NotReady, BadInput, SaveBusy, UnknownTrader, Closed, OutOfStock, NoRoom,
    PlayerCannotAfford, TraderCannotAfford, NotEnoughItems, NotAccepted, Failed,
    SpecialTrader, LiquidItem, TradeBusy
}

public sealed class TraderInfo
{
    public string Id { get; }
    public bool Open { get; }                  // read only, see below
    public float Money { get; }
    public MoneyCurrency Currency { get; }
    public float BuyFactor { get; }            // product of all mods' buy factors, 1 when none
    public float SellFactor { get; }           // product of all mods' sell factors, 1 when none
    public bool BuysStolen { get; }
    public string[] AllowedCategories { get; }
    public string[] ForbiddenCategories { get; }
}
```

## Example

```csharp
TraderInfo shop = Traders.Get(shopId);
if (shop != null && shop.Money < 500f) Traders.SetMoney("MyMod", shopId, 500f);
Traders.SetPriceFactors("MyMod", shopId, 1.25f, 1f);

TraderTradeRefusal why;
if (!Traders.Buy("MyMod", shopId, itemId, 2, out why)) CoreLog.For("MyMod").Warn("buy refused: " + why);
if (!Traders.Sell("MyMod", shopId, itemId, 1, out why)) CoreLog.For("MyMod").Warn("sell refused: " + why);
```

## Not wrapped

- **Open.** `TraderInfo.Open` is read only. The game sets it from the trader's opening hours every in-game minute when `autoOpenClose` is on, and it does not stop a trade: the game's buy and sell checks do not read it. A setter would be overwritten, so there is none. `Traders.Buy` and `Traders.Sell` do check it, and refuse with `Closed`.
- **Item copy.** A bought unit keeps the shelf unit's owner and item data, such as its skin. Its cold (refrigerated) state is not kept. `Buy` takes only stock that the game prices as a new unit, so the price does not change. A sold unit keeps its own owner, quality and other item data in the trader's stock.
- **Stolen goods.** A sale takes a stolen unit only when the trader buys stolen goods, and never a unit stolen from that trader. The unit keeps its owner, so the game's stolen price factor applies to the sale. `Buy` never takes stolen stock.
- **Trade screen.** A trade does not check whether the trade screen is open for that trader.
- **Item price rules.** The price rules of the game (item value, stolen goods, containers) are unchanged. Price factors only scale the result, and the result is never below the game's own minimum.
