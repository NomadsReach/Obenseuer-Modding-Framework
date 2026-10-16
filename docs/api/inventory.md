---
title: Inventory and container API
parent: API reference
nav_order: 31
---

# Inventory and container API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.inventory`.
Files: `src/Api/Inventory.cs`, `src/Api/InventoryPatches.cs`, `src/Api/InventoryStack.cs`, `src/Api/Containers.cs`, `src/Api/ContainersPatches.cs`.

Everything not listed here is internal. Item ids are the game's `int` `Item.ID`.

## Names

The facade does not use the game's `Inventory` and `ItemStack` names, because the framework's own code uses those unqualified and would not compile:

- `PlayerInventory` for the player inventory facade (`src/Story/Law.cs` reads `Inventory.instance` unqualified).
- `ItemStackInfo` for the stack type (`src/Api/ShopsPlayer.cs` iterates the game's `ItemStack`).

## If a hook cannot be installed

The framework installs four hooks under one owner, `obenseuer.moddingframework.inventory`:

- `SlotController.AddItemAndReturnRemaining` postfix and `SlotController.TakeItem(Item, int, int, object[])` postfix. These raise `ItemAdded` and `ItemRemoved`.
- `SlotController.TakeItem(ItemData)` and `SlotController.DropItem` prefix/finalizer pairs. These count UI drags, so the events stay quiet while the player moves items in a UI.

If any of the four is missing, every inventory hook is removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.inventory")` becomes true, and one warning is logged. Reads and writes keep working; only the events stop.

## Threading and safety

- Main thread only.
- `PlayerInventory.IsAvailable` is false when the inventory hooks were not installed or the owner was disabled. Reads and writes are not affected.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return `0`, `false` or an empty list.
- Writes return `bool`. Invalid counts (`<= 0`) throw `ArgumentException`. Nothing else throws.

## Player inventory

```csharp
public static class PlayerInventory
{
    public static int FreeSlots { get; }
    public static int Capacity { get; }
    public static int Count(int itemId);
    public static bool Has(int itemId, int count);
    public static bool Give(int itemId, int count);
    public static bool Take(int itemId, int count);
    public static List<ItemStackInfo> Items();
    public static int GetHeldItemId(bool leftHand);
    public static bool EquipHeld(string modId, int itemId);
    public static bool UnequipHeld(string modId, bool leftHand);
    public static event Action<ItemChangedArgs> ItemAdded;   // args.ItemId, args.Count
    public static event Action<ItemChangedArgs> ItemRemoved;
}
```

- `Count` counts the stacks whose item id equals `itemId`, in every inventory slot (backpack and character slots included).
- `Has` uses the game's `CheckForItem`, which ignores ownership and counts plain item stacks only.
- `Give` refuses (returns `false`, adds nothing) unless the whole amount fits in the main inventory grid. When it fits, it uses the game's dialogue add (`Inventory.AddItemDialogueAmount`): the same stack rules and the game's "Items added!" notification. That add only reaches the main grid, never the equipped backpack or the equipment slots, and drops anything that does not fit on the ground while still returning true. So the fit check covers the main grid only: a full grid with free backpack space makes `Give` return `false`. Money ids `0` and `1` are refused; use `Wallet`.
- `Take` returns `false` unless the whole amount is held. Then it uses the game's dialogue remove (`Inventory.RemoveItemDialogueAmount`), which takes one unit at a time from the first matching stack in slot order (main grid, then backpack, then equipment slots). It does not prefer spoiled, low-quality or own items. A stack with `ownerId > 0` that is taken while a witness sees it commits theft, following the game's own crime rule; check `Items()` first when that matters. Money is not kept as item stacks, so `Take` returns `false` for ids `0` and `1`; use `Wallet`.
- `Items()` lists the non-empty stacks in every inventory slot, backpack and character slots included.
- `FreeSlots` and `Capacity` count the same slots as `Items()`, so they include the backpack and the equipment slots (hat, watch, hands, backpack slot). `FreeSlots` is not the room `Give` can use, which is the main grid only.
- `GetHeldItemId` returns the id of the item in the right or left hand (`PlayerHandItems`), or `-1`. The game does not track a stack count for a hand item.
- `EquipHeld(modId, itemId)` puts one unowned unit from the main grid into a hand. The item's category picks the hand: `Wieldable Primary` is the right hand, `Wieldable Secondary` the left. It calls the game's `Inventory.EquipItem`, the call that `ItemEquipable.Invoke` makes, so the hand's `OnDropped` and `OnTaken` callbacks run as in the game. It refuses (returns `false`, changes nothing) when the item has no hand category, when the main grid holds no single unowned unit of it, when the hand's slot rules refuse the item, when the hand holds an owned item, when the item or the other hand is two-handed and the other hand is full, when the mod id is invalid, during a save or a load, or from an `ItemAdded` or `ItemRemoved` handler nested deeper than 8 levels.
- `UnequipHeld(modId, leftHand)` moves the item in that hand into the main grid, through the game's `Inventory.Unequip`. It refuses an empty hand, an owned item (the game does not let the player take off an owned worn item) and an `Unstorable` item (the game shows no Unequip button for it). It returns `true` only when the hand is empty afterwards, so a full main grid gives `false` and the item stays in the hand.
- Hats, faces, watches and backpacks are not held items. Use `Wardrobe.Equip` and `Wardrobe.Unequip`, which use the same game call.
- An equip or unequip made through the framework raises `ItemAdded` for each stack the game adds to a player slot, and no `ItemRemoved`, because the game clears the source stack without its take method. A swap into an occupied hand raises two `ItemAdded`. `Count` does not change, so count the stacks rather than summing the events.
- `EquipHeld` does not run the item action's side effects: `ItemEquipable.Invoke` first calls the base `Item.ItemAction.Invoke`, which plays the use sound and raises the `ItemConsumed` event and the achievement check, and then plays the item sound. The facade skips all of them. Owned stacks are refused, so the owner theft check in the base call does not apply.
- `ItemAdded` and `ItemRemoved` (payload `ItemChangedArgs`: `ItemId`, `Count`) fire from the game's slot add and take methods, filtered to player slots (main grid, backpack, equipment slots). `Give` and `Take` raise one event per item id for the whole operation (`Take(id, 5)` raises one `ItemRemoved(id, 5)`). Game code that adds or removes items itself (pickups, crafting, consuming) still raises one event per game call, and crafting or selling removes one unit per call, so sum the counts.
- Inventory drag and drop, and the item transfers started from the container screen raise no events. Equips and unequips made through `EquipHeld`, `UnequipHeld`, `Wardrobe.Equip` and `Wardrobe.Unequip` raise `ItemAdded` only (see the held item bullets above). Shift-click moves out of a container raise `ItemAdded`, and moves into a container raise nothing. The ground drop from the drag area, removals that bypass the slot take method (quality or liquid removals, `Storage.RemoveItem` on a backpack) raise no event either. The framework does not gate `ItemAdded` or `ItemRemoved` on a save load (see Save loads). Treat the events as a feed of script and gameplay changes and use `Count` for the truth.
- `Give` and `Take` return `false` when they are called from an `ItemAdded` or `ItemRemoved` handler that is nested deeper than 8 levels. The first refusal is logged once. `Give` and `Take` also return `false` while a trader trade runs (see [traders.md](traders.md)).
- If the game's own add finds the inventory full it drops the rest on the ground. Those dropped units are not counted: `ItemAdded` reports only what went into the inventory. `Give` also checks fit before it adds.

## ItemStackInfo

```csharp
public sealed class ItemStackInfo
{
    public int ItemId { get; }
    public int Count { get; }
    public int Slot { get; }             // storage slot index for GetItems and TakeStack, -1 when none
    public int OwnerId { get; }          // -1 for the player's own items
    public float Quality { get; }        // -1 when the item has no quality data
    public int IntegrityPercent { get; } // -1 when the item does not spoil
    public int LiquidItemId { get; }     // -1 when there is no liquid
    public float LiquidAmount { get; }
    public bool IsStolen { get; }        // the stack's own stolen flag, see below
    public bool HasOwnerPermission { get; }
    public float Charge { get; }         // -1 when the item has no ammo data
    public float MaxCharge { get; }      // -1 when the item has no ammo data
    public float WalletCoins { get; }    // -1 when the item has no wallet data
    public int WalletOc { get; }         // -1 when the item has no wallet data
    public bool IsWritable { get; }      // true when the item has writable data
    public string WrittenTitle { get; }  // null when none
    public string WrittenText { get; }   // null when none
}
```

- `Slot` is the storage slot index of a stack from `GetItems`, its position in `storageSlots`, and is the value to pass as `slot` to `Containers.TakeStack` or `PlayerShops.Sell`. It is -1 for stacks not in a container slot: `PlayerInventory.Items`, `StackActions.StolenStacks` and trash piles from `Cleaning.TrashItems`. Dishwasher contents from `Dishwashers.Contents` come from `GetItems`, so they carry the index too. No framework code reads `Slot`; those two calls take the index as their own argument and refuse -1.
- The game stores no spoil timer. `IntegrityPercent` is the game's own integrity (`ItemPerishabledata.GetIntegrityPercent`), which falls by the item's loss per hour.
- `LiquidAmount` is the game's `ItemLiquidData.GetLiquidAmount` for the stack size.
- `IsStolen` is the stack's own `ItemStolenData.playerStolen` flag. The game sets it when a stack with an owner id of 2 or more enters the player's inventory with no owner permission, whether or not a witness saw it; a witness only decides whether the theft crime is also committed. It does not show a crime, a witness or an owner's claim, so it is not the game's crime state. The game's mental-health expectation reads it for a used item.
- `HasOwnerPermission` is true when the stack has an `ItemOwnerPermission` with `ownerPermission` set. Every meta entry is checked. The class defaults `ownerPermission` to true, so presence usually means permitted. `allowUse` is not exposed.
- `Charge` and `MaxCharge` are the game's `ItemAmmodata.ammo` and `maxAmmo` for the stack.
- `WalletCoins` is `ItemOSCoinWalletData.coins`, and `WalletOc` is its `ToOC()`, the game's rounded OC value.
- `IsWritable` is true when the stack has `ItemWritableData`, so a note with no title can be told apart from a stack with no writable data. `WrittenTitle` and `WrittenText` are the game's `title` and `content`.

## Containers

```csharp
public sealed class ContainerInfo
{
    public string Guid { get; }
    public int InstanceId { get; }
    public string Name { get; }          // the game's GetTitle(), which adds " (Empty)" when empty
    public Vector3 Position { get; }
    public int Apartment { get; }        // -1 when not in an apartment
    public int OwnerId { get; }          // the NPC owner, -1 when none
    public bool NpcOwned { get; }        // owner id > 1, the game's "Steal" condition
    public bool Locked { get; }
}

public static class Containers
{
    public static List<ContainerInfo> All();
    public static ContainerInfo FindByGuid(string guid);
    public static string GuidOf(ContainerInfo container);
    public static List<ItemStackInfo> GetItems(ContainerInfo container);
    public static bool Give(ContainerInfo container, int itemId, int count);
    public static bool Take(ContainerInfo container, int itemId, int count);
    public static ItemStackInfo TakeStack(ContainerInfo container, int slot, int count);

    public static event Action<ContainerInfo> Opened;
    public static event Action<ContainerInfo> Closed;
}
```

- `All` finds every active `Storage` in the active scene with `FindObjectsByType`, which scans the whole scene, then builds a snapshot per container. Call it once and cache the result; do not call it every frame. Shops, vending machines, prisons and the equipped backpack's storage are included; check `Name`. `ContainerInfo` is a snapshot: `Locked` and `Name` are not updated, but `Give` and `Take` check the lock again.
- `GetItems` reads the game's `storageSlots`, skips empty slots, and sets each stack's `Slot` to its index there. Pass `info.Slot` to `TakeStack` or `PlayerShops.Sell` to take that exact stack.
- `Give` refuses when the container is locked or when no slot can hold the whole amount (checked against every slot). Otherwise it calls the game's `Storage.AddItem`, which places into slot 0 and its siblings. In an unusual slot layout that call could still drop the overflow in the world. Giving to an NPC-owned container is allowed, as in the game.
- `Take` refuses when the container is locked, when it is NPC-owned and `PermissionToTake` is false, or when it does not hold the whole amount. Otherwise it calls the game's `Storage.RemoveItem` and refreshes the container's slots. The removed items are destroyed, not moved: nothing is added to the player's inventory, so a mod that wants a transfer must `PlayerInventory.Give` the same amount itself, and should do so first. The game shows `Steal` for NPC-owned containers, but its theft path is not a public method a facade can call, so the facade refuses rather than bypass it.

- `TakeStack(container, slot, count)` takes `count` items from the stack in slot `slot`, the index in the game's `Slots` order, and returns that stack as an `ItemStackInfo` with its owner and meta. It returns `null` when the container is locked, is NPC-owned and `PermissionToTake` is false, the slot is out of range or empty, the stack holds fewer than `count`, or a save is busy. As with `Take`, the items are destroyed, not moved to the player. Pass `ItemStackInfo.Slot` from `GetItems` as `slot`; -1 is refused. It also returns `null` when the slot's stack is no longer the one `GetItems` reads (the container's slots were rebuilt), so list again before taking.
- `FindByGuid(guid)` returns the container whose save GUID is `guid`, or `null`. It first uses the game's GUID registry, the same lookup `SaveData.CaptureByGuid` and `SaveData.ApplyByGuid` use. The game fills that registry only for objects it matched to an entry in the loaded save, so when the registry has no live `Storage` for the GUID, `FindByGuid` scans the live scene with `SaveController.GetObjectByGUID(guid, typeof(Storage))`. That fallback calls `FindObjectsOfType` once, so each miss costs one whole-scene scan; do not call it every frame. It returns `null` for an empty or null GUID (no scan), for an unknown GUID, and for a destroyed container.
- `GuidOf(container)` returns `container.Guid`, or `null` for `null`. An empty string means the container has no GUID.
- `Opened` and `Closed` fire once per open and once per close of a `Storage` container, and carry a `ContainerInfo` snapshot built as `All` builds it. `Opened` fires when the game opens the container's storage. `Closed` fires when the game closes it, or when the container is disabled or destroyed while open (a scene change). The framework tracks the one container it has open, so a repeated close raises nothing, and opening a container while another is open raises `Closed` for the old one first. Use `container.Guid` (or `GuidOf`) to identify it on another PC.
- Close paths: the menu close (the game's `HideGameMenu`, which runs for the "Pause Menu" binding while a menu is open, the menu's own close buttons and the other close calls) and the container's own exit (crafting, vending machines, trades and backpacks close their storage themselves) both raise `Closed`. Walking away is not a separate path: while a container menu is open the game disables movement, so the player cannot walk away from it. A container opened without a menu is closed by its owner, which the exit hook covers.
- `Opened` and `Closed` also cover the storages behind crafting stations, vending machines, trades and the equipped backpack, because the game opens them through the same `Storage` methods. `LiquidStorage` (bottles and tanks) has its own enter and exit and raises nothing.
- `Opened` and `Closed` mean the storage was entered or exited, not that a container menu is visible. `Storage.EnterExternally` runs its side effects, including setting `Storage.active` and the `Opened` raise, even when no menu shows. The menu is shown only when the `isInteractable | forceOpen` check at `Storage` passes.
- Backpack, cooler bag and rucksack storages, crafting input and output storages, and trade and vending storages fire them too.
- With container A open, opening the backpack raises `Closed(A)`, then `Opened(backpack)`. Closing the backpack raises `Closed(backpack)`, then `Opened(A)`, because `BackpackStorage.CloseBackpack` re-interacts with the last used container. A lock-while-open mod sees A close and reopen on every backpack toggle.
- A mod that tracks world containers should ignore entries with an empty `Guid`. Those have no save identity, so `FindByGuid` cannot return them and another PC cannot match them.
- Handlers run synchronously on the main thread. A handler that throws is logged and the other handlers still run. The framework does not gate these events on a save load (see Save loads).

## Example

Run after a save is loaded, from a test plugin on the main thread:

```csharp
int id = ITEM_ID;   // placeholder: a real item id (0 and 1 are money)
Log.LogInfo("count " + PlayerInventory.Count(id) + " has1 " + PlayerInventory.Has(id, 1) + " free " + PlayerInventory.FreeSlots + "/" + PlayerInventory.Capacity);
Log.LogInfo("give " + PlayerInventory.Give(id, 2) + " count now " + PlayerInventory.Count(id));
Log.LogInfo("take " + PlayerInventory.Take(id, 2) + " count now " + PlayerInventory.Count(id));
Log.LogInfo("stacks " + PlayerInventory.Items().Count + " held right " + PlayerInventory.GetHeldItemId(false));
Log.LogInfo("containers " + Containers.All().Count);
```

Expected: `give` true and count +2, `take` true and count back. The `ItemAdded` and `ItemRemoved` handlers log each event.

## Save loads

`ItemAdded` and `ItemRemoved` are not gated on a save load, so a slot add or take made while a save loads still fires. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
