---
title: Gas stations
parent: Fuel
grand_parent: Money and shops
nav_order: 2
---

# Gas stations

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail you can skip on a first read.

`FuelStations.All()` lists the gas stations in the scene you are in. Each one is a `FuelStationInfo`, a snapshot of the station at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FuelStations</code> <span>list the gas stations</span></p>

This is the class you start from. `All()` lists the gas stations, and `IsAvailable` tells you whether the purchase event works. The purchase event is on the [buying fuel](buying-fuel.md) page.

<div class="api-pair" markdown="1">

```csharp
public static class FuelStations
{
    public static List<FuelStationInfo> All();
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<FuelStationInfo>` | Every gas station in the active scene. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the purchase event could not be set up. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FuelStationInfo</code> <span>a snapshot of one gas station</span></p>

`FuelStations.All()` gives you this, one per gas station in the scene. It is a copy of the station when you asked, so ask again for a current `Price`. <span class="tech">(It is filled from the game's `GasStation` fields `currentFuelPrice` and `currency`.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class FuelStationInfo
{
    public string Id { get; }
    public int Price { get; }
    public MoneyCurrency Currency { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | Its save id. When it has none, a text path of where it sits in the scene. |
| `Price` | `int` | The price of one unit of fuel, as the game stores it. |
| `Currency` | `MoneyCurrency` | The money the price is in. OC or RM. |

</div>
</div>

## Things to know

- `All()` lists the gas stations in the scene you are standing in. Stations in other scenes, and stations that are switched off, are not listed. With no save loaded it returns an empty list. <span class="tech">(It lists the active `GasStation` objects in the active scene. Unity's default for `FindObjectsByType` skips inactive ones. `Id` is the station's `GUID`, or its scene path when it has none.)</span>
- `Price` is for one unit of fuel, not one litre. The pump multiplies the price by the amount and rounds up to a whole number. <span class="tech">(It is `GasStation.currentFuelPrice`. The pump bills `ceil(price * amount)` (`FuelPump.GetPrice`).)</span>
- The price changes now and then. The game picks a new random price for each station on a timer, so ask again if you need the current one.

## Limits

- A station priced in RM does not raise `FuelBought`. Real Money (RM, see the [glossary](../../glossary.md#rm)) is added up on a separate bill, and nothing here pays it. The game records a theft crime for any fuel still unpaid when a minitrain leaves the pump's fuel area. That is not a purchase, so no event is raised for it. <span class="tech">(`Currency` is `GasStation.currency`. A pump with RM currency feeds `GasStation.UpdatePriceToPayRm`. The crime is `GasStation.GiveCrimePoints`, called from `MinitrainFuelArea.OnTriggerExit`. The event only follows `GasStation.PayFuelOc`, which pays in OC.)</span>
- You can't change a station's price. The game picks the price itself and a change you made would last only until the next random price. For now you can only read the price. <span class="tech">(The price is the `protected` field `GasStation.currentFuelPrice`, and the only game write is `SetRandomPrice` on a random timer.)</span>
