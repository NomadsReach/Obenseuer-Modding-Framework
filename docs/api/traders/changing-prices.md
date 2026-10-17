---
title: Changing prices
parent: Traders
grand_parent: People
nav_order: 4
---

# Changing prices

`SetPriceFactors` and `ClearPriceFactors` scale one trader's prices.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Traders</code> <span>scales one trader's prices</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Traders
{
    public static bool SetPriceFactors(string modId, string shopId, float buy, float sell);
    public static bool ClearPriceFactors(string modId, string shopId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetPriceFactors(string modId, string shopId, float buy, float sell)` | `bool` | Scales one trader's prices. Returns `true` when set. |
| `ClearPriceFactors(string modId, string shopId)` | `bool` | Removes your factors from one trader. |

</div>
</div>

## Things to know

- A price factor scales a trader's prices. `buy` scales what the player pays when buying from the trader. `sell` scales what the player gets when selling to the trader. A factor of 1.25 means 25 percent more. A factor must be above 0 and at most 4.
- Each mod has its own factors, and all mods' factors are multiplied together. `TraderInfo.BuyFactor` and `SellFactor` show the result.
- Factors apply everywhere the game and the framework show or charge a price. That is the game's own trade screen, `Shops.PriceOf`, `Shops.Stock`, the `Shops.PriceQuery` event, and `Traders.Buy` and `Traders.Sell`.
- Price factors are not saved, and every normal level load clears them. Set them again from an `Events.SceneLoaded` handler, as the quick example does. `SetPriceFactors` and `ClearPriceFactors` also work inside `Events.SceneLoaded` and while a save is loading or being written. <span class="tech">(every Single scene load clears them)</span>
- `PriceFactorsAvailable` is `false` when price factors don't work. Then `SetPriceFactors` returns `false`. <span class="tech">(it is true when the price hook (`Trade.GetItemStackPrice`) is installed)</span>

## Limits

- A price never goes below 1 OC for each unit (or the same amount in RM). That is the game's own lowest price, so a factor or a `Shops.PriceQuery` handler can't make an item free.
