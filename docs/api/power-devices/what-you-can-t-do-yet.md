---
title: What you can't do (yet)
parent: Power devices
grand_parent: Objects and devices
nav_order: 4
---

# What you can't do (yet)

- You can't list the devices of a particular grid through this API. The current building area's grid balance is available through `Building.CurrentArea`.
- You can't read a power draw for crafting stations or batteries. The game does not use one. <span class="tech">(`PowerSource.powerConsumption` is not used by the game.)</span>
- You can't list which items a battery accepts as fuel. A battery accepts fuel by item kind, not by a list of item numbers. <span class="tech">(`BatteryPowerSource.fuelType` is a flag set, not item ids.)</span>
