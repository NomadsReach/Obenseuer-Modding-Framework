---
title: Buying fuel
parent: Fuel
grand_parent: Money and shops
nav_order: 3
---

# Buying fuel

`FuelBought` tells your mod when the player has paid for fuel at a station.

```csharp
public sealed class FuelBoughtArgs    // what the event tells you
{
    public string StationId { get; }         // the station, the same as FuelStationInfo.Id
    public float Amount { get; }             // how many units of fuel were paid for, over all pumps
    public int Cost { get; }                 // the money paid
    public MoneyCurrency Currency { get; }   // the money the cost is in; OC for now
}

public static class FuelStations
{
    public static event Action<FuelBoughtArgs> FuelBought;   // the player has paid for fuel
}
```

- **`FuelBought` fires when a payment goes through.** If the player cannot afford the bill, nothing is paid and the event does not fire.
  - Technical: it fires after `GasStation.PayFuelOc` when the game's `priceToPayOc` went from above zero back to zero.
- **`Currency` is always OC for now.** The game only pays this kind of bill in OC (Open Sewer Coins, see the [glossary](../../glossary.md#oc)).
- **`Amount` is the fuel the payment covers.** The framework reads it before the game empties the pumps.
  - Technical: it is `FuelPump.filledAmount`, added up over the station's pumps.
- **`Cost` and `Amount` can disagree when the player filled more than one pump.** `Cost` is the bill the game holds, and the game overwrites that bill each time it changes instead of adding to it. So `Cost` is the last pump's bill, while `Amount` adds up every pump. This is how the game counts, and the framework does not fix it.
  - Technical: `Cost` is `priceToPayOc`, and `GasStation.UpdatePriceToPayOc` overwrites it.
- **The money side of the same payment is `Cash.MoneyChanged`.** See [Economy](../economy.md). `FuelBought` tells you what the money was spent on.
- **The event fires for every way the game starts a payment.** The game calls the payment method from scene or dialogue wiring, not from its code, and the framework changes the method itself so every caller is covered.
  - Technical: `PayFuelOc` has no C# caller in the game code.
- **The framework does not hold the event back while a save loads.** A handler that should ignore events raised then can check `SaveData.Busy`, which is `true` while a save is loading or being written.
