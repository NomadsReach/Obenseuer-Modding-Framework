---
title: Changing prices
parent: Traders
grand_parent: People
nav_order: 4
---

# Changing prices

```csharp
public static class Traders
{
    public static bool SetPriceFactors(string modId, string shopId, float buy, float sell);   // scale one trader's prices; true when set
    public static bool ClearPriceFactors(string modId, string shopId);                         // remove your factors from one trader
}
```

- **A price factor scales a trader's prices.** `buy` scales what the player pays when buying from the trader. `sell` scales what the player gets when selling to the trader. A factor of 1.25 means 25 percent more. A factor must be above 0 and at most 4.
- **Each mod has its own factors, and all mods' factors are multiplied together.** `TraderInfo.BuyFactor` and `SellFactor` show the result.
- **Factors apply everywhere the game and the framework show or charge a price.** That is the game's own trade screen, `Shops.PriceOf`, `Shops.Stock`, the `Shops.PriceQuery` event, and `Traders.Buy` and `Traders.Sell`.
- **A price never goes below 1 OC for each unit (or the same amount in RM).** That is the game's own lowest price, so a factor or a `Shops.PriceQuery` handler can't make an item free.
- **Price factors are not saved, and every normal level load clears them.** Set them again from an `Events.SceneLoaded` handler, as the quick example does. `SetPriceFactors` and `ClearPriceFactors` also work inside `Events.SceneLoaded` and while a save is loading or being written.
  - Technical: every Single scene load clears them.
- **`PriceFactorsAvailable` is `false` when price factors don't work.** Then `SetPriceFactors` returns `false`.
  - Technical: it is true when the price hook (`Trade.GetItemStackPrice`) is installed.
