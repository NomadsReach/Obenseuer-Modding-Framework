---
title: Obenseuer Modding Framework wardrobe API (worn inventory items)
parent: API reference
nav_order: 88
---

# Obenseuer Modding Framework wardrobe API (worn inventory items)

Experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.wardrobe`. Logs go to `BepInEx\logs\obenseuer.moddingframework.wardrobe.log`.

Everything not listed here is internal. Main thread only.

Scope: the four worn slots of the player's inventory, which the game calls Head, Face, Watch and Backpack. Appearance (body, face and clothing meshes) is `PlayerLook`. The hands (Wieldable Primary and Secondary) are not in this facade.

## Rules

- **Game path only.** `Equip` calls `Inventory.EquipItem`, the same method `ItemEquipable` calls when the player uses a wearable. `Unequip` calls `Inventory.Unequip`. The facade never writes to the slot data itself.
- **Bag only.** `Equip` takes the item from the bag (`Inventory.Slots`). A wearable in a backpack, or already worn, is not moved.
- **Saves.** Nothing is stored by the facade. The game saves the worn items itself.
- **Owned items.** The game's owned-item rule applies. `Unequip` refuses a worn item that has an owner. `Equip` refuses a bag item whose owner does not allow its use, and it does not swap out a worn item that has an owner. A refusal returns `false` and is logged once.
- **Bad arguments.** A `null` slot, an unknown slot name or a negative item id throws `ArgumentException`. Everything else returns the fallback.

## API

```csharp
public static class Wardrobe
{
    public static bool IsAvailable { get; }
    public static List<string> Slots();              // "Head", "Face", "Watch", "Backpack", in that order
    public static int WornOn(string slot);           // item id, or -1 when the slot is empty or not available
    public static bool Equip(int itemId);            // true when the item is worn afterwards
    public static bool Unequip(string slot);         // true when an item was taken off, false when the slot is empty or its item is owned
    public static event Action<WardrobeChangedArgs> Changed;
}

public sealed class WardrobeChangedArgs
{
    public string Slot { get; }                      // the slot that changed
    public int ItemId { get; }                       // the item worn there now, or -1
}
```

- `Equip` returns `false` when the item is not in the bag, its owner does not allow its use, has no wearable-slot category, or the game does not put it on. When the item has a category for more than one slot, the first matching category is used. If another item is worn in that slot, the game swaps it into the bag.
- `Unequip` returns `false` when nothing is worn there, when the worn item has an owner, or when the game cannot move the item to the bag (for example, the bag is full).
- `Changed` fires after the item worn in Head, Face, Watch or Backpack changes. That covers `Equip`, `Unequip`, and the player dragging an item into or out of a worn slot, including a drag that is cancelled and put back. It fires once per operation and only when the worn item id differs afterwards (or the game's `Equip` or `Unequip` ran). It is not raised for the hand slots.
- Gap: a worn item the game removes by script, for example when it spoils, does not raise `Changed`. A load does not raise it either.
- `Equip` also fails (returns `false`, nothing moves) when the slot would refuse the item. The slot's own category rules decide, so an item that is also `Unstorable` is not equipped. The game plays the item's sound in `ItemEquipable.Invoke`; `Equip` does not.
- Inventory events: the `PlayerInventory` events ([inventory.md](inventory.md)) count the worn slots as player slots. `Equip` and `Unequip` raise neither `ItemAdded` nor `ItemRemoved`, because they run inside the inventory's UI scope. Other moves into a worn slot can still raise them there. Those are separate events from `Changed`, and `Changed` is raised once per operation.
- A `Changed` handler may call `Equip` or `Unequip`, up to 8 nested levels. Past that both return `false`, and the first refusal is logged once.
- `IsAvailable` is `installed && !CorePatcher.IsDisabled(owner)`. `WornOn`, `Equip` and `Unequip` do not depend on it.

## Notes

- A wearable's slot is its category (`Head`, `Face`, `Watch` or `Backpack`). `ItemDef.EquipSlot` uses the same category names and also reports the two hand slots.
- The game's hand slots are not reachable through this facade.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
