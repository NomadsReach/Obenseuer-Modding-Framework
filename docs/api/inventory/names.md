---
title: Names
parent: Inventory
grand_parent: Items and crafting
nav_order: 2
---

# Names

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

- **The player inventory is called `PlayerInventory` and a stack of items is called `ItemStackInfo`.** The game's own names are `Inventory` and `ItemStack`, and the framework cannot use those names for its own types without clashing with them.
  - Technical: the framework's own code uses `Inventory` and `ItemStack` unqualified (`src/Story/Law.cs` reads `Inventory.instance`, and `src/Api/ShopsPlayer.cs` walks the game's `ItemStack`).
