---
title: Common tasks
parent: Shop stock
grand_parent: Money and shops
nav_order: 1
---

# Common tasks

Every example below is a complete mod. Each one runs once a save is loaded, because all of them need the shops in the world. Each one takes a shop from `Shops.All()` and checks what the call gave back. The limits are on the [traders](traders.md#limits), [vending machines](vending-machines.md#limits) and [shop keys](shop-keys.md#limits) pages.

## Put a one-time starting stack in a trader

`Place` puts the items in once for each save and returns `true` when at least one item was placed.

```csharp
using ObenseuerCore;

[ModInfo("example.appleplace", "Apple Place", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class ApplePlace : FrameworkMod
{
    protected override void OnLoad()
    {
        if (!ShopStock.IsAvailable)
        {
            Log.Warn("Shop stock is off, so nothing can be added. See the limits on the shop keys page.");
            return;
        }

        Events.WhenReady(() =>
        {
            foreach (ShopInfo shop in Shops.All())
            {
                if (shop.Kind != ShopKind.Trader)
                    continue;

                if (ShopStock.Place(Mod.Id, shop.Id, 10620, 3))
                    Log.Info("Placed 3 apples in " + shop.Name + ".");
                else
                    Log.Warn("Place was refused for " + shop.Name + ". See the limits on the traders page.");

                break;
            }
        });
    }
}
```

## Sell an item in a vending machine at your own price

`AddVendingEntry` adds one entry for sale. This one charges a fixed 15 OC, with room for 8. The limits are on the [vending machines](vending-machines.md#limits) page.

```csharp
using ObenseuerCore;

[ModInfo("example.applevending", "Apple Vending", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class AppleVending : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (ShopInfo shop in Shops.All())
            {
                if (shop.Kind != ShopKind.Vending)
                    continue;

                if (ShopStock.AddVendingEntry(Mod.Id, shop.Id, 10620, 8, 15f, MoneyCurrency.OC))
                    Log.Info("Added apples to " + shop.Name + " at 15 OC.");
                else
                    Log.Warn("The vending entry was refused for " + shop.Name + ". See the limits on the vending machines page.");

                break;
            }
        });
    }
}
```

## Take back what your mod added

`Remove` takes back one item in one shop that your mod added. The limits are on the [traders](traders.md#limits) and [shop keys](shop-keys.md#limits) pages.

```csharp
using ObenseuerCore;

[ModInfo("example.appleremove", "Apple Remove", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class AppleRemove : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (ShopInfo shop in Shops.All())
            {
                if (shop.Kind != ShopKind.Trader)
                    continue;

                if (ShopStock.Remove(Mod.Id, shop.Id, 10620))
                    Log.Info("Took the apples back from " + shop.Name + ".");
                else
                    Log.Warn("Remove was refused for " + shop.Name + ". See the limits on the traders page.");

                break;
            }
        });
    }
}
```

## Check that the feature works before you rely on it

`IsAvailable` is `false` when the game update moved the methods the framework needs. The limits are on the [shop keys](shop-keys.md#limits) page.

```csharp
using ObenseuerCore;

[ModInfo("example.stockcheck", "Stock Check", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class StockCheck : FrameworkMod
{
    protected override void OnLoad()
    {
        if (!ShopStock.IsAvailable)
            Log.Warn("Shop stock is off, so the stock will not be added. See the limits on the shop keys page.");
    }
}
```
