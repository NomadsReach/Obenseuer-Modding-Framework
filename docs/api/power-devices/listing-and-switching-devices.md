---
title: Listing and switching devices
parent: Power devices
grand_parent: Objects and devices
nav_order: 2
---

# Listing and switching devices

This is the full list of what the API gives you, one section for each part. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PowerDeviceKind</code> <span>whether a device gives or uses power</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum PowerDeviceKind
{
    Source,
    Consumer
}
```

| Value | Meaning |
|---|---|
| `Source` | The device gives power to others: a generator, a battery, or furniture that produces power. |
| `Consumer` | The device uses power, including a stove that burns its own fuel. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PowerDevices</code> <span>list, read and switch power devices</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class PowerDevices
{
    public static List<PowerDeviceInfo> All();
    public static PowerDeviceInfo Get(string id);
    public static bool SetOn(string id, bool on);

    public static event Action<PoweredChangedArgs> PoweredChanged;
    public static event Action<FuelRanOutArgs> FuelRanOut;

    public static bool IsAvailable { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the power events could not be set up. |

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<PowerDeviceInfo>` | Every power device in the scene you are in. |
| `Get(string id)` | `PowerDeviceInfo` | The device with this id. `null` when none has it. |
| `SetOn(string id, bool on)` | `bool` | Switches a device on or off. Returns `true` when it is now in that state. |

| Event | Gives you | When it fires |
|---|---|---|
| `PoweredChanged` | `PoweredChangedArgs` | A furniture device or a battery has gained or lost power. `IsPowered` in the event is the new state. |
| `FuelRanOut` | `FuelRanOutArgs` | A fuel burner has run out of fuel. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PowerDeviceInfo</code> <span>a snapshot of one device</span></p>
<div class="api-pair" markdown="1">

```csharp
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
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The device's id: its save id, or its place in the scene if it has none. |
| `Kind` | `PowerDeviceKind` | Whether the device gives power (`Source`) or uses it (`Consumer`). |
| `IsOn` | `bool` | `true` when the device is switched on. |
| `IsPowered` | `bool` | `true` when the device is really getting power and running. |
| `NetPower` | `float` | Furniture only: positive when it makes power, negative when it uses power. `0` for everything else. |
| `FuelLevel` | `float` | Seconds of burn time left. `0` for devices that do not burn fuel. |
| `MaxFuel` | `float` | The most seconds of burn time it can hold. `0` for devices that do not burn fuel. |
| `StoredCharge` | `float` | Batteries only: the charge stored now, in the game's own units. `0` for everything else. |
| `MaxCharge` | `float` | Batteries only: the most charge it can store. `0` for everything else. |
| `FuelItemIds` | `List<int>` | The item numbers of the fuel it accepts. Empty when it burns no fuel. |
| `IsBurning` | `bool` | Fuel burners only: `true` while fuel is burning. |
| `BurnTimeFactor` | `float` | Fuel burners only: how fast it burns fuel. `0` for everything else. |

</div>
</div>

## Things to know

- `All()` and `Get(id)` read the device as it is right now, and they are slow, so do not call them every frame. They search the scene each time and look up the fuel items. Call them when something happens, or on a timer. A `PowerDeviceInfo` is a snapshot, so ask again for fresh values. <span class="tech">(The id is the object's GUID (`FurnitureStats.GUID`, `CraftingBase.GUID` or `BatteryPowerSource.GUID`, see the [glossary](../../glossary.md#guid)), or the scene path when it has none. `FuelItemIds` walks the item database for each fuel category.)</span>
- `Kind` is `Source` when the device feeds power to others. That is a generator, a battery, or a piece of furniture that makes power. Everything else is a `Consumer`, even a stove that burns its own fuel. <span class="tech">(`Source` means a `Generator`, a `BatteryPowerSource`, or furniture with `NetPower` above 0.)</span>
- `IsOn` is the device's switch. `IsPowered` is whether it is really running. A device can be switched on and still have no power, for example a generator with no fuel, or a lamp whose power line cannot feed it. <span class="tech">(For furniture, `IsOn` is `FurnitureStats.isOn` and `IsPowered` is `FurnitureStats.isActive`, which stays `false` when the switch is on but the grid cannot feed the device. For a crafting station, they are `CraftingBase.On` and `CraftingBase.PoweredAndActive`. For a battery, `IsOn` is whether drain is enabled (`BatteryPowerSource.drainEnabled`) and `IsPowered` is `BatteryPowerSource.powering`; both are read by reflection.)</span>
- `NetPower` only means something for furniture. Crafting stations and batteries always give `0`, because the game does not use a power draw for them. <span class="tech">(It is `FurnitureStats.netPowerOn`. `PowerSource.powerConsumption` is marked "Not used anywhere yet" in the game.)</span>
- `FuelLevel` and `MaxFuel` are in seconds of burn time. For a crafting station that needs fuel, they are what is left and the most it holds. For a battery, they are how long it would run at its current drain, whether or not drain is on. For a battery that is charging they are `0`, because no burn time applies. For everything else they are `0`. <span class="tech">(For stations they are `Fuel.BurnTimeRemaining` and `Fuel.MaxBurnTime`. For batteries, they are the stored charge and the capacity divided by `BatteryPowerSource.fuelDrainPerHour`, times 3600, and `0` when the drain is zero or negative.)</span>
- `StoredCharge` and `MaxCharge` only apply to batteries. They add up all the battery's storage slots, in the game's own units. They are `0` for everything else. <span class="tech">(The units are `ItemAmmodata.ammo` and `maxAmmo`, or `ItemLiquidData.GetLiquidAmount` and `GetLiquidMaxAmount`. The battery's `fuelType` flags choose which meta types count, the same as `BatteryPowerSource.CheckItems`.)</span>
- `FuelItemIds` lists the items a fuel burner accepts. It is empty for devices that burn no fuel. It is also empty for batteries, because a battery accepts fuel by what kind of item it is, not by a list of items. <span class="tech">(The ids are the item ids in the device's fuel categories (`Fuel.FuelCategories`), the same filter the game's fuel slot uses.)</span>
- `IsBurning` and `BurnTimeFactor` only apply to crafting stations that burn fuel. `IsBurning` is `true` while fuel is burning. `BurnTimeFactor` is how fast the machine burns fuel, which depends on its quality and upgrades. Both give `false` or `0` for everything else. <span class="tech">(They are `Fuel.active` and `Fuel.BurnTimeFactor`.)</span>
- `SetOn(id, on)` switches the device the way the game does. It returns `true` when the device is in the state you asked for afterwards, so asking for the state it is already in also gives `true`. It returns `false` for an unknown or empty id, for a device the game keeps always on, and while a save is loading or saving. <span class="tech">(For furniture it calls `FurnitureStats.turnON` or `turnOFF`. For a crafting station it calls `CraftingBase.Toggle(on)`, and returns `false` for an `AlwaysOn` station, which the game ignores. For a battery it calls `BatteryPowerSource.ToggleDrain(on)`.)</span>

## Limits

- Switching on a station that has no fuel works, but it does not run. `IsOn` becomes `true` while `IsPowered` stays `false`, because the game does not check the fuel when you switch it on. <span class="tech">(A crafting station's `Toggle` does not check fuel.)</span>
