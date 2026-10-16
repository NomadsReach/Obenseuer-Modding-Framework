---
title: Power devices API
parent: API reference
nav_order: 56
---

# Power devices API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.powerdevices`.
Files: `src/Api/PowerDevices.cs`, `src/Api/PowerDevicesPatches.cs`.

This area wraps the three power objects the game keeps per device:

- furniture power devices (`FurnitureStats`: `isOn`, `netPowerOn`), on the furniture grid;
- crafting stations with a power source (`CraftingBase._Power`, a `PowerSource`, including generators and fuel-burning stoves);
- batteries (`BatteryPowerSource`, which burns stored fuel items over time).

`WorldPower` (`docs/api/world.md`) still reports blackouts and generator switches. It is not repeated here. `FuelStations` (`docs/api/fuel.md`) covers the gas pumps, not these devices.

## If a hook cannot be installed

The framework installs three hooks on `FurnitureStats` (`Awake`, `OutputON`, `OutputOFF`). If any is missing, the owner is disabled and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.powerdevices.log`. The reads and `SetOn` keep working; `PoweredChanged` stops.

Once those three are in, the framework also installs two more hooks, each under its own owner: `Fuel.OnFuelConsumeStopped` (`obenseuer.moddingframework.powerdevices.fuel`, for `FuelRanOut`) and `BatteryPowerSource.PowerON` and `PowerOFF` (`obenseuer.moddingframework.powerdevices.battery`, for the battery part of `PoweredChanged`). A missing method there disables only its own event. `IsAvailable` does not change. If the furniture hooks fail, neither of these is installed.

## Threading and safety

- Main thread only.
- `PowerDevices.IsAvailable` is false when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, `All` returns an empty list, `Get` returns `null`, and `SetOn` returns `false`.
- Nothing throws. A null or empty `id` finds nothing.
- `SetOn` returns `false` while a save is loading or being written.
- Queries scan the active scene on every call. `All` and `Get` also look up fuel items (`FuelItemIds`), which walks the item database for each fuel category. Call them on demand, not per frame.
- Events are raised synchronously after the game has changed the state. A handler that throws is logged and the other handlers still run.

## API

```csharp
public enum PowerDeviceKind { Source, Consumer }

public static class PowerDevices
{
    public static List<PowerDeviceInfo> All();
    public static PowerDeviceInfo Get(string id);
    public static bool SetOn(string id, bool on);

    public static event Action<PoweredChangedArgs> PoweredChanged;
    public static event Action<FuelRanOutArgs> FuelRanOut;

    public static bool IsAvailable { get; }
}

public sealed class PowerDeviceInfo
{
    public string Id { get; }
    public PowerDeviceKind Kind { get; }
    public bool IsOn { get; }
    public bool IsPowered { get; }
    public float NetPower { get; }
    public float FuelLevel { get; }
    public float MaxFuel { get; }
    public float StoredCharge { get; }
    public float MaxCharge { get; }
    public List<int> FuelItemIds { get; }
    public bool IsBurning { get; }
    public float BurnTimeFactor { get; }
}

public sealed class PoweredChangedArgs
{
    public string Id { get; }
    public bool IsPowered { get; }
}

public sealed class FuelRanOutArgs
{
    public string Id { get; }
}
```

## Fields

- `Id`: the object's GUID (`FurnitureStats.GUID`, `CraftingBase.GUID`, `BatteryPowerSource.GUID`), or the scene path when the GUID is empty.
- `Kind`: `Source` when the device feeds power to others: a `Generator`, a `BatteryPowerSource`, or a furniture device with `NetPower > 0`. Everything else is `Consumer`, including fuel-burning stoves that have a power source of their own.
- `IsOn`: the device's switch.
  - Furniture: `FurnitureStats.isOn`.
  - Crafting station: `CraftingBase.On`.
  - Battery: whether drain is enabled (`BatteryPowerSource.drainEnabled`, read by reflection).
- `IsPowered`: the device is supplied and running.
  - Furniture: `FurnitureStats.isActive`. It stays false when the switch is on but the grid cannot feed the device.
  - Crafting station: `CraftingBase.PoweredAndActive`.
  - Battery: `BatteryPowerSource.powering` (read by reflection).
- `NetPower`: furniture only, the game's `FurnitureStats.netPowerOn`. Positive means the device produces power, negative means it draws. It is `0` for crafting stations and batteries, because the game does not use a per-device draw for them (`PowerSource.powerConsumption` is marked "Not used anywhere yet").
- `FuelLevel` and `MaxFuel`: in game seconds of burn time.
  - Crafting stations that require fuel: `Fuel.BurnTimeRemaining` and `Fuel.MaxBurnTime`.
  - Batteries: the stored charge and the capacity, divided by `BatteryPowerSource.fuelDrainPerHour` and multiplied by 3600. This is the time the battery would run at its current drain rate, whether or not drain is on. It is `0` when the drain is zero or negative (a charging battery), because no burn time applies.
  - Everything else is `0`.
- `StoredCharge` and `MaxCharge`: batteries only, in the game's own units (`ItemAmmodata.ammo` and `maxAmmo`, or `ItemLiquidData.GetLiquidAmount` and `GetLiquidMaxAmount`), summed over the battery's storage stacks. The battery's `fuelType` flags choose which meta types count, the same as `BatteryPowerSource.CheckItems`. Both are `0` for everything else.
- `FuelItemIds`: item ids in the device's fuel categories (`Fuel.FuelCategories`, the same filter the game's fuel slot uses). Empty for devices that do not burn fuel, and for batteries, which accept fuel by item meta type, not by category.
- `IsBurning`: crafting stations with a fuel power source only, the game's `Fuel.active`. True while the station is burning fuel. `false` for everything else.
- `BurnTimeFactor`: crafting stations with a fuel power source only, the game's `Fuel.BurnTimeFactor` (the machine's burn speed, set from its quality and upgrades). `0` for everything else.

## Writes

`SetOn` calls the game's own method for each kind:

- furniture: `FurnitureStats.turnON` or `turnOFF`;
- crafting station: `CraftingBase.Toggle(on)`. It returns `false` for an `AlwaysOn` station, which the game ignores;
- battery: `BatteryPowerSource.ToggleDrain(on)`.

It returns true when the device is in the requested state afterwards, so a call for the current state also returns `true`. A crafting station's `Toggle` does not check fuel: a generator with no fuel can be switched on, and `IsOn` becomes true while `IsPowered` stays false.

## Events

- `PoweredChanged` fires when a furniture device's `IsPowered` flips, through the game's `FurnitureStats.OutputON` or `OutputOFF`. That covers the switch, grid changes from other devices (for example a generator running out, or a device being removed), and the device's own changes.
- It does not fire while the game restores furniture on load. A transition is also suppressed while a save is loading.
- Batteries raise `PoweredChanged` when `BatteryPowerSource.powering` flips, through its private `PowerON` and `PowerOFF`. The event fires after the game's relays for that transition have run. It does not fire while a save loads. Two battery transitions happen outside a player action: `LateStart` runs `CheckItems` two frames after the battery starts, and the post-load offline drain (`LoadDelay`) can switch it off after loading has finished. Both raise the event, since the game changes the state there too. A handler that should ignore them can check `Get(id)` against its own record.
- `FuelRanOut` fires once when a crafting station's fuel runs out. It is raised from the game's `Fuel.OnFuelConsumeStopped` when the fuel counter was on and `BurnTimeRemaining` is 0. It covers every crafting station with a fuel power source, generators and stoves alike. It does not fire while a save loads or saves. `FuelRanOutArgs.Id` is the station's id. Known edge: a manual switch-off (`SetOn(id, false)`) in the one game tick after `BurnTimeRemaining` reaches 0 also reports a run-out, because the game runs its own run-out logic on that tick too.
- Crafting station power changes raise no `PoweredChanged`. Use `WorldPower.GeneratorToggled` for a generator's switch and `FuelRanOut` for a generator running out of fuel.
- `PoweredChangedArgs.IsPowered` is the new state. Read `Get(id)` for the rest.

## Not exposed

- Crafting station power changes that go through `OnPowerOn` or `OnPowerOff` (load) do not raise `PoweredChanged`. A fuel run-out raises `FuelRanOut` instead.
- Furniture grid totals (`FurnitureManager.NetPower`) and the per-manager device list.
- `PowerSource.powerConsumption`, which the game does not use.
- `BatteryPowerSource` fuel items (`fuelType` is a flag set, not item ids).
