---
title: Player inventory
parent: Inventory
grand_parent: Items and crafting
nav_order: 3
---

# Player inventory

`PlayerInventory` counts, gives and takes items for the player, tells you what the player holds in each hand, and tells you when items are added or removed. Items are named by their item id (see [Items](../items.md)). Money is not an item here: ids `0` and `1` are money, and money goes through `Wallet` (see the [core API page](../../api-core/wallet.md#wallet)).

```csharp
public static class PlayerInventory
{
    public static int FreeSlots { get; }                             // how many slots are empty
    public static int Capacity { get; }                              // how many slots there are in all
    public static int Count(int itemId);                             // how many of this item the player has
    public static bool Has(int itemId, int count);                   // true when the player has at least this many
    public static bool Give(int itemId, int count);                  // gives items; true when all of them were added
    public static bool Take(int itemId, int count);                  // takes items; true when all of them were taken
    public static List<ItemStackInfo> Items();                       // every stack the player carries
    public static int GetHeldItemId(bool leftHand);                  // the item in a hand; -1 when the hand is empty
    public static bool EquipHeld(string modId, int itemId);          // puts one item from the main grid into a hand
    public static bool UnequipHeld(string modId, bool leftHand);     // moves the item in a hand back into the main grid
    public static bool IsAvailable { get; }                          // false when the added and removed events could not be set up
    public static event Action<ItemChangedArgs> ItemAdded;           // items were added to the player's slots
    public static event Action<ItemChangedArgs> ItemRemoved;         // items were removed from the player's slots
}

public sealed class ItemChangedArgs    // what you get when items are added or removed
{
    public int ItemId { get; }     // the item's id
    public int Count { get; }      // how many were added or removed
}
```

- **Pass a count above zero.** A count of `0` or less throws an `ArgumentException`. Nothing else throws.
- **`Count(id)` counts that item in every slot the player has.** That includes the backpack and the equipment slots.
- **`Has(id, count)` is `true` when the player has at least that many.** It does not care who owns the items, and it counts ordinary item stacks only.
  - Technical: it uses the game's `CheckForItem`.
- **`Give` adds the whole amount or nothing.** It returns `false` and adds nothing unless the whole amount fits in the main grid. It also shows the game's "Items added!" notice. It only fills the main grid, never the equipped backpack or the equipment slots, so a full main grid with free backpack space makes `Give` return `false`. Money ids `0` and `1` are refused: use `Wallet`.
  - Technical: it uses the game's dialogue add (`Inventory.AddItemDialogueAmount`) after a fit check of the main grid. That game call drops what does not fit on the ground and still returns `true`, which is why the fit check comes first.
- **`Take` removes the whole amount or nothing.** It returns `false` unless the player holds the whole amount. It takes one unit at a time from the first matching stack in slot order: the main grid, then the backpack, then the equipment slots. It does not prefer spoiled, low-quality or the player's own items. If a stack that belongs to someone else is taken while a witness sees it, that is theft, the same as in the game. Check `Items()` first when that matters. Money is not kept as item stacks, so `Take` returns `false` for ids `0` and `1`: use `Wallet`.
  - Technical: it uses the game's dialogue remove (`Inventory.RemoveItemDialogueAmount`). A stack with `ownerId > 0` is an owned stack.
- **`Give` and `Take` return `false` while the player is in a trade with a trader.** See [Traders](../traders.md).
- **`Give` and `Take` return `false` when an `ItemAdded` or `ItemRemoved` handler calls them more than 8 levels deep.** The first refusal goes to the log.
- **`Items()` lists the stacks that are not empty, in every slot.** That includes the backpack and the character slots.
- **`FreeSlots` and `Capacity` count the same slots as `Items()`.** That includes the backpack and the equipment slots (hat, watch, hands and the backpack slot). `FreeSlots` is not the room that `Give` can use, because `Give` uses the main grid only.
- **`GetHeldItemId` gives the id of the item in the right or left hand, or `-1`.** The game does not keep a count for a held item.
- **`EquipHeld(modId, itemId)` puts one item from the main grid into a hand.** The item's category picks the hand: `Wieldable Primary` is the right hand and `Wieldable Secondary` is the left. The hand's drop and take reactions run as they do in the game. It returns `false` and changes nothing when any of these is true:
  - the item has no hand category;
  - the main grid has no single unit of it that nobody owns;
  - the hand's own rules refuse the item;
  - the hand holds an item that belongs to someone;
  - the item, or the item in the other hand, needs both hands and the other hand is full;
  - the mod id is not valid;
  - a save is loading or being written;
  - a handler for `ItemAdded` or `ItemRemoved` calls it more than 8 levels deep.
  - Technical: it calls the game's `Inventory.EquipItem`, the call that `ItemEquipable.Invoke` makes.
- **`EquipHeld` does not do what the game does when the player uses the item.** It plays no use sound, raises no consumed event, does not run the achievement check, and plays no item sound. Items that belong to someone are refused, so the theft check that goes with ownership does not apply.
  - Technical: `ItemEquipable.Invoke` first calls the base `Item.ItemAction.Invoke`, which plays the use sound and raises `ItemConsumed` and the achievement check, then plays the item sound. `EquipHeld` skips all of these.
- **`UnequipHeld(modId, leftHand)` moves the item in that hand into the main grid.** It refuses an empty hand, an item that belongs to someone (the game does not let the player take off an owned worn item), and an item that cannot be stored (the game shows no Unequip button for it). It returns `true` only when the hand is empty afterwards, so a full main grid gives `false` and the item stays in the hand.
  - Technical: it calls the game's `Inventory.Unequip`. An item that cannot be stored has the `Unstorable` category.
- **Hats, masks, watches and backpacks are not held items.** Use `Wardrobe.Equip` and `Wardrobe.Unequip` for those (see [Wardrobe](../wardrobe.md)). They use the same game call.
- **`ItemAdded` and `ItemRemoved` fire when items go into or come out of the player's slots.** The slots are the main grid, the backpack and the equipment slots. `Give` and `Take` raise one event for each item id for the whole call: `Take(id, 5)` raises one `ItemRemoved` with a count of 5. When the game itself adds or removes items (pickups, crafting, using things up), you get one event for each game call. Crafting and selling remove one unit per call, so add the counts up.
  - Technical: they are raised after the game's slot add and take methods (`SlotController.AddItemAndReturnRemaining` and `SlotController.TakeItem`), filtered to player slots.
- **Equipping and unequipping raise `ItemAdded` but not `ItemRemoved`.** This is true for `EquipHeld`, `UnequipHeld`, `Wardrobe.Equip` and `Wardrobe.Unequip`. The game clears the source stack without calling its normal take method. Swapping into a hand that is already full raises two `ItemAdded`. `Count` does not change, so count the stacks rather than adding up the events.
- **Some moves raise no event at all.** Dragging items around the inventory, and the item transfers started from the container screen, raise nothing. Shift-click moves out of a container raise `ItemAdded`, and moves into a container raise nothing. Dropping an item on the ground from the drag area raises nothing, and neither do removals that skip the slot's take method (quality or liquid removals, and `Storage.RemoveItem` on a backpack). Treat the events as a feed of script and gameplay changes, and use `Count` for the truth.
- **The events do not hold back while a save is loading.** An add or take made while a save loads still fires. A handler that should ignore those can check `SaveData.Busy`, which is `true` while a save is loading or being written.
- **Items that the game drops on the ground because the inventory was full are not counted.** `ItemAdded` reports only what went into the inventory.
