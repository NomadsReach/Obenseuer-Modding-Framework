---
title: Adding and removing
parent: Loot pools
grand_parent: Items and crafting
nav_order: 4
---

# Adding and removing

## Things to know

- `Add` registers your item for a category and returns `true`. If the category and the item are already loaded it takes effect at once. Otherwise it waits and takes effect when both exist, so you can call it at any time, even before the game has finished loading. <span class="tech">(A waiting entry is applied after the next rebuild of the category database or when `ItemRegistry.Loaded` fires. You can call `Add` from a plugin's `Awake`.)</span>
- Calling `Add` again for the same item and category replaces your earlier entry. The result is the same as the last call, whether the first entry was already in effect or still waiting.
- If another mod already added the same item to the same category, both entries count and one warning names both mods. The weights add up.
- `Remove` takes out only what your mod added, never the game's items. It returns `true` when your mod had an entry for that category and item, whether it was in effect or waiting, and `false` otherwise.
- `Remove` only stops future picks. It does not take items out of containers that already hold them, or out of a batch that was already worked out. A container saves its next batch, so your item can sit in a save even when no slot holds it yet. <span class="tech">(The saved batch is `nextItemsToSpawn`.)</span>
- `DefineCategory(modId, name)` makes a new, empty category and returns `true`. Put items in it with `Add` and the same name.
- You can call `DefineCategory` at any time, as with `Add`. If the game's loot lists are not built yet, the category is remembered and made when they are. Categories are never removed. <span class="tech">(The category gets the number 1000000 plus its order, and is made again after every rebuild, as `Add` entries are.)</span>
- A new category is only used by code that asks for it by name. The game's own containers only name the game's categories, so none of them will ever draw from your category. <span class="tech">(Only code that sets `Storage.itemSpawnCategory` to the name uses it.)</span>

## Limits

- If a category or item never appears, your entry is skipped every time and error lines and a warning go to the log for it.
- `Add` returns `false` in a few cases. That is when `IsAvailable` is `false`, when the item number is 0 or 1 (the same numbers `Containers.Give` refuses), and while a save is loading or being written.
- `Add` throws an error for bad values. An invalid mod id, an empty category name, a tier that does not exist, or a weight outside 1 to 100 throws an `ArgumentException`.
- `Remove` also returns `false` while a save is loading or being written.
- `DefineCategory` returns `false` when the name is empty, when the loot pools are not available, when another category of that name was already made by a mod, or when the game already has a category of that name. If the name is used by another mod or by the game, one warning names the caller. If your own mod defines its own name again, you get `false` without a warning.
- If the game has a category with that name when its lists are built, yours is skipped and error lines and a warning go to the log.
