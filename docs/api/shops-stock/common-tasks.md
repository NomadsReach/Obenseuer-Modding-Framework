---
title: Common tasks
parent: Shop stock
grand_parent: Money and shops
nav_order: 1
---

# Common tasks

Give a trader a one-time starting stack. `Place` puts the items in once for each save and returns true when at least one item was placed.

```csharp
if (ShopStock.Place("example.applestall", "some-trader-id", 10620, 3)) Log.Info("placed 3 apples");
```

Sell an item in a vending machine at a fixed price of 15 OC, with room for 8.

```csharp
ShopStock.AddVendingEntry("example.applestall", "Some Vending Machine", 10620, 8, 15f, MoneyCurrency.OC);
```

Take back what your mod added for one item in one shop.

```csharp
ShopStock.Remove("example.applestall", "Some Trader", 10620);
```

Check that the feature works before you rely on it.

```csharp
if (!ShopStock.IsAvailable) Log.Warn("shop stock is off, the stock will not be added");
```
