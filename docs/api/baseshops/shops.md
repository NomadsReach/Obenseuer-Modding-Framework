---
title: Shops
parent: Base shops
grand_parent: Money and shops
nav_order: 2
---

# Shops

This is the full list of what the API gives you.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BaseShops</code> <span>the static class for base shops</span></p>

Use `All` to list the shops in the scene, `SetMoney` and `Restock` to change a shop, and `Buy` and `Sell` to trade furniture with one.

<div class="api-pair" markdown="1">

```csharp
public static class BaseShops
{
    public static List<BaseShopInfo> All();
    public static bool SetMoney(string id, int oc, int rm);
    public static bool Restock(string id);
    public static BaseShopBuyResult Buy(string id, string furnitureId, int count);
    public static BaseShopSellResult Sell(string id, string furnitureId, int count);
    public static event Action<BaseShopBoughtArgs> Bought;
    public static event Action<BaseShopSoldArgs> Sold;
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<BaseShopInfo>` | Every shop in the scene you are in. |
| `SetMoney(id, oc, rm)` | `bool` | Sets how much money the shop holds. `true` when it was set. |
| `Restock(id)` | `bool` | Makes the shop restock now. `true` when it was told to. |
| `Buy(id, furnitureId, count)` | `BaseShopBuyResult` | Buys furniture from a shop for the player. See [Buying](buying.md). |
| `Sell(id, furnitureId, count)` | `BaseShopSellResult` | Sells furniture from the player to a shop. See [Selling](selling.md). |

| Event | Gives you | When it fires |
|---|---|---|
| `Bought` | `BaseShopBoughtArgs` | The player bought furniture through `Buy`. |
| `Sold` | `BaseShopSoldArgs` | The player sold furniture through `Sell`. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BaseShopInfo</code> <span>a snapshot of one shop</span></p>

`All` gives you one of these for each shop. Pass its `Id` to the other calls.

<div class="api-pair" markdown="1">

```csharp
public sealed class BaseShopInfo
{
    public string Id { get; }
    public string Kind { get; }
    public int Oc { get; }
    public int Rm { get; }
    public bool RestockDaily { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The shop's id. Pass it to the other calls. |
| `Kind` | `string` | The type of shop, such as `FurnitureShop` or `TrainShop`. |
| `Oc` | `int` | How many OC the shop holds. |
| `Rm` | `int` | How many RM the shop holds. |
| `RestockDaily` | `bool` | `true` when the shop restocks every day. `false` when it restocks on a timer. |

</div>
</div>

## Things to know

- `All()` lists the shops that are switched on in the scene you are in. A shop in another scene, or one the game has hidden, is not listed. With no save loaded you get an empty list. <span class="tech">(It searches the active objects of the active scene for `BaseShop`. `Kind` is the C# type name.)</span>
- A shop id is a piece of text that names one shop. `All()` gives it to you, and it is stable as long as the scene does not change. A null or empty id finds nothing. <span class="tech">(The id is the shop's save GUID when it has one, otherwise its scene path.)</span>
- `SetMoney(id, oc, rm)` sets how much money the shop holds. The shop pays the player out of this money when it buys furniture, so the new amounts limit what it can pay.
- `SetMoney` returns `false` for a negative amount, for an unknown shop, and while a save is loading or being written. A shop screen that is already open can show the old amounts until it refreshes. <span class="tech">(It writes `currentOcMoney` and `currentRmMoney` directly, because the game has no setter. The sale checks read those fields.)</span>
- `Restock(id)` makes the shop restock now. It draws new amounts of money for the shop and refreshes its stock.
- `Restock` does not reset the shop's restock timer, so the next restock the game planned still happens when it was planned. It returns `false` for an unknown shop and while a save is loading or being written. <span class="tech">(It calls the game's `Restock()`, which draws new OC and RM amounts in the shop's ranges and then calls `UpdateShopItems()`, which a subclass overrides. `timeToRestock` and `restockedDay` keep their values. For a furniture shop this runs the framework's furniture changes on `UpdateShopItems`.)</span>
- `RestockDaily` tells you when the shop restocks by itself. When it is `true`, the shop restocks at the start of each new day. Otherwise it restocks on its own timer, after a time chosen between its smallest and largest restock time. <span class="tech">(The daily restock happens on the first minute tick of a new day. The timer runs between `minRestockingTime` and `maxRestockingTime` seconds.)</span>
- The list of what a shop has in stock belongs to each kind of shop, and furniture stock is on the [furniture page](../furniture.md). The price percentages and the stock amount ranges are not available. <span class="tech">(Each subclass owns its stock list, for example `FurnitureShop.availableFurnitures`. The percentages are `sellPercent` and `buyPercent`.)</span>
