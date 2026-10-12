# Fuel API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.fuel`.
Files: `src/Api/FuelStations.cs`, `src/Api/FuelStationsPatches.cs`.

The minitrain's fuel tank is not repeated here. `Trains.Minitrain` already reports `Fuel` and `Capacity` (`docs/api/trains.md`).

## Wiring

```csharp
internal static void FuelStations.Init();
```

Core calls this from `Core.Awake`.

`Init` installs the `GasStation.PayFuelOc` hook under one owner. If the hook is missing, it is removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.fuel")` becomes true, and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.fuel.log`. `All` keeps working; only `FuelBought` stops.

## Threading and safety

- Main thread only.
- `FuelStations.IsAvailable` is false when the hook was not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, `All` returns an empty list.
- There are no writes. Nothing throws to the caller.
- `FuelBought` is raised synchronously from the game's `PayFuelOc`. A handler that throws is logged and the other handlers still run (`Events.Raise`).

## Stations

```csharp
public static List<FuelStationInfo> All();

public sealed class FuelStationInfo
{
    public string Id { get; }              // SceneIds.Of(station): the station's GUID, or its scene path when it has none
    public int Price { get; }              // price per fuel unit, as the game stores it (GasStation.currentFuelPrice)
    public MoneyCurrency Currency { get; } // the currency the price is in (GasStation.currency)
}
```

- `All` lists the active `GasStation` objects in the loaded scene. Inactive stations are not listed (Unity's default for `FindObjectsByType`).
- `Price` is per fuel unit, not per litre. The pump bills `ceil(price * amount)` (`FuelPump.cs:380`, `GetPrice` at `FuelPump.cs:404`).
- A station with `Currency` RM accumulates its bill in RM. The game charges that path as a crime on leaving the fuel area (`GasStation.cs:237`, `MinitrainFuelArea.cs:23`), so it is not a purchase and raises no event.

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

- `FuelBought` fires after `GasStation.PayFuelOc` completes a payment: the game's own `priceToPayOc` went from above zero back to zero (`GasStation.cs:203`). A payment the player cannot afford leaves the bill in place and fires nothing.
- The money side is `Cash.MoneyChanged` (`docs/API.md`). `FuelBought` is the fuel side of the same payment.
- `Amount` is read before the game resets the pumps (`GasStation.cs:195`), so it is the fuel the payment covers.

## Ceilings and risks

- `Cost` is the bill the game holds, `priceToPayOc`. `GasStation.UpdatePriceToPayOc` overwrites that bill rather than adding to it (`GasStation.cs:212`), so `Cost` is the last pump's bill. `Amount` sums every pump. A visit that filled two pumps can therefore report a `Cost` and an `Amount` that do not match. This is the game's own accounting, not fixed here.
- `PayFuelOc` has no C# caller in the decompile. The game calls it from scene or dialogue wiring, which is not in the decompile. The hook is on the method, so every caller is covered.
- No price setter. The price is `GasStation.currentFuelPrice`, which is `protected` (`GasStation.cs:53`), and the only game write is `SetRandomPrice` on a random timer (`GasStation.cs:164`). A write would last until the next roll. Not exposed.
- Not tested in game.

## Smoke test

1. Load a save near a fuel station. `FuelStations.All()` should list it with a `Price` that matches the price shown on the pump, and `Currency` OC.
2. Subscribe with `FuelStations.FuelBought += a => Log("fuel " + a.StationId + " " + a.Amount + " " + a.Cost);`. Fill a tank, then pay at the station.
3. `FuelBought` should fire once. `Cost` should equal the amount charged, and `Cash.MoneyChanged` should fire for the same payment.
4. Pay with less money than the bill. Nothing should fire.
