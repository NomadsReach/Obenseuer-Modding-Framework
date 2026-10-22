---
title: Traders
parent: People
grand_parent: API reference
has_children: true
nav_order: 15
---

# Traders

Status: experimental.

## What it is

Traders are the characters the player buys from and sells to on the trade screen. Each has some money, a stock of items, and prices. This API reads a trader's money, currency and what it will buy, changes how much money it has, refills it, scales its prices up or down, and lets your mod buy from or sell to a trader directly, without the trade screen. To find a trader's id, use `Shops.All()` on the [Economy](economy/shops.md#shops) page.

## When to use it

- Give a trader more money so the player can sell it a lot, or refill it on a schedule.
- Make a trader more expensive or cheaper for a while, for example during a mod event.
- Buy an item for the player from a trader, or sell the player's items to a trader, from your own code or menu.

## Quick example

This mod makes every trader charge 25 percent more when the player buys, and leave selling prices alone. The factors are forgotten when a new level loads, so it sets them again each time.

```csharp
using ObenseuerCore;

[ModInfo("example.pricewatch", "Price Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class PriceWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(Apply);
        Events.SceneLoaded += scene => Apply();
    }

    private void Apply()
    {
        foreach (ShopInfo shop in Shops.All())
        {
            if (shop.Kind != ShopKind.Trader)
                continue;

            if (!Traders.SetPriceFactors(Mod.Id, shop.Id, 1.25f, 1f))
                Log.Warn("The price factors were refused for " + shop.Id + ". See the limits on the changing prices page.");
        }
    }
}
```

`WhenReady` runs your code once the game is ready, and `SceneLoaded` runs it each time a level loads. See [Using events](../guides/getting-started/using-events.md) for more on both. The limits are on the [Changing prices](traders/changing-prices.md#limits) page. The first argument of `SetPriceFactors` is your mod's id, `Mod.Id`.
