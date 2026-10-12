# Power devices API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.powerdevices`.
Files: `src/Api/PowerDevices.cs`, `src/Api/PowerDevicesPatches.cs`.

This area wraps the three power objects the game keeps per device:

- furniture power devices (`FurnitureStats`: `isOn`, `netPowerOn`), on the furniture grid;
- crafting stations with a power source (`CraftingBase._Power`, a `PowerSource`, including generators and fuel-burning stoves);
- batteries (`BatteryPowerSource`, which burns stored fuel items over time).

`WorldPower` (`docs/api/world.md`) still reports blackouts and generator switches. It is not repeated here. `FuelStations` (`docs/api/fuel.md`) covers the gas pumps, not these devices.

## Wiring

```csharp
internal static void PowerDevices.Init();
```

Core calls this from `Core.Awake`.

`Init` installs three hooks on `FurnitureStats` (`Awake`, `OutputON`, `OutputOFF`). If any is missing, the owner is disabled and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.powerdevices.log`. The reads and `SetOn` keep working; `PoweredChanged` stops.

## Threading and safety

- Main thread only.
- `PowerDevices.IsAvailable` is false when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, `All` returns an empty list, `Get` returns `null`, and `SetOn` returns `false`.
- Nothing throws. A null or empty `id` finds nothing.
- `SetOn` returns `false` while a save loads (`SaveController.Loading`).
- Queries scan the scene on every call. `All` and `Get` also look up fuel items (`FuelItemIds`), which walks the item database for each fuel category. Call them on demand, not per frame.
- Events are raised synchronously after the game has changed the state. A handler that throws is logged and the other handlers still run (`Events.Raise`).

## API

```csharp
public enum PowerDeviceKind { Source, Consumer }

public static class PowerDevices
{
    public static List<PowerDeviceInfo> All();
    public static PowerDeviceInfo Get(string id);
    public static bool SetOn(string id, bool on);

    public static event Action<PoweredChangedArgs> PoweredChanged;

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
    public List<int> FuelItemIds { get; }
}

public sealed class PoweredChangedArgs
{
    public string Id { get; }
    public bool IsPowered { get; }
}
```

## Fields

- `Id`: the object's GUID (`FurnitureStats.GUID`, `CraftingBase.GUID`, `BatteryPowerSource.GUID`), or the scene path when the GUID is empty (`SceneIds.Of`).
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
- `FuelLevel` and `MaxFuel`: in game seconds of burn time, for crafting stations that require fuel (`Fuel.BurnTimeRemaining` and `Fuel.MaxBurnTime`). Both are `0` for everything else, including batteries (see below).
- `FuelItemIds`: item ids in the device's fuel categories (`Fuel.FuelCategories`, the same filter the game's fuel slot uses). Empty for devices that do not burn fuel, and for batteries, which accept fuel by item meta type, not by category.

## Writes

`SetOn` calls the game's own method for each kind:

- furniture: `FurnitureStats.turnON` or `turnOFF`;
- crafting station: `CraftingBase.Toggle(on)`. It returns `false` for an `AlwaysOn` station, which the game ignores;
- battery: `BatteryPowerSource.ToggleDrain(on)`.

It returns true when the device is in the requested state afterwards, so a call for the current state also returns `true`. A crafting station's `Toggle` does not check fuel: a generator with no fuel can be switched on, and `IsOn` becomes true while `IsPowered` stays false.

## Events

- `PoweredChanged` fires when a furniture device's `IsPowered` flips, through the game's `FurnitureStats.OutputON` or `OutputOFF`. That covers the switch, grid changes from other devices (for example a generator running out, or a device being removed), and the device's own changes.
- It does not fire while the game restores furniture on load. `Awake` runs during a load, and a transition raised inside it is suppressed by a counter that the `Awake` prefix and finalizer keep balanced. A transition is also suppressed while `SaveController.Loading` is set.
- Crafting power sources do not raise `PoweredChanged`. Use `WorldPower.GeneratorToggled` for a generator's switch. A generator running out of fuel has no event.
- `PoweredChangedArgs.IsPowered` is the new state. Read `Get(id)` for the rest.

## Not exposed

- Battery stored fuel: a battery's charge is item meta in its storage (`ItemAmmodata.ammo` or liquid). `FuelLevel` and `MaxFuel` stay `0`.
- Crafting station power changes that go through `OnPowerOn` or `OnPowerOff` (fuel run-out, load) do not raise `PoweredChanged`.
- Furniture grid totals (`FurnitureManager.NetPower`) and the per-manager device list.
- `PowerSource.powerConsumption`, which the game does not use.
- `BatteryPowerSource` fuel items (`fuelType` is a flag set, not item ids).

## Smoke test

1. Load a save with a furniture lamp on a powered circuit. `PowerDevices.All()` should list it with `Kind` `Consumer`, `IsOn` and `IsPowered` true, and `NetPower` below zero.
2. Subscribe with `PowerDevices.PoweredChanged += a => Log("power " + a.Id + " " + a.IsPowered);`. Switch the lamp off and on. Each switch should log once with the new `IsPowered`.
3. Call `PowerDevices.SetOn(id, false)` on the lamp. It should return `true`, `Get(id).IsOn` should be false and the lamp should go dark.
4. Find a generator (`Kind` `Source`, `FuelItemIds` non-empty). `SetOn(id, false)` should turn it off and fire `WorldPower.GeneratorToggled` once.
5. Load the same save again. `PoweredChanged` should not fire for the restored lamps during load.
