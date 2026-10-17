---
title: Common tasks
parent: Base shops
grand_parent: Money and shops
nav_order: 1
---

# Common tasks

Give every shop in the scene 500 OC and no RM, so it can pay for more sales.

```csharp
foreach (BaseShopInfo shop in BaseShops.All())
    BaseShops.SetMoney(shop.Id, 500, 0);
```

Make a shop restock. `Restock` returns `true` when the shop was told to restock.

```csharp
void RestockShop(string shopId)
{
    Log.Info("Restocked: " + BaseShops.Restock(shopId));
}
```

Buy one piece of furniture for the player. The result tells you what happened.

```csharp
void BuyOne(string shopId, string furnitureId)
{
    BaseShopBuyResult result = BaseShops.Buy(shopId, furnitureId, 1);
    Log.Info("Buy result: " + result);
}
```

Sell one piece of furniture from the player's stock to a shop.

```csharp
void SellOne(string shopId, string furnitureId)
{
    BaseShopSellResult result = BaseShops.Sell(shopId, furnitureId, 1);
    Log.Info("Sell result: " + result);
}
```
