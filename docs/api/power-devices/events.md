---
title: Events
parent: Power devices
grand_parent: Objects and devices
nav_order: 3
---

# Events

- **`PoweredChanged` fires when a furniture device or a battery gains or loses power.** `IsPowered` in the event is the new state. Read `Get(id)` for the rest. For furniture it also fires when the cause is somewhere else, for example a generator running out or a device being removed.
  - Technical: for furniture it is raised from the game's `FurnitureStats.OutputON` and `OutputOFF`, so it covers the switch, grid changes from other devices, and the device's own changes. For a battery it is raised when `BatteryPowerSource.powering` flips, through its private `PowerON` and `PowerOFF`, after the game's own signals for that change have run.
- **`PoweredChanged` does not fire while a save is loading.** A change that happens while the game restores its furniture on load is not reported.
- **A battery can lose or gain power with no action by the player.** A battery checks its items shortly after it starts, and after a load it works out how much it drained while you were away. Both can switch it, and both fire the event. If your handler should ignore them, compare `Get(id)` with your own record.
  - Technical: `LateStart` runs `CheckItems` two frames after the battery starts, and the post-load offline drain (`LoadDelay`) can switch it off after loading has finished.
- **`FuelRanOut` fires once when a fuel burner's fuel runs out.** It covers every crafting station with a fuel source, generators and stoves alike. `FuelRanOutArgs.Id` is the station's id. It does not fire while a save is loading or saving.
  - Technical: it is raised from the game's `Fuel.OnFuelConsumeStopped`, when the fuel counter was on and `BurnTimeRemaining` is 0.
- **Switching a burner off in the one moment after its fuel hits zero can also report a run-out.** This is a known quirk: the game runs its own run-out logic on that moment too.
  - Technical: it is a manual switch-off (`SetOn(id, false)`) in the one game tick after `BurnTimeRemaining` reaches 0.
- **A crafting station switching on or off does not fire `PoweredChanged`.** Use `WorldPower.GeneratorToggled` for a generator's switch, and `FuelRanOut` for a generator running out of fuel.
  - Technical: crafting station power changes that go through `OnPowerOn` or `OnPowerOff` (on load) raise nothing.
- **`IsAvailable` is `false` when the power events could not be set up.** Then `PoweredChanged` stops. The reads and `SetOn` keep working. See "If something fails to set up" below.
