---
title: Names
parent: Inventory
grand_parent: Items and crafting
nav_order: 2
---

# Names

The framework's inventory types have their own names, so they don't clash with the game's.

## Things to know

- The player inventory is called `PlayerInventory` and a stack of items is called `ItemStackInfo`. The game's own names are `Inventory` and `ItemStack`, and the framework cannot use those names for its own types without clashing with them. <span class="tech">(The framework's own code uses `Inventory` and `ItemStack` unqualified (`src/Story/Law.cs` reads `Inventory.instance`, and `src/Api/ShopsPlayer.cs` walks the game's `ItemStack`).)</span>
