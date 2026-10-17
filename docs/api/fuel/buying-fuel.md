---
title: Buying fuel
parent: Fuel
grand_parent: Money and shops
nav_order: 3
---

# Buying fuel

`FuelBought` tells your mod when the player has paid for fuel at a station.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FuelStations</code> <span>fuel purchase event</span></p>

`FuelBought` is the event on this class. `FuelStations.All()` lists the stations, and it is covered on the [gas stations](gas-stations.md) page.

<div class="api-pair" markdown="1">

```csharp
public static class FuelStations
{
    public static event Action<FuelBoughtArgs> FuelBought;
}
```

| Event | Gives you | When it fires |
|---|---|---|
| `FuelBought` | `FuelBoughtArgs` | The player has paid for fuel. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FuelBoughtArgs</code> <span>what FuelBought tells you</span></p>

`FuelBought` gives you this. The framework builds it from a hook on the game's `GasStation.PayFuelOc` payment, so it is the framework's own type.

<div class="api-pair" markdown="1">

```csharp
public sealed class FuelBoughtArgs
{
    public string StationId { get; }
    public float Amount { get; }
    public int Cost { get; }
    public MoneyCurrency Currency { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `StationId` | `string` | The station, the same as `FuelStationInfo.Id`. |
| `Amount` | `float` | How many units of fuel were paid for, over all pumps. |
| `Cost` | `int` | The money paid. |
| `Currency` | `MoneyCurrency` | The money the cost is in. OC for now. |

</div>
</div>

## Things to know

- `FuelBought` fires when a payment goes through. If the player cannot afford the bill, nothing is paid and the event does not fire. <span class="tech">(It fires after `GasStation.PayFuelOc` when the game's `priceToPayOc` went from above zero back to zero.)</span>
- `Currency` is always OC for now. The game only pays this kind of bill in OC (Open Sewer Coins, see the [glossary](../../glossary.md#oc)).
- `Amount` is the fuel the payment covers. The framework reads it before the game empties the pumps. <span class="tech">(It is `FuelPump.filledAmount`, added up over the station's pumps.)</span>
- The money side of the same payment is `Cash.MoneyChanged`. See [Economy](../economy.md). `FuelBought` tells you what the money was spent on.
- The event fires for every way the game starts a payment. The game calls the payment method from scene or dialogue wiring, not from its code, and the framework changes the method itself so every caller is covered. <span class="tech">(`PayFuelOc` has no C# caller in the game code.)</span>

## Limits

- `Cost` and `Amount` can disagree when the player filled more than one pump. `Cost` is the bill the game holds, and the game overwrites that bill each time it changes instead of adding to it. So `Cost` is the last pump's bill, while `Amount` adds up every pump. This is how the game counts, and the framework does not fix it. <span class="tech">(`Cost` is `priceToPayOc`, and `GasStation.UpdatePriceToPayOc` overwrites it.)</span>
- The framework does not hold the event back while a save loads. A handler that should ignore events raised then can check `SaveData.Busy`, which is `true` while a save is loading or being written.
