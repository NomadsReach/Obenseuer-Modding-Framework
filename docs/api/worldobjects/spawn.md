---
title: Spawn
parent: World objects
grand_parent: Objects and devices
nav_order: 5
---

# Spawn

This page uses `Vector3`, `GameObject` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

`Spawn.Item` drops new items into the world at a position, the way the game drops an item.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Spawn</code> <span>drops items into the world</span></p>
Use `Item` to drop items into the world at a position.

<div class="api-pair" markdown="1">

```csharp
public static class Spawn
{
    public static bool Item(int itemId, int count, Vector3 position);
    public static bool Item(int itemId, int count, Vector3 position, out GameObject spawned);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Item(itemId, count, position)` | `bool` | Drops the items. Returns `true` when they were placed. |
| `Item(itemId, count, position, out spawned)` | `bool` | The same, and also gives you the first object it made. |

</div>
</div>

## Things to know

- `Item` drops `count` of an item at `position`, and the player can pick them up. The game makes each one, starts its expiry timer, and registers it so it is saved. <span class="tech">(it builds each stack the way the game's drop does, with `ItemOperations.InstantiateItem` and `SetCollectibleItemValues`, the steps of `SpawnItemToPosition`; the game registers it in `DestructibleList`)</span>
- The `out GameObject spawned` version also gives you the first object it made. It is `null` when nothing was made. Its id for other APIs comes from `WorldItems.IdOf` (see [World items](../worlditems.md)).
- Spawned items use the game's expiry, so they disappear unless the player picks them up. `WorldItems.SetExpiryOverride` changes the time for later default spawns (see [World items](../worlditems.md), Expiry default).
- Spawned items have no save id. A save id on a spawned item would stop the game from saving it: the game would look for a scene object that does not exist when the save loads.

## Limits

- `count` must be 1 to 100 for each call, or `Item` throws an `ArgumentException`. The limit is for one call only. There is no limit on how many calls you make, and every call drops its items at the same `position`. The items are split into stacks of at most the item's stack size. <span class="tech">(the stack size is `Item.Stackable`)</span>
- `Item` returns `false` when it can't place the item. That is on the main menu, while a save is loading or being written, for an unknown item id, and when the game's own call fails (the error goes to the log).
- The coins (item ids 0 and 1) are refused. Use `Wallet` for money.
- The position is used as given. The game does not move the item down to the ground, so pass a point on the floor.
- The game's option for items with no timer (`noExpirationTimer`) is not available here.
- You can't spawn arbitrary prefabs. There is no `Spawn.Prefab`, because the game has no list of names for general prefabs. The game finds item prefabs by item title, and only for items. <span class="tech">(item prefabs are loaded by title through `Resources` (`Item.ItemAppearance.GetPrefabByTitle`))</span>
