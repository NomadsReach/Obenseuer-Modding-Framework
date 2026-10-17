---
title: Common tasks
parent: Fuel
grand_parent: Money and shops
nav_order: 1
---

# Common tasks

Find out how long one item burns in a generator. `Get` returns `null` when the item has no fuel data, so check the result. The number `10530` is Firewood:

```csharp
FuelTypeInfo fuel = FuelTypes.Get(10530);
if (fuel != null) Log.Info("It burns for " + fuel.BurnSeconds + " seconds a unit.");
```

Find the cheapest gas station in the scene:

```csharp
FuelStationInfo cheapest = null;
foreach (FuelStationInfo station in FuelStations.All())
    if (cheapest == null || station.Price < cheapest.Price) cheapest = station;
if (cheapest != null) Log.Info("Cheapest: " + cheapest.Id + " at " + cheapest.Price);
```

Check that the purchase event works before you rely on it:

```csharp
if (!FuelStations.IsAvailable) Log.Warn("The FuelBought event is off in this game version.");
```
