---
title: Reading
parent: Loot pools
grand_parent: Items and crafting
nav_order: 3
---

# Reading

## Things to know

- `IsAvailable` tells you whether adding and the event work. `Add`, `Remove` and `ContainerFilled` need it. `Categories` and `PoolFor` do not. <span class="tech">(It is true when the loot patches are installed and their feature group is not turned off.)</span>
- `Categories()` lists every category the game has. It is empty when the game's loot database is not ready yet, for example on the main menu.
- `PoolFor(category)` lists the items of one category. The common items come first and then the rare ones, each item once, in the order they first appear, with `Weight` counting the copies. An unknown category gives an empty list.
