---
title: Shop stock
parent: Money and shops
grand_parent: API reference
has_children: true
nav_order: 9
---

# Shop stock

Status: experimental.

## What it is

The game has two kinds of shop you can add goods to: traders, who are people with a shop of their own, and vending machines. This API lets your mod put an item in a trader's shop (a fixed stack that is kept topped up) or add one more item for sale in a vending machine. Reading shops, stock and prices is on the [economy page](economy/shops.md#shops).

A trader's stock here is not a price list. A trader still prices an item by the item's own value, so you can't set a separate trader price for one item.

## When to use it

- Sell your mod's item at a trader, so the player can buy it without crafting.
- Add your mod's item to a vending machine, with your own price.
- Put a one-time starting stack of an item into a trader.
- Take back stock your mod added.

## Quick example

This mod keeps 5 apples in a trader's shop and adds apples to a vending machine for sale at the game's normal price. When the game is ready, it also logs every shop in the scene with its id, so you can find the right key.

```csharp
using ObenseuerCore;

[ModInfo("example.applestall", "Apple Stall", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class AppleStall : FrameworkMod
{
    protected override void OnLoad()
    {
        ShopStock.AddTraderStock("example.applestall", "Some Trader", 10620, 5);
        ShopStock.AddVendingEntry("example.applestall", "Some Vending Machine", 10620, 10, 0f, MoneyCurrency.OC);
        Events.WhenReady(() =>
        {
            foreach (ShopInfo shop in Shops.All())
                Log.Info(shop.Kind + " " + shop.Name + " " + shop.Id);
        });
    }
}
```

The first value you pass is your mod id (see [Mod id](../glossary.md#mod-id)). The second is the shop key: the id, the title or the name of the shop. Replace the two placeholder keys with ones from your log. The number 10620 is the apple's item id. A price of 0 means the machine uses the game's normal price. See [Using events](../guides/getting-started/using-events.md) for more on `WhenReady`.
