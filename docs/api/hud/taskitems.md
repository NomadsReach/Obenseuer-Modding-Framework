---
title: TaskItems
parent: HUD
grand_parent: Screen and UI
nav_order: 7
---

# TaskItems

A task item is a quest item the player carries, such as a key or a recipe. `TaskItems` lists what the player holds, adds and removes them, and tells you when a count changes. It covers only the counts of these items. The quest list and its goals are on the [Quests](../quest-authoring.md) page.

```csharp
public sealed class TaskItemCount    // one task item the player holds
{
    public string Id { get; }         // the item's id
    public string Name { get; }       // the item's name
    public int Amount { get; }        // how many the player holds
    public string Type { get; }       // the kind of item, for example "Keys"
    public string Details { get; }    // the item's description; "" when the game has none
}

public sealed class TaskItemChangedArgs    // what Changed tells you
{
    public string Id { get; }          // the item's id
    public string Name { get; }        // the item's name
    public string Type { get; }        // the kind of item
    public int OldAmount { get; }      // the count before the change
    public int NewAmount { get; }      // the count after the change
    public bool Held { get; }          // true when the player still holds the item after the change
}

public static class TaskItems
{
    public static event Action<TaskItemChangedArgs> Changed;   // a task item's count changed
    public static List<TaskItemCount> All();      // every task item the player holds
    public static bool Has(string id);            // true when the player holds this item
    public static int Amount(string id);          // how many the player holds; 0 for none or an unknown id
    public static bool Add(string id, int amount);      // gives the player some; true when the count went up
    public static bool Remove(string id, int amount);   // takes some away; true when the count went down
}
```

- **A task item's id is its own `id`, or its asset path, or its name.** The game picks the first one it has.
  - Technical: `Id` is `TaskItem.Id`: the `id` field, else the addressable asset path, else `itemName`. Task items are the `TaskItem` records held by `TaskItemsManager`, the list that the console's `add_task_item` command fills.
- **`Add(id, amount)` gives the player the item, and `Remove(id, amount)` takes it away.** `amount` must be above 0. `Add` gives back `true` only when the count for that id went up. `Remove` gives back `true` when the count went down or the entry was removed. Both give back `false` while a save is loading or being written, and when the game's task item list is not loaded.
  - Technical: they call `TaskItemsManager.AddTaskItem(taskItem, amount)` and `TaskItemsManager.RemoveItem(taskItem, amount)`. `Remove` gives back `true` when the first record with that id lost count or was removed, so a record at count 0 that the game removes also counts. The list check is `TaskItemsManager.instance` being null.
- **You can only add an item the game has already loaded.** A mod can't create a new task item, and an item the game has not loaded yet is not found, so `Add` gives back `false`.
- **`Add` does not give a blueprint.** A blueprint also needs to be registered with the game's building system, which this API does not do. To give the player a blueprint for your mod's furniture, use `FurnitureRegistry.DefineBlueprint` and `FurnitureRegistry.GiveBlueprint` on the [custom furniture](../furniture.md) page.
  - Technical: the item is a `TaskItemBlueprint`, and the game's add also registers it with `BuildingSystem`.
- **`Add` and `Remove` only change the count.** They do not run the item's give-item or send-mail steps, the same as the console's `add_task_item` command.
  - Technical: they skip the item's `GiveItem` and `SendMail`.
- **`Remove` works from what the player already holds, so the item does not need to be loaded.**
- **`Has(id)` is `true` when the player holds a record with that id, even at count 0.** It gives back `false` for a null or empty id.
- **`Type` and `Details` are for information.** `Type` is the name of the kind of item the game gives it, such as `Keys` or `Recipes`. `Details` is its description text.
  - Technical: `Type` is the name of the game's `TaskItem.Type` member (`Keys`, `Recipes`, `TaskItems` and so on). `Details` is `itemDetails`.
- **`Changed` fires once for each item whose count changes, and once when an item is added or removed.** It fires for every change, whether it comes from this API or from the game itself. Recipes are added at count 0, so they fire `Changed` too, with `Type` set to the recipe's type name.
  - Technical: it comes from any source that goes through the game's `AddTaskItem` or `RemoveItem`.
- **`Held` tells you whether the player still has the item after the change.** A new item has `OldAmount` 0. An item whose count reaches 0 but stays on the list has `NewAmount` 0 and `Held` `true`. `Held` is `false` only when the item was removed from the list, and then `NewAmount` is 0.
- **`Changed` does not fire while a save is loading.**
- **A handler may call `Add` or `Remove`, up to a limit.** A change made inside a handler fires its own event before the rest of the first change's events. This can go eight levels deep, the same as `Cash.MoneyChanged`. An `Add` or `Remove` made while the eighth event runs does not fire another event, and that is logged once.
- **The quest list is on another page.** `TaskController` is the same system as `Quests`, so this page does not cover it again. `TaskPanel` is the task list screen and holds no game state a mod needs.
