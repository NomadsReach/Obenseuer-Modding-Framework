---
title: Common tasks
parent: Base shops
grand_parent: Money and shops
nav_order: 1
---

# Common tasks

Every example below runs once a save is loaded, because all of them need the shops that are in the world. Each one checks what the call gave back, and logs a warning when it was refused. The limits are on the [buying](buying.md#limits), [selling](selling.md#limits) and [details](details.md#what-to-check-for-and-what-comes-back) pages.

## Give every shop 500 OC and no RM

This runs once a save is loaded, so the shops are in the scene. It gives each shop 500 OC and no RM, so it can pay the player for more sales.

```csharp
using ObenseuerCore;

[ModInfo("example.shopfund", "Shop Fund", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class ShopFund : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (BaseShopInfo shop in BaseShops.All())
            {
                if (BaseShops.SetMoney(shop.Id, 500, 0))
                    Log.Info("Gave " + shop.Id + " 500 OC and 0 RM.");
                else
                    Log.Warn("Money was refused for " + shop.Id + ". See the limits on the details page.");
            }
        });
    }
}
```

## Make one shop restock

This runs once a save is loaded, then restocks the first shop it finds. `Restock` returns `true` when the shop was told to restock.

```csharp
using ObenseuerCore;

[ModInfo("example.restocker", "Restocker", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class Restocker : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (BaseShopInfo shop in BaseShops.All())
            {
                if (BaseShops.Restock(shop.Id))
                {
                    Log.Info("Asked " + shop.Id + " to restock.");
                    break;
                }

                Log.Warn("Restock was refused for " + shop.Id + ". See the limits on the details page.");
            }
        });
    }
}
```

## Buy one piece of furniture for the player

This runs once a save is loaded. It takes the first furniture id from the game's own catalogue, then asks the first furniture shop to sell one piece of it to the player.

```csharp
using System.Collections.Generic;
using ObenseuerCore;

[ModInfo("example.buypiece", "Buy One Piece", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class BuyOnePiece : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            List<FurnitureCatalogEntry> pieces = Building.Catalog();
            if (pieces.Count == 0)
            {
                Log.Warn("No furniture is in the catalogue yet.");
                return;
            }

            foreach (BaseShopInfo shop in BaseShops.All())
            {
                BaseShopBuyResult result = BaseShops.Buy(shop.Id, pieces[0].Id, 1);
                if (result != BaseShopBuyResult.Ok)
                {
                    Log.Warn("Buy was refused by " + shop.Id + ": " + result + ". See the limits on the buying page.");
                    continue;
                }

                Log.Info("Bought 1 x " + pieces[0].Name + " from " + shop.Id + ".");
                break;
            }
        });
    }
}
```

## Sell one piece of furniture from the player to a shop

This runs once a save is loaded. The player must already hold the piece in their building stock, so a refused call is normal when they do not.

```csharp
using System.Collections.Generic;
using ObenseuerCore;

[ModInfo("example.sellpiece", "Sell One Piece", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class SellOnePiece : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            List<FurnitureCatalogEntry> pieces = Building.Catalog();
            if (pieces.Count == 0)
            {
                Log.Warn("No furniture is in the catalogue yet.");
                return;
            }

            foreach (BaseShopInfo shop in BaseShops.All())
            {
                BaseShopSellResult result = BaseShops.Sell(shop.Id, pieces[0].Id, 1);
                if (result != BaseShopSellResult.Ok)
                {
                    Log.Warn("Sell was refused by " + shop.Id + ": " + result + ". See the limits on the selling page.");
                    continue;
                }

                Log.Info("Sold 1 x " + pieces[0].Name + " to " + shop.Id + ".");
                break;
            }
        });
    }
}
```