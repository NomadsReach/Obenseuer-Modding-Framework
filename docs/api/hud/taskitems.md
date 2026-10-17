---
title: TaskItems
parent: HUD
grand_parent: Screen and UI
nav_order: 7
---

# TaskItems

A task item is a quest item the player carries, such as a key or a recipe. `TaskItems` lists what the player holds, adds and removes them, and tells you when a count changes. It covers only the counts of these items. The quest list and its goals are on the [Quests](../quest-authoring.md) page.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TaskItemCount</code> <span>one task item the player holds</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class TaskItemCount
{
    public string Id { get; }
    public string Name { get; }
    public int Amount { get; }
    public string Type { get; }
    public string Details { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The item's id. |
| `Name` | `string` | The item's name. |
| `Amount` | `int` | How many the player holds. |
| `Type` | `string` | The kind of item, for example `"Keys"`. |
| `Details` | `string` | The item's description. `""` when the game has none. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TaskItemChangedArgs</code> <span>what Changed tells you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class TaskItemChangedArgs
{
    public string Id { get; }
    public string Name { get; }
    public string Type { get; }
    public int OldAmount { get; }
    public int NewAmount { get; }
    public bool Held { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The item's id. |
| `Name` | `string` | The item's name. |
| `Type` | `string` | The kind of item. |
| `OldAmount` | `int` | The count before the change. |
| `NewAmount` | `int` | The count after the change. |
| `Held` | `bool` | `true` when the player still holds the item after the change. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TaskItems</code> <span>the task items the player holds</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class TaskItems
{
    public static event Action<TaskItemChangedArgs> Changed;
    public static List<TaskItemCount> All();
    public static bool Has(string id);
    public static int Amount(string id);
    public static bool Add(string id, int amount);
    public static bool Remove(string id, int amount);
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<TaskItemCount>` | Every task item the player holds. |
| `Has(string id)` | `bool` | `true` when the player holds this item. |
| `Amount(string id)` | `int` | How many the player holds. `0` for none or an unknown id. |
| `Add(string id, int amount)` | `bool` | Gives the player some. `true` when the count went up. |
| `Remove(string id, int amount)` | `bool` | Takes some away. `true` when the count went down. |

| Event | Gives you | When it fires |
|---|---|---|
| `Changed` | `TaskItemChangedArgs` | Once for each item whose count changes, and once when an item is added or removed. |

</div>
</div>

## Things to know

- A task item's id is its own `id`, or its asset path, or its name. The game picks the first one it has. <span class="tech">(`Id` is `TaskItem.Id`: the `id` field, else the addressable asset path, else `itemName`. Task items are the `TaskItem` records held by `TaskItemsManager`, the list that the console's `add_task_item` command fills.)</span>
- `Add(id, amount)` gives the player the item, and `Remove(id, amount)` takes it away. `amount` must be above 0. <span class="tech">(They call `TaskItemsManager.AddTaskItem(taskItem, amount)` and `TaskItemsManager.RemoveItem(taskItem, amount)`.)</span>
- `Add` gives back `true` only when the count for that id went up. `Remove` gives back `true` when the count went down or the entry was removed. <span class="tech">(`Remove` gives back `true` when the first record with that id lost count or was removed, so a record at count 0 that the game removes also counts.)</span>
- `Remove` works from what the player already holds, so the item does not need to be loaded.
- `Add` and `Remove` only change the count. They do not run the item's give-item or send-mail steps, the same as the console's `add_task_item` command. <span class="tech">(They skip the item's `GiveItem` and `SendMail`.)</span>
- `Has(id)` is `true` when the player holds a record with that id, even at count 0. It gives back `false` for a null or empty id.
- `Type` and `Details` are for information. `Type` is the name of the kind of item the game gives it, such as `Keys` or `Recipes`. `Details` is its description text. <span class="tech">(`Type` is the name of the game's `TaskItem.Type` member (`Keys`, `Recipes`, `TaskItems` and so on). `Details` is `itemDetails`.)</span>
- `Changed` fires for every change, whether it comes from this API or from the game itself. Recipes are added at count 0, so they fire `Changed` too, with `Type` set to the recipe's type name. <span class="tech">(It comes from any source that goes through the game's `AddTaskItem` or `RemoveItem`.)</span>
- `Held` tells you whether the player still has the item after the change. A new item has `OldAmount` 0. An item whose count reaches 0 but stays on the list has `NewAmount` 0 and `Held` `true`. `Held` is `false` only when the item was removed from the list, and then `NewAmount` is 0.
- A handler may call `Add` or `Remove`. A change made inside a handler fires its own event before the rest of the first change's events. This can go eight levels deep, the same as `Cash.MoneyChanged`.
- The quest list is on another page. `TaskController` is the same system as `Quests`, so this page does not cover it again. `TaskPanel` is the task list screen and holds no game state a mod needs.
- `Add` does not give a blueprint. A blueprint also needs to be registered with the game's building system, which this API does not do. To give the player a blueprint for your mod's furniture, use `FurnitureRegistry.DefineBlueprint` and `FurnitureRegistry.GiveBlueprint` on the [custom furniture](../furniture.md) page. <span class="tech">(The item is a `TaskItemBlueprint`, and the game's add also registers it with `BuildingSystem`.)</span>

## Limits

- Both `Add` and `Remove` give back `false` while a save is loading or being written, and when the game's task item list is not loaded. <span class="tech">(The list check is `TaskItemsManager.instance` being null.)</span>
- You can only add an item the game has already loaded. A mod can't create a new task item, and an item the game has not loaded yet is not found, so `Add` gives back `false`.
- `Changed` does not fire while a save is loading.
- An `Add` or `Remove` made while the eighth event runs does not fire another event, and that is logged once.
