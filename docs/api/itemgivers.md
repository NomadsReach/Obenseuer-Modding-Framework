---
title: Item givers API
parent: API reference
nav_order: 33
---

# Item givers API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.itemgivers`.
Files: `src/Api/ItemGivers.cs`, `src/Api/ItemGiversPatches.cs`.

This area wraps `InteractableItemGiver`, the world object that hands items to the player when it is used. It reads the stock, empties or refills it, and reports each handout. It does not hand out items itself. See "Not exposed".

## If a hook cannot be installed

The framework installs one hook: a postfix on the game's private `GiveItem`. If it fails, the owner is disabled and `ItemGivers.IsAvailable` is `false`. `All`, `SetEmpty` and `Respawn` keep working; `Gave` stops.

## Threading and safety

- Main thread only.
- `ItemGivers.IsAvailable` is `false` when the hook was not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, `All` returns an empty list.
- Nothing throws. A null or empty `id` finds nothing.
- `SetEmpty` and `Respawn` return `false` while a save is loading or saving (`SaveData.Busy`).

## API

```csharp
public static class ItemGivers
{
    public static List<ItemGiverInfo> All();
    public static bool SetEmpty(string id, bool empty);
    public static bool Respawn(string id);

    public static event Action<ItemGiverArgs> Gave;

    public static bool IsAvailable { get; }
}

public sealed class ItemGiverInfo
{
    public string Id { get; }      // the save GUID, or the scene path
    public string Title { get; }   // Interactable.title
    public bool CanGive { get; }   // InteractableItemGiver.canGive
    public bool Empty { get; }     // true when the stock is 0; always false for an unlimited giver
    public int Amount { get; }     // the stock left; -1 for an unlimited giver
}

public sealed class ItemGiverArgs
{
    public string Id { get; }      // save GUID, or the scene path
    public int ItemId { get; }     // ItemReference.ID of the item handed over
    public int Amount { get; }     // how many of that item were handed over
}
```

- `All()` lists every `InteractableItemGiver` in the active scene. It uses `FindObjectsByType`, which skips inactive GameObjects.
- A giver is unlimited when either stock bound is `-1`. That is the game's own rule (`HasInfiniteAmount`). `Amount` and `Empty` come from the private stock field and the private bounds, read through cached reflection.
- `SetEmpty(id, empty)` refuses an unlimited giver. If `empty` is `true` and the stock is above 0, the stock becomes 0. If `empty` is `false` and the stock is 0, the stock becomes 1, the smallest stock that lets the giver hand out an item. Use `Respawn` for the game's own random refill. The change runs the game's private `UpdateState`, which fires `onEmpty` or `onNotEmpty`. When the stock already matches, nothing fires. It returns `true` when `Empty` now equals the request.
- `Respawn(id)` refuses an unlimited giver. Otherwise it runs the game's private `RefillItemGiver`. That sets a random stock between the two bounds, fires the relays, and resets the spawn timer. It returns `true` when the refill ran. It does not change `CanGive`. A giver that disabled itself after a handout (`selfDisable`) stays disabled.
- `Gave` fires when the game's private `GiveItem` reports that the items were added to the player's inventory. It fires once for each item stack. It fires before the game lowers the stock and before `onInteract`, so a handler that reads `Amount` on the giver sees the stock from before that handout.

## Not exposed

- Handing out items. The API never calls `GiveItems`. A handout is always the game's own, from the player's use or a game script.
- Requirements and removals: `requiredItems`, `removeItems`, `requiredTaskItem` and `removeTaskItem`.
- The item pool and the random pick: `itemsToGive`, `randomItemToGive`, `AddItemToPool` and `RemoveItemFromPool`.
- The respawn timer: `spawnSettings`.

## Ceilings and risks

- `SetEmpty(id, false)` sets the stock to 1 when it is 0. That number is not from the game. Use `Respawn` for the game's own range.
- `Respawn` does not reset `CanGive`, so a giver that switched itself off stays off.
- The `Gave` hook reads the item and amount from the game's own call. A giver that hands out through another path that does not call `GiveItem` will not raise `Gave`.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
