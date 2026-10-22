---
title: Common tasks
parent: Traders
grand_parent: People
nav_order: 1
---

# Common tasks

Every example below is a complete mod. Each one runs once a save is loaded, because all of them need a trader from the scene. Each one takes the first trader from `Shops.All()` and checks what the call gave back. The limits are on the [money](changing-a-trader-s-money.md#limits), [buy and sell](buying-from-and-selling-to-a-trader.md#limits) and [prices](changing-prices.md#limits) pages.

## Give a trader enough money to pay the player

A shop id comes from `Shops.All()`, so there is no key to write down by hand. `SetMoney` returns `true` when the trader took the new amount.

```csharp
using ObenseuerCore;

[ModInfo("example.traderfund", "Trader Fund", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class TraderFund : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (ShopInfo shop in Shops.All())
            {
                if (shop.Kind != ShopKind.Trader)
                    continue;

                TraderInfo trader = Traders.Get(shop.Id);
                if (trader == null)
                {
                    Log.Warn("No trader was found for " + shop.Id + ". See the limits on the money page.");
                    break;
                }

                if (trader.Money >= 500f)
                {
                    Log.Info(shop.Name + " already holds " + trader.Money + ".");
                    break;
                }

                if (Traders.SetMoney(Mod.Id, shop.Id, 500f))
                    Log.Info("Gave " + shop.Name + " 500 money.");
                else
                    Log.Warn("SetMoney was refused for " + shop.Name + ". See the limits on the money page.");

                break;
            }
        });
    }
}
```

## Buy 2 apples from a trader for the player

`Buy` tells you why it refused through `refusal`. Item id 10620 is the apple.

```csharp
using ObenseuerCore;

[ModInfo("example.traderbuy", "Trader Buy", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class TraderBuy : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (ShopInfo shop in Shops.All())
            {
                if (shop.Kind != ShopKind.Trader)
                    continue;

                TraderTradeRefusal refusal;
                if (Traders.Buy(Mod.Id, shop.Id, 10620, 2, out refusal))
                    Log.Info("Bought 2 apples at " + shop.Name + ".");
                else
                    Log.Warn("The buy was refused: " + refusal + ". See the limits on the buy and sell page.");

                break;
            }
        });
    }
}
```

## Sell 1 apple from the player to a trader

```csharp
using ObenseuerCore;

[ModInfo("example.tradersell", "Trader Sell", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class TraderSell : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (ShopInfo shop in Shops.All())
            {
                if (shop.Kind != ShopKind.Trader)
                    continue;

                TraderTradeRefusal refusal;
                if (Traders.Sell(Mod.Id, shop.Id, 10620, 1, out refusal))
                    Log.Info("Sold 1 apple to " + shop.Name + ".");
                else
                    Log.Warn("The sale was refused: " + refusal + ". See the limits on the buy and sell page.");

                break;
            }
        });
    }
}
```

## Take your price factors off a trader

`ClearPriceFactors` returns `true` when the factors your mod set were removed.

```csharp
using ObenseuerCore;

[ModInfo("example.traderclearfactors", "Trader Clear Factors", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class TraderClearFactors : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (ShopInfo shop in Shops.All())
            {
                if (shop.Kind != ShopKind.Trader)
                    continue;

                if (Traders.ClearPriceFactors(Mod.Id, shop.Id))
                    Log.Info("Took the price factors off " + shop.Name + ".");
                else
                    Log.Warn("ClearPriceFactors was refused for " + shop.Name + ". See the limits on the prices page.");

                break;
            }
        });
    }
}
```
