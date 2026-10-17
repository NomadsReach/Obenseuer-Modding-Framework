---
title: Notes
parent: Items
grand_parent: Items and crafting
nav_order: 4
---

# Notes

These notes explain what item quality means here, and how an `ItemDef` behaves once you have it.

## Things to know

- Quality and condition belong to each single item the player holds, not to the catalogue. `Quality` here is only the starting value in the catalogue.
- An `ItemDef` is a snapshot made when you asked, and a property read allocates nothing. `Price` and `MaxStack` show the difficulty at the time of the call, so do not keep an `ItemDef` and treat it as a live value. <span class="tech">(`EquipSlot`, `EquipType`, `AmmoItemIds`, `AmmoCategories`, `FuelBurnTime`, `Electronic` and `ElectronicType` are read once, when the `ItemDef` is built.)</span>
