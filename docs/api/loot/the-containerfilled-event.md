---
title: The ContainerFilled event
parent: Loot pools
grand_parent: Items and crafting
nav_order: 5
---

# The ContainerFilled event

- **`ContainerFilled` fires after a container has been filled with at least one stack.** It covers the first fill, the refills when a save loads and on a timer, and any other fill of a container, whether it draws from a pool or not.
  - Technical: it is raised after `Storage.SpawnItems`. With no subscriber the framework does almost nothing extra: one check for `null`.
- **It does not fire for some ways items appear.** That covers the janitors' fills, items the player moves between containers, the contents of storage packages, and items that collectible spawners put into the world.
  - Technical: the janitor fills are `StorageJanitorAction`.
- **Your handlers run one at a time.** If one throws an error, the framework logs it and still runs the others.
