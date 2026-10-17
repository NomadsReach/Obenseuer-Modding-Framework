---
title: What you can't do (yet)
parent: Power devices
grand_parent: Objects and devices
nav_order: 4
---

# What you can't do (yet)

- **You can't read the total power of a furniture grid or list the devices of a grid.**
  - Technical: `FurnitureManager.NetPower` and the per-manager device list are not available to mods.
- **You can't read a power draw for crafting stations or batteries.** The game does not use one.
  - Technical: `PowerSource.powerConsumption` is not used by the game.
- **You can't list which items a battery accepts as fuel.** A battery accepts fuel by item kind, not by a list of item numbers.
  - Technical: `BatteryPowerSource.fuelType` is a flag set, not item ids.
