---
title: Listing and switching devices
parent: Power devices
grand_parent: Objects and devices
nav_order: 2
---

# Listing and switching devices

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public enum PowerDeviceKind
{
    Source,     // the device gives power to others: a generator, a battery, or furniture that produces power
    Consumer    // the device uses power, including a stove that burns its own fuel
}

public static class PowerDevices
{
    public static List<PowerDeviceInfo> All();            // every power device in the scene you are in
    public static PowerDeviceInfo Get(string id);         // the device with this id; null when none has it
    public static bool SetOn(string id, bool on);         // switches a device on or off; true when it is now in that state

    public static event Action<PoweredChangedArgs> PoweredChanged;   // a furniture device or a battery has gained or lost power
    public static event Action<FuelRanOutArgs> FuelRanOut;           // a fuel burner has run out of fuel

    public static bool IsAvailable { get; }               // false when the power events could not be set up
}

public sealed class PowerDeviceInfo    // a snapshot of one device
{
    public string Id { get; }                // the device's id: its save id, or its place in the scene if it has none
    public PowerDeviceKind Kind { get; }     // whether the device gives power (Source) or uses it (Consumer)
    public bool IsOn { get; }                // true when the device is switched on
    public bool IsPowered { get; }           // true when the device is really getting power and running
    public float NetPower { get; }           // furniture only: positive when it makes power, negative when it uses power; 0 for everything else
    public float FuelLevel { get; }          // seconds of burn time left; 0 for devices that do not burn fuel
    public float MaxFuel { get; }            // the most seconds of burn time it can hold; 0 for devices that do not burn fuel
    public float StoredCharge { get; }       // batteries only: the charge stored now, in the game's own units; 0 for everything else
    public float MaxCharge { get; }          // batteries only: the most charge it can store; 0 for everything else
    public List<int> FuelItemIds { get; }    // the item numbers of the fuel it accepts; empty when it burns no fuel
    public bool IsBurning { get; }           // fuel burners only: true while fuel is burning
    public float BurnTimeFactor { get; }     // fuel burners only: how fast it burns fuel; 0 for everything else
}
```

- **`All()` and `Get(id)` read the device as it is right now, and they are slow, so do not call them every frame.** They search the scene each time and look up the fuel items. Call them when something happens, or on a timer. A `PowerDeviceInfo` is a snapshot, so ask again for fresh values.
  - Technical: the id is the object's GUID (`FurnitureStats.GUID`, `CraftingBase.GUID` or `BatteryPowerSource.GUID`, see the [glossary](../../glossary.md#guid)), or the scene path when it has none. `FuelItemIds` walks the item database for each fuel category.
- **`Kind` is `Source` when the device feeds power to others.** That is a generator, a battery, or a piece of furniture that makes power. Everything else is a `Consumer`, even a stove that burns its own fuel.
  - Technical: `Source` means a `Generator`, a `BatteryPowerSource`, or furniture with `NetPower` above 0.
- **`IsOn` is the device's switch. `IsPowered` is whether it is really running.** A device can be switched on and still have no power, for example a generator with no fuel, or a lamp whose power line cannot feed it.
  - Technical: for furniture, `IsOn` is `FurnitureStats.isOn` and `IsPowered` is `FurnitureStats.isActive`, which stays `false` when the switch is on but the grid cannot feed the device. For a crafting station, they are `CraftingBase.On` and `CraftingBase.PoweredAndActive`. For a battery, `IsOn` is whether drain is enabled (`BatteryPowerSource.drainEnabled`) and `IsPowered` is `BatteryPowerSource.powering`; both are read by reflection.
- **`NetPower` only means something for furniture.** Crafting stations and batteries always give `0`, because the game does not use a power draw for them.
  - Technical: it is `FurnitureStats.netPowerOn`. `PowerSource.powerConsumption` is marked "Not used anywhere yet" in the game.
- **`FuelLevel` and `MaxFuel` are in seconds of burn time.** For a crafting station that needs fuel, they are what is left and the most it holds. For a battery, they are how long it would run at its current drain, whether or not drain is on. For a battery that is charging they are `0`, because no burn time applies. For everything else they are `0`.
  - Technical: for stations they are `Fuel.BurnTimeRemaining` and `Fuel.MaxBurnTime`. For batteries, they are the stored charge and the capacity divided by `BatteryPowerSource.fuelDrainPerHour`, times 3600, and `0` when the drain is zero or negative.
- **`StoredCharge` and `MaxCharge` only apply to batteries.** They add up all the battery's storage slots, in the game's own units. They are `0` for everything else.
  - Technical: the units are `ItemAmmodata.ammo` and `maxAmmo`, or `ItemLiquidData.GetLiquidAmount` and `GetLiquidMaxAmount`. The battery's `fuelType` flags choose which meta types count, the same as `BatteryPowerSource.CheckItems`.
- **`FuelItemIds` lists the items a fuel burner accepts.** It is empty for devices that burn no fuel. It is also empty for batteries, because a battery accepts fuel by what kind of item it is, not by a list of items.
  - Technical: the ids are the item ids in the device's fuel categories (`Fuel.FuelCategories`), the same filter the game's fuel slot uses.
- **`IsBurning` and `BurnTimeFactor` only apply to crafting stations that burn fuel.** `IsBurning` is `true` while fuel is burning. `BurnTimeFactor` is how fast the machine burns fuel, which depends on its quality and upgrades. Both give `false` or `0` for everything else.
  - Technical: they are `Fuel.active` and `Fuel.BurnTimeFactor`.
- **`SetOn(id, on)` switches the device the way the game does.** It returns `true` when the device is in the state you asked for afterwards, so asking for the state it is already in also gives `true`. It returns `false` for an unknown or empty id, for a device the game keeps always on, and while a save is loading or saving.
  - Technical: for furniture it calls `FurnitureStats.turnON` or `turnOFF`. For a crafting station it calls `CraftingBase.Toggle(on)`, and returns `false` for an `AlwaysOn` station, which the game ignores. For a battery it calls `BatteryPowerSource.ToggleDrain(on)`.
- **Switching on a station that has no fuel works, but it does not run.** `IsOn` becomes `true` while `IsPowered` stays `false`, because the game does not check the fuel when you switch it on.
  - Technical: a crafting station's `Toggle` does not check fuel.
