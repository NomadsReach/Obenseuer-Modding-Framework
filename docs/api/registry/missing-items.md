---
title: Missing items
parent: Registry
grand_parent: Items and crafting
nav_order: 8
---

# Missing items

A save can hold a stack of an item from a mod that is not installed. The game would turn that stack into a coin when it loads. The framework answers with a placeholder instead, so the stack keeps its number and shows the real item again when the mod returns. The item's number is at or above 1000000 (the lowest number the registry hands out) and is not in the game's item list.

- **The placeholder is called `Missing item`, or `Missing item (mod: title)` when the save's list names the item.** Its description says the mod is not installed or, when the mod is installed, that it could not add the item (see the framework log). When the mod is installed and loads, the stack shows the real item again.
- **The placeholder looks like a paper bag and has no use.** It has no categories, actions or attachments, and its value is 0. A stack can hold a huge number of them, far more than any real stack.
  - Technical: the icon is `Sprites/Items/Paper Bag_spr` and the prefab is `Prefabs/Items/ERROR`, both paths in `Items.json`. The stack size is 100000000, which stays below `int.MaxValue` after the difficulty multiplier (at most 4x).
- **The player can't sell the placeholder back, but a trader can still sell one.** A trader whose saved stock holds a removed mod's item still sells its `Missing item` stack, for 0 money.
  - Technical: `CanBuy` checks money only, so buying is not refused.
- **Recipes have no placeholder.** A saved copy of a missing recipe is not listed and can't be crafted until the mod returns.
  - Technical: crafting reads only recipes from the database.
- **Recycling refuses a placeholder.** The bottle recycling panel only accepts certain categories, and the placeholder has none.
- **The framework's other item calls still refuse an item number that is not in the game's item list.** That covers giving items, spawning, shop stock, containers, mail and others.

### Cleaning up

```csharp
public sealed class MissingItemInfo    // one stack of a missing item
{
    public int Id { get; }          // the item's number
    public string Mod { get; }      // the mod it came from, from the save's list; empty if unknown
    public string Title { get; }    // the item's name, from the save's list; empty if unknown
    public string Where { get; }    // where the stack is: "player inventory", or "container" and its name
    public int Amount { get; }      // how many are in the stack
}

public static class ItemRegistry
{
    public static List<MissingItemInfo> MissingItems();            // lists the missing stacks you can reach
    public static bool RemoveMissingItems(string modId);           // empties the missing stacks of one mod
    public static bool RemoveAllMissingItems();                    // empties the missing stacks of every mod
}
```

- **`MissingItems()` lists the placeholder stacks in the player's inventory and in the containers of the loaded scenes.** The inventory includes the backpack and the character slots. It gives an empty list while a save is loading or being written, and before the game's item list is built.
- **`RemoveMissingItems(modId)` empties the placeholder stacks of that mod in the places it can reach.** It also drops that mod's carried entries from the save's list: all of its recipe entries, and each of its item entries whose number no scanned stack holds after the clear. An item entry whose number a scanned stack still holds is kept. It returns `true`. A null or empty `modId` removes nothing: it returns `false` and writes a warning to the log once. A `modId` that is not empty but is not a valid mod id throws an `ArgumentException`. It returns `false` while a save is loading or being written, or before the game's item list is built.
- **`RemoveAllMissingItems()` empties the placeholder stacks of every mod, and drops every mod's carried entries by the same rule.** It returns `true`, and `false` in the same cases as `RemoveMissingItems`. When it removes a stack it writes a warning with the count for each mod, because it also clears mods the player only switched off for this session. Call it only from a clean-up the player asked for. A mod that clears its own stacks calls `RemoveMissingItems(modId)`.
- **An emptied stack is gone for good.** Installing the mod again does not bring it back.
- **Some places are not checked.** Stacks in scenes that are not loaded, on the floor, or in the list of items a container will spawn next are not cleared. The log says so. Run the call again after those scenes load. Their numbers are kept, but the mod's name is lost if the clean-up dropped its entry, because only the stacks it checked keep their entries.
  - Technical: the list of items a container will spawn next is `Storage.nextItemsToSpawn`.
- **The warning at load names this call as the way to clear the stacks.** There is no Mods menu entry for it, because the Mods menu has a Reset button on each row and no confirmation step. The clean-up is only available from code.
