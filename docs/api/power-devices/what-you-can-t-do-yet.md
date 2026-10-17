---
title: What you can't do (yet)
parent: Power devices
grand_parent: Objects and devices
nav_order: 4
---

# What you can't do (yet)

- You can't read the total power of a furniture grid or list the devices of a grid. <span class="tech">(`FurnitureManager.NetPower` and the per-manager device list are not available to mods.)</span>
- You can't read a power draw for crafting stations or batteries. The game does not use one. <span class="tech">(`PowerSource.powerConsumption` is not used by the game.)</span>
- You can't list which items a battery accepts as fuel. A battery accepts fuel by item kind, not by a list of item numbers. <span class="tech">(`BatteryPowerSource.fuelType` is a flag set, not item ids.)</span>
