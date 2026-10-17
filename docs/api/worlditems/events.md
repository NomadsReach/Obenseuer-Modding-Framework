---
title: Events
parent: World item events
grand_parent: Items and crafting
nav_order: 2
---

# Events

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

Item ids are the game's whole-number item ids. A world item's id is different: it is a piece of text (see "World item ids").

Each event gives you a small object with the facts about what happened. They tell you about something after it happened. You cannot cancel or change what the player did.

```csharp
public static class WorldItems
{
    public static event Action<PickedUpArgs> PickedUp;   // the player took a world item into the inventory
    public static event Action<DroppedArgs> Dropped;     // the player put an inventory item on the ground
    public static event Action<UsedArgs> Used;           // the player used a held item
    public static bool IsAvailable { get; }              // false when one of the three events could not be set up
}

public sealed class PickedUpArgs
{
    public int ItemId { get; }         // the picked up item's id
    public int Amount { get; }         // how many were taken
    public Vector3 Position { get; }   // where the item lay, in world coordinates
}

public sealed class DroppedArgs
{
    public int ItemId { get; }         // the dropped item's id
    public int Amount { get; }         // how many were dropped
    public Vector3 Position { get; }   // where the game put the item, in world coordinates
}

public sealed class UsedArgs
{
    public int ItemId { get; }         // the id of the item that was used
}
```

- **`PickedUp` fires once when the player takes a world item into the inventory.** `Amount` is how many units were taken. `Position` is where the item lay. Placed furniture and rat traps that the player picks up fire it too. If only part of a stack fits into the inventory, it does not fire.
  - Technical: it is raised after `CollectibleItem.PerformPickup`, which the game calls from the player's Interact and StopInteract (a tap). Furniture and rat-trap pickups also pass through `PerformPickup`.
- **`Dropped` fires when the player puts an inventory item on the ground.** That covers dragging it to the Remove Area, right-click drop, the amount slider, and the game's own drops of held items (using the primary button, and when the player unequips). `Position` is the spot the game chose, which is not the same as where the player stands. There is no "thrown" flag, because the game has no throwing.
- **`Used` fires when the player presses the Use or the secondary Use button on a held item.** It fires once for each use, not once for each action. A normal held item runs one action, so it fires once with that item's id. A liquid container runs all the actions of its liquid in one use, so it fires once with the liquid's id. A liquid with no actions fires nothing. Using a world item directly, and an active trade, do not fire it.
  - Technical: the event is raised after `Item.InvokeAction` and, for liquids, `Item.InvokeActions`; the nested `InvokeAction` calls inside `InvokeActions` raise nothing. A postfix still runs when another mod's prefix on `Item.InvokeAction` skips the game's action, so `Used` can fire for a use the game did not apply. The framework's own prefix sets the state the postfix reads, and it is not known in which order the two prefixes run.
- **For food and drink, use `Survival.Consumables.Consumed` instead of `Used`.** `Consumed` fires only after the game has applied the effects. It does not fire when a use was cancelled, and it also covers eating from the world and uses forced by depression. `Used` fires for every held item use (equipping, reading and tools too), before anyone knows the result, and not for use from the world. See [Survival](../../api-core/survival.md#survival).
- **`Spawn.Item` never raises `Dropped`, even when you call it from inside a handler for a drop.** See [World objects](../worldobjects/spawn.md#spawn).
- **Loading a save and spawning a scene never raise these events.** The game does not run the changed methods while it loads.
- **The events cannot be cancelled.** They only watch. A handler that throws an error cannot break the game or other mods: the framework logs it and still runs the others.
