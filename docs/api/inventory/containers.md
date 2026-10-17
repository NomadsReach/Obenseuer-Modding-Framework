---
title: Containers
parent: Inventory
grand_parent: Items and crafting
nav_order: 5
---

# Containers

A container is a chest, locker, shelf or any other storage the player can open. `Containers` finds them, looks inside, moves items in and out, and tells you when the player opens or closes one. Each container is a `ContainerInfo`, a snapshot taken when you asked.

```csharp
public sealed class ContainerInfo    // a snapshot of one container
{
    public string Guid { get; }          // its save id; "" when it has none
    public int InstanceId { get; }       // the game's number for this object in this session
    public string Name { get; }          // its title, with " (Empty)" added when it is empty
    public Vector3 Position { get; }     // where it is, in world coordinates
    public int Apartment { get; }        // the apartment number it is in; -1 when it is not in an apartment
    public int OwnerId { get; }          // the id of the NPC who owns it; -1 when nobody does
    public bool NpcOwned { get; }        // true when an NPC owns it, so taking from it counts as stealing
    public bool Locked { get; }          // true when it is locked
}

public static class Containers
{
    public static List<ContainerInfo> All();                                            // every container in the scene you are in
    public static ContainerInfo FindByGuid(string guid);                                // the container with this save id; null when none has it
    public static string GuidOf(ContainerInfo container);                               // the container's save id; null for a null container
    public static List<ItemStackInfo> GetItems(ContainerInfo container);                // the stacks in the container, without the empty slots
    public static bool Give(ContainerInfo container, int itemId, int count);            // puts items into the container; true when all of them went in
    public static bool Take(ContainerInfo container, int itemId, int count);            // takes items out and destroys them; true when all were taken
    public static ItemStackInfo TakeStack(ContainerInfo container, int slot, int count);   // takes items from one slot and destroys them; null when it could not

    public static event Action<ContainerInfo> Opened;   // the player opened a container
    public static event Action<ContainerInfo> Closed;   // the player closed a container
}
```

- **`All()` lists every container in the scene you are in, and it is slow, so do not call it every frame.** It searches the whole scene each time. Call it once and keep the result. It includes shops, vending machines, prisons and the storage of the equipped backpack, so check `Name`. A `ContainerInfo` does not update by itself: `Locked` and `Name` stay as they were, but `Give` and `Take` check the lock again each time.
  - Technical: it finds every active `Storage` in the active scene with `FindObjectsByType`.
- **`GetItems` lists the stacks in a container, without the empty slots, and sets each stack's `Slot`.** Pass that `Slot` to `TakeStack` or `PlayerShops.Sell` to take that exact stack.
  - Technical: it reads the game's `storageSlots`, and `Slot` is the index there.
- **`Give` puts items into a container.** It refuses when the container is locked, or when no slot can hold the whole amount. Giving to a container that an NPC owns is allowed, as it is in the game. Money ids `0` and `1` are refused.
  - Technical: it checks the fit against every slot, then calls the game's `Storage.AddItem`, which places into slot 0 and its siblings. In an unusual slot layout that call could still drop the overflow in the world.
- **`Take` removes items from a container and destroys them.** It refuses when the container is locked, when an NPC owns it and the player may not take from it, or when it does not hold the whole amount. Nothing is added to the player's inventory, so a mod that wants a transfer must call `PlayerInventory.Give` with the same amount itself, and should do that first. The game would call taking from an NPC's container stealing. The framework refuses instead of getting around that.
  - Technical: it calls the game's `Storage.RemoveItem` and refreshes the container's slots. The refusal is `PermissionToTake` being false. The game's theft path is not a public method the framework can call.
- **`TakeStack(container, slot, count)` takes `count` items from the stack in one slot, and gives you that stack's details.** The result is an `ItemStackInfo` with who owns it and its extra data. It returns `null` when the container is locked, when an NPC owns it and the player may not take from it, when the slot is out of range or empty, when the stack holds fewer than `count`, when a save is loading or being written, and when `slot` is `-1`. As with `Take`, the items are destroyed, not moved to the player. It also returns `null` when the container's slots were rebuilt since you listed them, so list again before taking.
- **`FindByGuid(guid)` finds a container by its save id, or gives `null`.** It gives `null` for an empty or `null` id, for an unknown id, and for a container that was destroyed. When the game's quick lookup has no such container, it searches the whole scene once, so every miss costs one full search: do not call it every frame.
  - Technical: it first uses the game's GUID registry, the same lookup `SaveData.CaptureByGuid` and `SaveData.ApplyByGuid` use. The game fills that registry only for objects it matched to an entry in the loaded save. The fallback is `SaveController.GetObjectByGUID(guid, typeof(Storage))`, which calls `FindObjectsOfType` once.
- **`GuidOf(container)` gives the container's save id, or `null` for `null`.** An empty text means the container has no save id.
- **A mod that tracks containers should ignore the ones with an empty `Guid`.** They have no save identity, so `FindByGuid` cannot return them, and another player's game cannot match them.
- **`Opened` fires once each time a container is opened and `Closed` once each time it is closed.** Each gives you a `ContainerInfo` built the same way as in `All()`. `Closed` also fires when the container is switched off or destroyed while open, for example when the scene changes. Closing something twice raises nothing. Opening a container while another is open raises `Closed` for the old one first. Use `container.Guid` to point at a container on another player's game.
  - Technical: `Opened` is raised after `Storage.EnterExternally`, and `Closed` after `Storage.ExitExternally` or `Storage.OnDisable`. The framework tracks the one container it has open.
- **The two events cover more than chests.** They also cover backpack, cooler bag and rucksack storage, crafting input and output storage, and trade and vending storage, because the game opens them all the same way. Bottles and tanks (liquid storage) have their own way in and out, and raise nothing.
- **A container can count as open while no menu shows.** The events mean that the storage was entered or left, not that a container menu is visible.
  - Technical: `Storage.EnterExternally` runs its side effects, including setting `Storage.active` and raising `Opened`, even when no menu shows. The menu is shown only when the `isInteractable | forceOpen` check in `Storage` passes.
- **Every way a container closes raises `Closed`.** That covers the menu closing (the pause key while a menu is open, the menu's own close buttons) and the container closing itself (crafting, vending machines, trades and backpacks close their own storage). The player cannot walk away from an open container menu, because the game stops movement while it is open. A container opened without a menu is closed by whatever opened it, and that is covered too.
  - Technical: the menu close is the game's `HideGameMenu`.
- **Opening the backpack while a container is open closes that container and reopens it later.** With container A open, opening the backpack raises `Closed(A)` and then `Opened(backpack)`. Closing the backpack raises `Closed(backpack)` and then `Opened(A)`, because the game opens the last used container again. A mod that locks a container while it is open sees A close and reopen on every backpack toggle.
  - Technical: `BackpackStorage.CloseBackpack` re-interacts with the last used container.
- **Your handlers run one at a time, right when the game opens or closes the container.** If a handler throws an error, the framework logs it and still runs the other handlers. The framework does not hold these events back while a save is loading either.
