---
title: Missing items
parent: Registry
grand_parent: Items and crafting
nav_order: 8
---

# Missing items

A save can hold a stack of an item from a mod that is not installed. The game would turn that stack into a coin when it loads. The framework answers with a placeholder instead, so the stack keeps its number and shows the real item again when the mod returns. The item's number is at or above 1000000 (the lowest number the registry hands out) and is not in the game's item list.

## Things to know

- The placeholder is called `Missing item`, or `Missing item (mod: title)` when the save's list names the item. Its description says the mod is not installed or, when the mod is installed, that it could not add the item (see the framework log). When the mod is installed and loads, the stack shows the real item again.
- The placeholder looks like a paper bag and has no use. It has no categories, actions or attachments, and its value is 0. A stack can hold a huge number of them, far more than any real stack. <span class="tech">(The icon is `Sprites/Items/Paper Bag_spr` and the prefab is `Prefabs/Items/ERROR`, both paths in `Items.json`. The stack size is 100000000, which stays below `int.MaxValue` after the difficulty multiplier (at most 4x).)</span>
- A trader whose saved stock holds a removed mod's item still sells its `Missing item` stack, for 0 money. The player can't sell the placeholder back, but a trader can still sell one. <span class="tech">(`CanBuy` checks money only, so buying is not refused.)</span>
- Recipes have no placeholder. A saved copy of a missing recipe is not listed and can't be crafted until the mod returns. <span class="tech">(Crafting reads only recipes from the database.)</span>
- Recycling refuses a placeholder. The bottle recycling panel only accepts certain categories, and the placeholder has none.
- The framework's other item calls still refuse an item number that is not in the game's item list. That covers giving items, spawning, shop stock, containers, mail and others.

## Cleaning up


<div class="api-type" markdown="1">
<p class="api-type-head"><code>ItemRegistry</code> <span>the clean-up calls for missing stacks</span></p>

Use `MissingItems()` to list the placeholder stacks, and the two remove calls to empty them.

<div class="api-pair" markdown="1">

```csharp
public static class ItemRegistry
{
    public static List<MissingItemInfo> MissingItems();
    public static bool RemoveMissingItems(string modId);
    public static bool RemoveAllMissingItems();
}
```

| Method | Returns | What it does |
|---|---|---|
| `MissingItems()` | `List<MissingItemInfo>` | Lists the missing stacks you can reach. |
| `RemoveMissingItems(modId)` | `bool` | Empties the missing stacks of one mod. |
| `RemoveAllMissingItems()` | `bool` | Empties the missing stacks of every mod. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MissingItemInfo</code> <span>one stack of a missing item</span></p>

`MissingItems()` gives you one of these for each placeholder stack. `Where` says where the stack is, and `Amount` says how many are in it.

<div class="api-pair" markdown="1">

```csharp
public sealed class MissingItemInfo
{
    public int Id { get; }
    public string Mod { get; }
    public string Title { get; }
    public string Where { get; }
    public int Amount { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `int` | The item's number. |
| `Mod` | `string` | The mod it came from, from the save's list. Empty if unknown. |
| `Title` | `string` | The item's name, from the save's list. Empty if unknown. |
| `Where` | `string` | Where the stack is: "player inventory", or "container" and its name. |
| `Amount` | `int` | How many are in the stack. |

</div>
</div>

- `MissingItems()` lists the placeholder stacks in the player's inventory and in the containers of the loaded scenes. The inventory includes the backpack and the character slots. It gives an empty list while a save is loading or being written, and before the game's item list is built.
- `RemoveMissingItems(modId)` empties the placeholder stacks of that mod in the places it can reach. It also drops that mod's carried entries from the save's list: all of its recipe entries, and each of its item entries whose number no scanned stack holds after the clear. An item entry whose number a scanned stack still holds is kept. It returns `true`.
- A null or empty `modId` removes nothing: it returns `false` and writes a warning to the log once. A `modId` that is not empty but is not a valid mod id throws an `ArgumentException`.
- `RemoveAllMissingItems()` empties the placeholder stacks of every mod, and drops every mod's carried entries by the same rule. It returns `true`, and `false` in the same cases as `RemoveMissingItems`.
- When `RemoveAllMissingItems()` removes a stack it writes a warning with the count for each mod, because it also clears mods the player only switched off for this session. Call it only from a clean-up the player asked for. A mod that clears its own stacks calls `RemoveMissingItems(modId)`.
- The warning at load names this call as the way to clear the stacks. There is no Mods menu entry for it, because the Mods menu has a Reset button on each row and no confirmation step. The clean-up is only available from code.

## Limits

- `MissingItems()`, `RemoveMissingItems` and `RemoveAllMissingItems` return an empty list or `false` while a save is loading or being written, or before the game's item list is built.
- An emptied stack is gone for good. Installing the mod again does not bring it back.
- Some places are not checked. Stacks in scenes that are not loaded, on the floor, or in the list of items a container will spawn next are not cleared. The log says so. Run the call again after those scenes load. Their numbers are kept, but the mod's name is lost if the clean-up dropped its entry, because only the stacks it checked keep their entries. <span class="tech">(The list of items a container will spawn next is `Storage.nextItemsToSpawn`.)</span>
