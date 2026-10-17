---
title: Shops
parent: Economy
grand_parent: Money and shops
nav_order: 4
---

# Shops

`Shops` is for the NPC traders (shops run by a character) and the vending machines. You can list them, read their stock and prices, change what they charge, and get told about trades. Each shop is a `ShopInfo`, a snapshot taken when you asked.

```csharp
public enum ShopKind
{
    Trader = 0,    // a shop run by an NPC
    Vending = 1    // a vending machine
}

public sealed class ShopInfo    // one shop
{
    public string Id { get; }       // names this shop; use it with Stock and PriceOf
    public string Name { get; }     // the shop's title, or its object name when it has none
    public ShopKind Kind { get; }   // trader or vending machine
}

public sealed class StockItem    // one line of a shop's stock
{
    public int ItemId { get; }               // the item id
    public string Name { get; }              // the item's name
    public int Count { get; }                // how many the shop has
    public float UnitPrice { get; }          // the price of one
    public MoneyCurrency Currency { get; }   // the money the price is in
}

public sealed class TradeArgs    // what you get when a trade happens
{
    public int ItemId { get; }               // the item that was traded
    public int Count { get; }                // how many were asked for
    public float Price { get; }              // the money that really moved, in Currency
    public MoneyCurrency Currency { get; }   // the money used
    public ShopInfo Shop { get; }            // the shop
}

public sealed class PriceQueryArgs    // what you get when the game asks for a trader's price
{
    public ShopInfo Shop { get; }            // the shop
    public int ItemId { get; }               // the item
    public int Count { get; }                // how many the price is for
    public bool PlayerBuys { get; }          // true: the player buys from the shop. false: the player sells to it
    public MoneyCurrency Currency { get; }   // the money the price is in
    public float Price { get; set; }         // the price for the whole stack of Count; your handler can change it
}

public static class Shops
{
    public static event Action<TradeArgs> Bought;           // the player bought something
    public static event Action<TradeArgs> Sold;             // the player sold something
    public static event Action<PriceQueryArgs> PriceQuery;  // the game is working out a trader's price

    public static bool IsAvailable { get; }                 // false when a shop event could not be set up
    public static List<ShopInfo> All();                     // every trader and vending machine in the scene you are in
    public static ShopInfo Open();                          // the trader whose trade screen is open; null when none
    public static List<StockItem> Stock(string shopId);     // what the shop has for sale
    public static float PriceOf(string shopId, int itemId, int count, bool playerBuys, out MoneyCurrency currency);   // the price for a stack
}
```

- **`All()` lists the NPC traders and the vending machines.** A shop that has no id is left out and can't be addressed. `Open()` gives the trader whose trade screen is open right now, or `null`. Stock and prices are looked up by item id.
  - Technical: traders are `Trade` objects, including `TradeSpecial`. `Open()` reads `TradePanel.activeTrade`.
- **A trader's price is worked out for a brand new item.** The trader sells at its buying-price setting times the item's value, and buys at its selling-price setting. The money (OC or RM) follows the trader's own price settings, as in the game. Because it ignores the liquid, quality or container state of a real item, it can differ from the price of one particular item in the world.
  - Technical: the trader's `basePurchasePrice` and `baseSellPrice`, applied to a fresh item with no meta.
- **A vending machine's stock is every item it sells, with its count and price.** `PriceOf` gives a price for a vending machine only when `playerBuys` is `true`, because a machine does not buy from the player.
  - Technical: the price is the entry's `GetPrice()`.
- **`Bought` and `Sold` fire after a trade really went through.** `Price` is the money that moved, taken from the balance before and after, so it matches what the player really paid or got. Any other money change made by a handler or game script during the same trade is counted in it too. `Count` is how many were asked for, and for a vending machine it is always 1. A vending machine raises `Bought` only.
  - Technical: they follow a successful game `Trade.Buy` or `Trade.Sell` (and the `TradeSpecial` overrides), or a successful `VendingMachine.Buy`.
- **A trade made with `Traders.Buy` or `Traders.Sell` raises its events when the trade is finished, and none if it is refused or cancelled.** See [Traders](../traders.md).
- **The framework can make trades with traders for you.** `Traders.Buy` and `Traders.Sell` buy from or sell to a trader by id and item id, and give a reason when they refuse. The trade needs the game's own slot objects, and those calls handle that. See [Traders](../traders.md).

How `PriceQuery` works. The game asks for a trader's price all the time: for price checks, for labels and for every trade. `PriceQuery` lets your handler change the price each time.

- **`PriceQuery` fires for every trader price the game works out, but not for vending machine prices.** It also runs during a trader trade's own price checks. There your handler can read and set the price, but can't start a trader trade, and the framework's money and item writes are refused too (see [Traders](../traders.md)).
  - Technical: it is raised from a postfix on the game's `Trade.GetItemStackPrice`.
- **Handlers run in the order they were added, and each one sees the price the last one left.** Each may set `Price`. A value that is not a real number is ignored, and the price stays as it was.
- **After all handlers have run, a changed price is adjusted to fit the game's rules.** It is at least 1 OC for each unit (or the RM equivalent), at most 16,000,000, and rounded like the game does: OC to whole units, RM to 0.1. A handler can't make an item free, because that minimum is the game's own.
- **With no `PriceQuery` handler, the framework does almost nothing on the game's frequent price calls.** An unchanged price is returned exactly as the game worked it out.
- **Inside a `PriceQuery` handler, `Shops.PriceOf` and the game's own price calls give the price before any changes.** `PriceQuery` does not run again inside itself.
- **`PriceOf` includes the same adjustments, so it matches what the game charges.**
