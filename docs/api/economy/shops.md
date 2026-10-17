---
title: Shops
parent: Economy
grand_parent: Money and shops
nav_order: 4
---

# Shops

`Shops` is for the NPC traders (shops run by a character) and the vending machines. You can list them, read their stock and prices, change what they charge, and get told about trades. Each shop is a `ShopInfo`, a snapshot taken when you asked.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Shops</code> <span>list shops, read prices, and hear about trades</span></p>

Call `All()` to list the shops, `Stock(shopId)` for what one has, and `PriceOf` for a price. Subscribe to `Bought`, `Sold` or `PriceQuery` to hear about trades.

<div class="api-pair" markdown="1">

```csharp
public static class Shops
{
    public static event Action<TradeArgs> Bought;
    public static event Action<TradeArgs> Sold;
    public static event Action<PriceQueryArgs> PriceQuery;
    public static bool IsAvailable { get; }
    public static List<ShopInfo> All();
    public static ShopInfo Open();
    public static List<StockItem> Stock(string shopId);
    public static float PriceOf(string shopId, int itemId, int count, bool playerBuys, out MoneyCurrency currency);
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<ShopInfo>` | Every trader and vending machine in the scene you are in. |
| `Open()` | `ShopInfo` | The trader whose trade screen is open. `null` when none. |
| `Stock(shopId)` | `List<StockItem>` | What the shop has for sale. |
| `PriceOf(shopId, itemId, count, playerBuys, out currency)` | `float` | The price for a stack. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when a shop event could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Bought` | `TradeArgs` | The player bought something. |
| `Sold` | `TradeArgs` | The player sold something. |
| `PriceQuery` | `PriceQueryArgs` | The game is working out a trader's price. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ShopInfo</code> <span>one shop</span></p>

`Shops.All()` gives you one of these for each shop. Pass its `Id` to `Stock` and `PriceOf`.

<div class="api-pair" markdown="1">

```csharp
public sealed class ShopInfo
{
    public string Id { get; }
    public string Name { get; }
    public ShopKind Kind { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | Names this shop. Use it with `Stock` and `PriceOf`. |
| `Name` | `string` | The shop's title, or its object name when it has none. |
| `Kind` | `ShopKind` | Trader or vending machine. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>StockItem</code> <span>one line of a shop's stock</span></p>

`Shops.Stock(shopId)` returns one of these for each line of a shop's stock. `UnitPrice` is the price of one.

<div class="api-pair" markdown="1">

```csharp
public sealed class StockItem
{
    public int ItemId { get; }
    public string Name { get; }
    public int Count { get; }
    public float UnitPrice { get; }
    public MoneyCurrency Currency { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ItemId` | `int` | The item id. |
| `Name` | `string` | The item's name. |
| `Count` | `int` | How many the shop has. |
| `UnitPrice` | `float` | The price of one. |
| `Currency` | `MoneyCurrency` | The money the price is in. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TradeArgs</code> <span>what you get when a trade happens</span></p>

`Bought` and `Sold` pass you one of these. `Price` is the money that really moved, and `Shop` says where it happened.

<div class="api-pair" markdown="1">

```csharp
public sealed class TradeArgs
{
    public int ItemId { get; }
    public int Count { get; }
    public float Price { get; }
    public MoneyCurrency Currency { get; }
    public ShopInfo Shop { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ItemId` | `int` | The item that was traded. |
| `Count` | `int` | How many were asked for. |
| `Price` | `float` | The money that really moved, in `Currency`. |
| `Currency` | `MoneyCurrency` | The money used. |
| `Shop` | `ShopInfo` | The shop. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PriceQueryArgs</code> <span>what you get when the game asks for a trader's price</span></p>

`PriceQuery` gives you one of these while the game works out a trader's price. Set `Price` in your handler to change it.

<div class="api-pair" markdown="1">

```csharp
public sealed class PriceQueryArgs
{
    public ShopInfo Shop { get; }
    public int ItemId { get; }
    public int Count { get; }
    public bool PlayerBuys { get; }
    public MoneyCurrency Currency { get; }
    public float Price { get; set; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Shop` | `ShopInfo` | The shop. |
| `ItemId` | `int` | The item. |
| `Count` | `int` | How many the price is for. |
| `PlayerBuys` | `bool` | `true` when the player buys from the shop. `false` when the player sells to it. |
| `Currency` | `MoneyCurrency` | The money the price is in. |
| `Price` | `float` | The price for the whole stack of `Count`. Your handler can change it. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ShopKind</code> <span>a trader or a vending machine</span></p>

`ShopInfo.Kind` holds one of these: `Trader` for a shop run by an NPC, and `Vending` for a vending machine.

<div class="api-pair" markdown="1">

```csharp
public enum ShopKind
{
    Trader = 0,
    Vending = 1
}
```

| Value | Meaning |
|---|---|
| `Trader` | A shop run by an NPC. |
| `Vending` | A vending machine. |

</div>
</div>

### Things to know

- `All()` lists the NPC traders and the vending machines. A shop that has no id is left out and can't be addressed. `Open()` gives the trader whose trade screen is open right now, or `null`. Stock and prices are looked up by item id. <span class="tech">(traders are `Trade` objects, including `TradeSpecial`. `Open()` reads `TradePanel.activeTrade`.)</span>
- A trader's price is worked out for a brand new item. The trader sells at its buying-price setting times the item's value, and buys at its selling-price setting. The money (OC or RM) follows the trader's own price settings, as in the game. <span class="tech">(the trader's `basePurchasePrice` and `baseSellPrice`, applied to a fresh item with no meta.)</span>
- Because it ignores the liquid, quality or container state of a real item, a trader's price can differ from the price of one particular item in the world.
- A vending machine's stock is every item it sells, with its count and price. `PriceOf` gives a price for a vending machine only when `playerBuys` is `true`, because a machine does not buy from the player. <span class="tech">(the price is the entry's `GetPrice()`.)</span>
- `Bought` and `Sold` fire after a trade really went through. `Price` is the money that moved, taken from the balance before and after, so it matches what the player really paid or got. `Count` is how many were asked for, and for a vending machine it is always 1. A vending machine raises `Bought` only. <span class="tech">(they follow a successful game `Trade.Buy` or `Trade.Sell` (and the `TradeSpecial` overrides), or a successful `VendingMachine.Buy`.)</span>
- Any other money change made by a handler or game script during the same trade is counted in `Price` too.
- A trade made with `Traders.Buy` or `Traders.Sell` raises its events when the trade is finished, and none if it is refused or cancelled. See [Traders](../traders.md).
- The framework can make trades with traders for you. `Traders.Buy` and `Traders.Sell` buy from or sell to a trader by id and item id, and give a reason when they refuse. The trade needs the game's own slot objects, and those calls handle that. See [Traders](../traders.md).

### How `PriceQuery` works

The game asks for a trader's price all the time: for price checks, for labels and for every trade. `PriceQuery` lets your handler change the price each time.

- `PriceQuery` fires for every trader price the game works out. It also runs during a trader trade's own price checks. <span class="tech">(it is raised from a postfix on the game's `Trade.GetItemStackPrice`.)</span>
- Handlers run in the order they were added, and each one sees the price the last one left. Each may set `Price`. A value that is not a real number is ignored, and the price stays as it was.
- After all handlers have run, a changed price is adjusted to fit the game's rules. It is at least 1 OC for each unit (or the RM equivalent), at most 16,000,000, and rounded like the game does: OC to whole units, RM to 0.1. A handler can't make an item free, because that minimum is the game's own.
- With no `PriceQuery` handler, the framework does almost nothing on the game's frequent price calls. An unchanged price is returned exactly as the game worked it out.
- `PriceOf` includes the same adjustments, so it matches what the game charges.

### Limits

- `PriceQuery` is not raised for vending machine prices.
- Inside a `PriceQuery` handler, `Shops.PriceOf` and the game's own price calls give the price before any changes. `PriceQuery` does not run again inside itself.
- During a trader trade, a `PriceQuery` handler can read and set the price, but can't start a trader trade, and the framework's money and item writes are refused too (see [Traders](../traders.md)).
