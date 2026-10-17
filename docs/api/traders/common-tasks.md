---
title: Common tasks
parent: Traders
grand_parent: People
nav_order: 1
---

# Common tasks

Give a trader at least 500 money to pay the player with. `shopId` comes from `Shops.All()`.

```csharp
string shopId = "put a shop id from Shops.All() here";
TraderInfo trader = Traders.Get(shopId);
if (trader != null && trader.Money < 500f) Traders.SetMoney(Mod.Id, shopId, 500f);
```

Buy 2 apples (item id 10620) from a trader for the player. `Buy` tells you why it refused through `refusal`.

```csharp
string shopId = "put a shop id from Shops.All() here";
TraderTradeRefusal refusal;
if (!Traders.Buy(Mod.Id, shopId, 10620, 2, out refusal))
    Log.Warn("The buy was refused: " + refusal);
```

Sell 1 apple from the player to a trader.

```csharp
string shopId = "put a shop id from Shops.All() here";
TraderTradeRefusal refusal;
if (!Traders.Sell(Mod.Id, shopId, 10620, 1, out refusal))
    Log.Warn("The sale was refused: " + refusal);
```

Take your price factors off a trader.

```csharp
string shopId = "put a shop id from Shops.All() here";
Traders.ClearPriceFactors(Mod.Id, shopId);
```
