---
title: Base shops
parent: Money and shops
grand_parent: API reference
has_children: true
nav_order: 1
---

# Base shops

Status: experimental.

## What it is

The game has shops where the player buys furniture for their home and sells furniture back. Each shop holds some money of its own, in both currencies, and its stock changes when it restocks. This API lets your mod list those shops, and see and set the money a shop holds. It can make a shop restock, buy furniture from a shop for the player, and sell furniture from the player to a shop. It also tells you when either happens.

It works with furniture shops. The shop that sells trains is listed too, but you can't buy from it or sell to it through this API (see the rules below). Market stalls are on the [market shops](marketshops.md) page.

## When to use it

- Show which shops are in the scene and how much money each one has.
- Give a shop more money, so it can buy more from the player.
- Make a shop restock at a moment you choose.
- Buy or sell furniture for the player from your mod, for example as part of a quest.
- React when the player or another mod buys from or sells to a shop.

## Quick example

This mod writes a log line each time furniture is bought from or sold to a shop through this API. Once a save is loaded, it also logs each shop in the scene and the money it holds.

```csharp
using ObenseuerCore;

[ModInfo("example.shopwatch", "Shop Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ShopWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        BaseShops.Bought += args => Log.Info(args.Count + " x " + args.FurnitureId + " bought for " + args.Oc + " OC and " + args.Rm + " RM.");
        BaseShops.Sold += args => Log.Info(args.Count + " x " + args.FurnitureId + " sold for " + args.Oc + " OC and " + args.Rm + " RM.");
        Events.WhenReady(() =>
        {
            foreach (BaseShopInfo shop in BaseShops.All())
                Log.Info(shop.Kind + " " + shop.Id + " holds " + shop.Oc + " OC and " + shop.Rm + " RM.");
        });
    }
}
```

OC is Open Sewer Coins and RM is Real Money (see the [glossary](../glossary.md#oc)). `Bought` and `Sold` are events: the framework calls your code each time they happen. `Events.WhenReady` runs your code once a save is loaded, or at once if one already is. See [Using events](../guides/getting-started/using-events.md).
