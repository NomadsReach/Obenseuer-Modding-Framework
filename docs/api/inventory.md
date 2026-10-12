# Inventory and container API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.inventory`.
Files: `src/Api/Inventory.cs`, `src/Api/InventoryPatches.cs`, `src/Api/InventoryStack.cs`, `src/Api/Containers.cs`.

Everything not listed here is internal. Item ids are the game's `int` `Item.ID`.

## Names

The facade does not use the game's `Inventory` and `ItemStack` names, because the Core code uses those unqualified and would not compile:

- `PlayerInventory` for the player inventory facade (`src/Story/Law.cs` reads `Inventory.instance` unqualified).
- `ItemStackInfo` for the stack type (`src/Api/ShopsPlayer.cs` iterates the game's `ItemStack`).

## Wiring

```csharp
internal static void PlayerInventory.Init();
```

Core calls this from `Core.Awake`.

`Init` installs four hooks under one owner, `obenseuer.moddingframework.inventory`:

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
- `ItemAdded` and `ItemRemoved` (payload `ItemChangedArgs`: `ItemId`, `Count`) fire from the game's slot add and take methods, filtered to player slots (main grid, backpack, equipment slots). `Give` and `Take` raise one event per item id for the whole operation (`Take(id, 5)` raises one `ItemRemoved(id, 5)`). Game code that adds or removes items itself (pickups, crafting, consuming) still raises one event per game call, and crafting or selling removes one unit per call, so sum the counts.
- Inventory drag and drop, swaps, and the item transfers started from the container screen raise no events. Shift-click moves out of a container raise `ItemAdded`, and moves into a container raise nothing. The ground drop from the drag area, removals that bypass the slot take method (quality or liquid removals, `Storage.RemoveItem` on a backpack) and loading a save raise no event either. Treat the events as a feed of script and gameplay changes and use `Count` for the truth.
- If the game's own add finds the inventory full it drops the rest on the ground and `ItemAdded` still counts those units. `Give` avoids this with its fit check.

## ItemStackInfo

```csharp
public sealed class ItemStackInfo
{
    public int ItemId { get; }
    public int Count { get; }
    public int OwnerId { get; }          // -1 for the player's own items
    public float Quality { get; }        // -1 when the item has no quality data
    public int IntegrityPercent { get; } // -1 when the item does not spoil
    public int LiquidItemId { get; }     // -1 when there is no liquid
    public float LiquidAmount { get; }
}
```

- The game stores no spoil timer. `IntegrityPercent` is the game's own integrity (`ItemPerishabledata.GetIntegrityPercent`), which falls by the item's loss per hour.
- `LiquidAmount` is the game's `ItemLiquidData.GetLiquidAmount` for the stack size.

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
    public static List<ItemStackInfo> GetItems(ContainerInfo container);
    public static bool Give(ContainerInfo container, int itemId, int count);
    public static bool Take(ContainerInfo container, int itemId, int count);
}
```

- `All` finds every active `Storage` in the loaded scene with `FindObjectsByType`, which scans the whole scene, then builds a snapshot per container. Call it once and cache the result; do not call it every frame. Shops, vending machines, prisons and the equipped backpack's storage are included; check `Name`. `ContainerInfo` is a snapshot: `Locked` and `Name` are not updated, but `Give` and `Take` check the lock again.
- `GetItems` reads the game's `storageSlots`.
- `Give` refuses when the container is locked or when no slot can hold the whole amount (checked against every slot). Otherwise it calls the game's `Storage.AddItem`, which places into slot 0 and its siblings. In an unusual slot layout that call could still drop the overflow in the world. Giving to an NPC-owned container is allowed, as in the game.
- `Take` refuses when the container is locked, when it is NPC-owned and `PermissionToTake` is false, or when it does not hold the whole amount. Otherwise it calls the game's `Storage.RemoveItem` and refreshes the container's slots. The removed items are destroyed, not moved: nothing is added to the player's inventory, so a mod that wants a transfer must `PlayerInventory.Give` the same amount itself, and should do so first. The game shows `Steal` for NPC-owned containers, but its theft path is not a public method a facade can call, so the facade refuses rather than bypass it.

## Smoke test (BepInEx console)

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
