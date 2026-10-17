---
title: Events
parent: World item events
grand_parent: Items and crafting
nav_order: 2
---

# Events

This page uses `Vector3` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Item ids are the game's whole-number item ids, and a world item's id is different: it is a piece of text (see "World item ids"). Each event gives you a small object with the facts about what happened. They tell you about something after it happened, and you cannot cancel or change what the player did.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WorldItems</code> <span>world item events</span></p>
Use these events to hear when an item is picked up, dropped or used in the world.

<div class="api-pair" markdown="1">

```csharp
public static class WorldItems
{
    public static event Action<PickedUpArgs> PickedUp;
    public static event Action<DroppedArgs> Dropped;
    public static event Action<UsedArgs> Used;
    public static bool IsAvailable { get; }
}
```

| Event | Gives you | When it fires |
|---|---|---|
| `PickedUp` | `PickedUpArgs` | The player takes a world item into the inventory. |
| `Dropped` | `DroppedArgs` | The player puts an inventory item on the ground. |
| `Used` | `UsedArgs` | The player uses a held item. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when one of the three events could not be set up. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PickedUpArgs</code> <span>what PickedUp tells you</span></p>
`PickedUp` gives you this when the player picks up a world item. `Amount` is how many, and `Position` is where it was.

<div class="api-pair" markdown="1">

```csharp
public sealed class PickedUpArgs
{
    public int ItemId { get; }
    public int Amount { get; }
    public Vector3 Position { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ItemId` | `int` | The picked up item's id. |
| `Amount` | `int` | How many were taken. |
| `Position` | `Vector3` | Where the item lay, in world coordinates. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DroppedArgs</code> <span>what Dropped tells you</span></p>
`Dropped` gives you this when the player drops an item into the world. `Amount` is how many, and `Position` is where it landed.

<div class="api-pair" markdown="1">

```csharp
public sealed class DroppedArgs
{
    public int ItemId { get; }
    public int Amount { get; }
    public Vector3 Position { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ItemId` | `int` | The dropped item's id. |
| `Amount` | `int` | How many were dropped. |
| `Position` | `Vector3` | Where the game put the item, in world coordinates. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>UsedArgs</code> <span>what Used tells you</span></p>
`Used` gives you this when an item action runs. `ItemId` is the id of the item.

<div class="api-pair" markdown="1">

```csharp
public sealed class UsedArgs
{
    public int ItemId { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ItemId` | `int` | The id of the item that was used. |

</div>
</div>

## Things to know

- `PickedUp` fires once when the player takes a world item into the inventory. `Amount` is how many units were taken. `Position` is where the item lay. Placed furniture and rat traps that the player picks up fire it too. <span class="tech">(It is raised after `CollectibleItem.PerformPickup`, which the game calls from the player's Interact and StopInteract (a tap). Furniture and rat-trap pickups also pass through `PerformPickup`.)</span>
- `Dropped` fires when the player puts an inventory item on the ground. That covers dragging it to the Remove Area, right-click drop, the amount slider, and the game's own drops of held items (using the primary button, and when the player unequips).
- `Position` on `Dropped` is the spot the game chose, which is not the same as where the player stands. There is no "thrown" flag, because the game has no throwing.
- `Used` fires when the player presses the Use or the secondary Use button on a held item. It fires once for each use, not once for each action. <span class="tech">(The event is raised after `Item.InvokeAction` and, for liquids, `Item.InvokeActions`; the nested `InvokeAction` calls inside `InvokeActions` raise nothing.)</span>
- A normal held item runs one action, so `Used` fires once with that item's id. A liquid container runs all the actions of its liquid in one use, so it fires once with the liquid's id. A liquid with no actions fires nothing.
- For food and drink, use `Survival.Consumables.Consumed` instead of `Used`. `Consumed` fires only after the game has applied the effects. It does not fire when a use was cancelled, and it also covers eating from the world and uses forced by depression. `Used` fires for every held item use (equipping, reading and tools too), before anyone knows the result, and not for use from the world. See [Survival](../../api-core/survival.md#survival).
- The events cannot be cancelled. They only watch. A handler that throws an error cannot break the game or other mods: the framework logs it and still runs the others.

## Limits

- If only part of a stack fits into the inventory, `PickedUp` does not fire.
- Using a world item directly, and an active trade, do not fire `Used`.
- `Used` can fire for a use the game did not apply. <span class="tech">(A postfix still runs when another mod's prefix on `Item.InvokeAction` skips the game's action. The framework's own prefix sets the state the postfix reads, and it is not known in which order the two prefixes run.)</span>
- `Spawn.Item` never raises `Dropped`, even when you call it from inside a handler for a drop. See [World objects](../worldobjects/spawn.md#spawn).
- Loading a save and spawning a scene never raise these events. The game does not run the changed methods while it loads.
