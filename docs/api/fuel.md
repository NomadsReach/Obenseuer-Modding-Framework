---
title: Fuel API
parent: API reference
nav_order: 23
---

# Fuel API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.fuel`.
Files: `src/Api/FuelStations.cs`, `src/Api/FuelStationsPatches.cs`, `src/Api/FuelTypes.cs`.

The minitrain's fuel tank is not repeated here. `Trains.Minitrain` already reports `Fuel` and `Capacity` (`docs/api/trains.md`).

## If a hook cannot be installed

The framework installs the `GasStation.PayFuelOc` hook under one owner. If the hook is missing, it is removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.fuel")` becomes true, and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.fuel.log`. `All` keeps working; only `FuelBought` stops.

## Threading and safety

- Main thread only.
- `FuelStations.IsAvailable` is false when the hook was not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, `All` returns an empty list.
- There are no writes. Nothing throws to the caller.
- `FuelBought` is raised synchronously from the game's `PayFuelOc`. A handler that throws is logged and the other handlers still run.

## Stations

```csharp
public static List<FuelStationInfo> All();

public sealed class FuelStationInfo
{
    public string Id { get; }              // the station's GUID, or its scene path when it has none
    public int Price { get; }              // price per fuel unit, as the game stores it (GasStation.currentFuelPrice)
    public MoneyCurrency Currency { get; } // the currency the price is in (GasStation.currency)
}
```

- `All` lists the active `GasStation` objects in the active scene. Inactive stations are not listed (Unity's default for `FindObjectsByType`).
- `Price` is per fuel unit, not per litre. The pump bills `ceil(price * amount)` (`FuelPump`, `GetPrice` at `FuelPump`).
- A station with `Currency` RM accumulates its bill in RM. The game charges that path as a crime on leaving the fuel area, so it is not a purchase and raises no event.

## Fuel items

```csharp
public static class FuelTypes
{
    public static List<FuelTypeInfo> All();
    public static FuelTypeInfo Get(int itemId);     // null when the item has no fuel data
}

public sealed class FuelTypeInfo
{
    public int ItemId { get; }
    public int BurnSeconds { get; }                 // ItemFueldata.burntime, before the machine's burn time factor
    public int BurntItemId { get; }                 // ItemFueldata.burntItem, -1 when none
    public float BurntItemAmount { get; }           // refuse items per unit burned
}
```

- `All` lists every item in the game's item database that has `ItemFueldata` metadata. A generator burns such an item for `BurnSeconds` per unit and spawns `BurntItemAmount` of `BurntItemId` per unit.
- Not listed: an item with no fuel data that a machine accepts through its `FuelCategories` burns 60 seconds per unit. The accepted items per device are in `PowerDevices` (`FuelItemIds`).
- Liquid fuel is not reported. A liquid container burns `burntime` times the liquid amount, or 60 per unit when the liquid has no fuel data.
- Not exposed: battery fuel (which uses ammo and liquid metadata, not `ItemFueldata`) and minitrain fuel (a private `acceptedFuels` list). The per-machine burn time factor is `PowerDeviceInfo.BurnTimeFactor` (`docs/api/power-devices.md`), and a machine's fuel run-out is `PowerDevices.FuelRanOut`.

## Events

```csharp
public static event Action<FuelBoughtArgs> FuelBought;

public sealed class FuelBoughtArgs
{
    public string StationId { get; }       // the station, as FuelStationInfo.Id
    public float Amount { get; }           // game fuel units paid for (FuelPump.filledAmount, summed over the station's pumps)
    public int Cost { get; }               // the amount paid, in Currency
    public MoneyCurrency Currency { get; } // always OC for now (GasStation.PayFuelOc pays OC only)
}
```

- `FuelBought` fires after `GasStation.PayFuelOc` completes a payment: the game's own `priceToPayOc` went from above zero back to zero. A payment the player cannot afford leaves the bill in place and fires nothing.
- The money side is `Cash.MoneyChanged` ([economy.md](economy.md)). `FuelBought` is the fuel side of the same payment.
- `Amount` is read before the game resets the pumps, so it is the fuel the payment covers.

## Ceilings and risks

- `Cost` is the bill the game holds, `priceToPayOc`. `GasStation.UpdatePriceToPayOc` overwrites that bill rather than adding to it, so `Cost` is the last pump's bill. `Amount` sums every pump. A visit that filled two pumps can therefore report a `Cost` and an `Amount` that do not match. This is the game's own accounting, not fixed here.
- `PayFuelOc` has no C# caller in the game code. The game calls it from scene or dialogue wiring, which is not in the game code. The hook is on the method, so every caller is covered.
- No price setter. The price is `GasStation.currentFuelPrice`, which is `protected`, and the only game write is `SetRandomPrice` on a random timer. A write would last until the next roll. Not exposed.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
