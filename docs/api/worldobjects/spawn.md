---
title: Spawn
parent: World objects
grand_parent: Objects and devices
nav_order: 5
---

# Spawn

`Spawn.Item` drops new items into the world at a position, the way the game drops an item.

```csharp
public static class Spawn
{
    public static bool Item(int itemId, int count, Vector3 position);                         // drops the items; true when they were placed
    public static bool Item(int itemId, int count, Vector3 position, out GameObject spawned); // the same, and also gives you the first object it made
}
```

- **`Item` drops `count` of an item at `position`, and the player can pick them up.** The game makes each one, starts its expiry timer, and registers it so it is saved. The coins (item ids 0 and 1) are refused: use `Wallet` for money.
  - Technical: it builds each stack the way the game's drop does, with `ItemOperations.InstantiateItem` and `SetCollectibleItemValues`, the steps of `SpawnItemToPosition`. The game registers it in `DestructibleList`.
- **The `out GameObject spawned` version also gives you the first object it made.** It is `null` when nothing was made. Its id for other APIs comes from `WorldItems.IdOf` (see [World items](../worlditems.md)).
- **`count` must be 1 to 100 for each call, or `Item` throws an `ArgumentException`.** The limit is for one call only. There is no limit on how many calls you make, and every call drops its items at the same `position`. The items are split into stacks of at most the item's stack size.
  - Technical: the stack size is `Item.Stackable`.
- **`Item` returns `false` when it can't place the item.** That is on the main menu, while a save is loading or being written, for an unknown item id, and when the game's own call fails (the error goes to the log).
- **The position is used as given.** The game does not move the item down to the ground, so pass a point on the floor.
- **Spawned items have no save id.** A save id on a spawned item would stop the game from saving it: the game would look for a scene object that does not exist when the save loads.
- **You can't spawn arbitrary prefabs.** There is no `Spawn.Prefab`, because the game has no list of names for general prefabs. The game finds item prefabs by item title, and only for items.
  - Technical: item prefabs are loaded by title through `Resources` (`Item.ItemAppearance.GetPrefabByTitle`).
- **Spawned items use the game's expiry, so they disappear unless the player picks them up.** `WorldItems.SetExpiryOverride` changes the time for later default spawns (see [World items](../worlditems.md), Expiry default). The game's option for items with no timer (`noExpirationTimer`) is not available here.
