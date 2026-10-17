---
title: Limits
parent: Registry
grand_parent: Items and crafting
nav_order: 10
---

# Limits

- You can add items and recipes only before the game loads. What you add lives in the game's memory. The game's data files are not changed.
- A new recipe shows up at a station the next time the station refreshes its recipe list. The framework does not control when that happens. <span class="tech">(The station runs `UpdateRecipes`.)</span>
- A save that holds a removed mod's item shows a placeholder for that stack (see [Missing items](missing-items.md#missing-items)). The load writes a warning once for each saved entry.
- A save made while a mod is missing keeps that mod's entries, and each stack keeps its number through that save. The game's lookup gives the placeholder the stack's own number, and the package and start-selection saves copy the number from the stack. Moving a stack copies the number too. <span class="tech">(The load paths get the placeholder from `FetchItemByID` (`ItemStack.Meta` through `SetItemReference`, and `Inventory.OnLoadingGame`), so the reference holds the stack's own ID. The package and start-selection saves copy `itemReference.ID` into `itemId` for each stack whose reference is set, so they write the same ID without any rebuild. A move copies the reference's ID as well (`ItemStack.ChangeItemReference`, `Storage`).)</span>
- A saved copy of a removed mod's recipe keeps its item numbers. The items in it show as placeholders, so the copy is complete, but it is not listed or crafted until the mod returns. Saved copies are not checked. <span class="tech">(`SetItems` resolves the saved item IDs to placeholders.)</span>
- Nothing puts a mod item in a shop, a loot table or a container for you. Those parts of the framework need the item's number: see [Loot pools](../loot.md) and the shop pages.
- `RemoveMissingItems` does not clean stacks in scenes that are not loaded, on the floor, or in the list of items a container will spawn next. A stack on the floor or in the next-spawn list is never scanned, so no call clears it.
- A number that no scanned stack holds loses its carried entry in the save's list, even when one of those unscanned stacks still holds it. That entry only gives the placeholder its mod name and title. The stack keeps its number, so it shows the real item again when the mod returns.
- Once the entry is dropped, such a stack shows `Missing item` without a mod name. `RemoveMissingItems(mod)` then skips it, because the stack has no mod name to match.
- Only `RemoveAllMissingItems` clears it, and only once its place is loaded, since the scan never reads scenes that are not loaded.
- Dropping a placeholder stack keeps its number. The game's drop would normally lose the number, so the framework sets it before the game finishes the drop. The pickup and the floor save then use that number. The drop is not refused, because swapping an equipped item removes the slot after the spawn, so a refused drop would lose the item. <span class="tech">(The game's drop spawns the placeholder's ERROR prefab and never sets `CollectibleItem.Item`, so the framework's drop patch sets `Item` to a reference to the stack's id before the game's `ItemOperations.SetCollectibleItemValues` runs.)</span>
