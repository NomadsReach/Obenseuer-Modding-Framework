---
title: Gas stations
parent: Fuel
grand_parent: Money and shops
nav_order: 2
---

# Gas stations

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`FuelStations.All()` lists the gas stations in the scene you are in. Each one is a `FuelStationInfo`, a snapshot of the station at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public sealed class FuelStationInfo    // a snapshot of one gas station
{
    public string Id { get; }                // its save id, or when it has none a text path of where it sits in the scene
    public int Price { get; }                // the price of one unit of fuel, as the game stores it
    public MoneyCurrency Currency { get; }   // the money the price is in: OC or RM
}

public static class FuelStations
{
    public static List<FuelStationInfo> All();   // every gas station in the active scene
    public static bool IsAvailable { get; }      // false when the purchase event could not be set up
}
```

- **`All()` lists the gas stations in the scene you are standing in.** Stations in other scenes, and stations that are switched off, are not listed. With no save loaded it returns an empty list.
  - Technical: it lists the active `GasStation` objects in the active scene (Unity's default for `FindObjectsByType` skips inactive ones). `Id` is the station's `GUID`, or its scene path when it has none.
- **`Price` is for one unit of fuel, not one litre.** The pump multiplies the price by the amount and rounds up to a whole number.
  - Technical: it is `GasStation.currentFuelPrice`. The pump bills `ceil(price * amount)` (`FuelPump.GetPrice`).
- **The price changes now and then.** The game picks a new random price for each station on a timer, so ask again if you need the current one.
- **A station priced in RM does not raise `FuelBought`.** Real Money (RM, see the [glossary](../../glossary.md#rm)) is added up on a separate bill, and nothing here pays it. The game records a theft crime for any fuel still unpaid when a minitrain leaves the pump's fuel area. That is not a purchase, so no event is raised for it.
  - Technical: `Currency` is `GasStation.currency`. A pump with RM currency feeds `GasStation.UpdatePriceToPayRm`. The crime is `GasStation.GiveCrimePoints`, called from `MinitrainFuelArea.OnTriggerExit`. The event only follows `GasStation.PayFuelOc`, which pays in OC.
- **You can't change a station's price.** The game picks the price itself and a change you made would last only until the next random price. For now you can only read the price.
  - Technical: the price is the `protected` field `GasStation.currentFuelPrice`, and the only game write is `SetRandomPrice` on a random timer.
