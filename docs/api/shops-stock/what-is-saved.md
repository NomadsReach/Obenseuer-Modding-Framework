---
title: What is saved
parent: Shop stock
grand_parent: Money and shops
nav_order: 6
---

# What is saved

- Your trader stock and vending entries are not saved. Your mod adds them again on each start. This is the same as with [loot pools](../loot.md).
- `Place` records are saved with each save. They go in the save's mod data, in the file `obenseuer.moddingframework.shopstock.json`, together with the vending stock. The file is cleared when the player goes back to the main menu, like other mod save data. <span class="tech">(The file is in the save's `mods` folder.)</span>
