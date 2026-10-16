---
title: World item events API
parent: API reference
nav_order: 92
---

# World item events API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.worlditems`.
Files: `src/Api/WorldItems.cs`, `src/Api/WorldItemsPatches.cs`.

Everything not listed here is internal. Item type ids are the game's `int` `Item.ID`. A world item's id is a `string` (see World item ids).

## Events

```csharp
public static event Action<PickedUpArgs> PickedUp;
public static event Action<DroppedArgs> Dropped;
public static event Action<UsedArgs> Used;
public static bool IsAvailable { get; }
```

Payloads are sealed classes with get-only properties:

- `PickedUpArgs`: `int ItemId`, `int Amount`, `Vector3 Position`.
- `DroppedArgs`: `int ItemId`, `int Amount`, `Vector3 Position`.
- `UsedArgs`: `int ItemId`.

Events are postfixes. They fire after the game method has finished, and a throwing subscriber cannot break the game or other mods.

- `PickedUp`: the player takes a world collectible into the inventory. `Amount` is the number of units taken. `Position` is the collectible's position. It fires once per full pickup, through `CollectibleItem.PerformPickup`, which the game calls from the player's Interact and StopInteract (tap). Placed-furniture pickups and rat-trap pickups also pass through `PerformPickup`, so they fire too. A partial pickup (not everything fit) does not fire.
- `Dropped`: the player puts an inventory item on the ground. This covers the inventory drop (drag to the Remove Area, right-click drop, the amount slider) and the game's own wieldable drops (primary use and unequip). `Position` is the spawn point the game chose. It is not the player's position. There is no `thrown` flag, because the game has no throw action.
- `Used`: the player presses the Use or secondary Use button on a held inventory item. It fires once per use, not once per action. A held item runs one action, so it fires once with the held item's id. A liquid container runs all actions of its liquid in one use, so it fires once with the liquid's id. A liquid with no actions fires nothing. A world item used directly does not fire. An active trade does not fire. The postfix runs after a skipped original, so when another mod's prefix on `Item.InvokeAction` returns false and skips the game's action, `Used` can still fire for a use the game did not apply. The facade's prefix sets the state the postfix reads, so the event fires only if that prefix ran. The prefix order was not checked.

`Used` versus `Survival.Consumables.Consumed`: for food and drink use `Consumed`. It fires only after the game applied the effects, not when an intercept cancelled the use, and it also covers eating from the world and depression-forced uses. `Used` fires for every held item use (equip, read, tools too) before any outcome is known, and not for world use.

`Spawn.Item` never raises `Dropped`, even when called from a handler that runs inside a drop.

Save load and scene spawn never raise these events. The game does not call the hooked methods on load.

## World item ids

- `WorldItems.IdOf(GameObject item)` and `WorldItems.IdOf(Component item)` return the id of a world collectible, or `null` when the object is not a `Collectible`, or when it is a dropped item restored from the save that is no longer in `DestructibleList`.
- A collectible with a game GUID (`Collectible.GUID`, set on scene-placed items) uses that GUID. It is the same on every machine that loaded the same scene file.
- A dropped item restored from the save uses `<scene name>/DestructibleList<n>`, where `n` is its index in `DestructibleList.prefabs`. The game restores those entries from the save file in file order, so two machines that loaded the same save give the same id to the same item. A dropped item made in this session does not use this form.
- Any other collectible without a GUID (a scene-placed item, or a dropped item made in this session) uses `<scene name>/path:<name>[<index>]/<child name>[<index>]/...`. The path runs from the scene root to the object, and each `[<index>]` is the object's sibling index. The id does not depend on the order in which the game adds items to `DestructibleList`.
- List indexes shift at once when any dropped item is removed, because the removal takes its entry out of `DestructibleList`. The save also prunes removed entries. A path id changes the same way when an earlier sibling is removed or moved. So an id can name another item after any pickup, `Remove` or save. Fetch a fresh id after any removal. Co-op: a path id, and a `DestructibleList` id after any removal, is not a cross-peer key, and no existing save data gives a stable one for a dropped item. The host should issue its own id when an item is dropped, keep the map in the co-op layer (not in a save entry), and send that id. Use `Find` on it at the moment of use, and drop the host's map entry when the item is picked up or removed.
- `WorldItems.Find(string id)` returns the item's `GameObject`, or `null` when no active collectible in the active scene has that id. A path id is resolved by walking the scene's root objects down the path, and the object at the end must have a `Collectible`. An object whose name contains `/` cannot be found by a path id.
- `WorldItems.Remove(string id)` removes the item the way a pickup does, through `Collectible.Destroy`: a GUID item is recorded as destroyed, and a dropped item leaves `DestructibleList`. Nothing is given to the player, and no sound or pickup relay runs. It returns `true` when the item was removed, and `false` when there is no such item or the game refuses the removal. It does not raise `PickedUp`.
- `Remove` returns `false` while a save loads or saves (`SaveData.Busy`).
- `Spawn.Item(int itemId, int count, Vector3 position, out GameObject spawned)` sets `spawned` to the first object it creates, or `null`. It returns the same `bool` as the three-argument overload, which is unchanged.

## Expiry default

```csharp
public static int ExpiryMinutes { get; }
public static bool SetExpiryOverride(int minutes);
public static bool SetExpiryOverride(string owner, int minutes);
public static void ClearExpiryOverride();
public static bool ClearExpiryOverride(string owner);
```

- `ExpiryMinutes` is the level's default item timer in minutes (`info_game_logic.itemExpirationTimeMinutes`). It ignores the override. It is `-1` when there is no level logic object, or when the read fails (logged once).
- `SetExpiryOverride(minutes)` sets the timer for new items that the game starts without an explicit time. Each gets a random time between 0.5 and 1.5 times `minutes`, so `minutes` is an average and not an exact time. A `minutes` below 1 means those items never expire. A `minutes` above 21,474,836 is refused: it returns `false`, logs once, and keeps the current override, because the game's timer would overflow. It returns `false` when its hook is not installed. It works inside `Events.SceneLoaded`. A call made while a save loads can be dropped by the clears below.
- `ClearExpiryOverride()` puts back the game's default. It works at any time, including inside `Events.SceneLoaded`.
- Several mods: each `owner` (your mod id) holds its own value. The newest `SetExpiryOverride` wins. `ClearExpiryOverride(owner)` removes only that owner's value, and the previous owner's value applies again. It returns `false` when the owner had no value. The overloads without `owner` share one legacy slot. An invalid `owner` throws `ArgumentException`.
- Every override is cleared when a save starts loading (`SaveController.PlayerWillLoadGame`) and on each single scene load (a level change or the main menu). Set it again in your `Events.SceneLoaded` handler, which runs after the clear, or in `Events.WhenReady`, which runs after the load. A call made while a save loads can be dropped by these clears.
- Affected: the game's default-time calls on new items. These are items spawned by the game's drop and spawn code (`ItemOperations.SetCollectibleItemValues`, which `Spawn.Item` also uses, and coins), perished items that spawn a replacement, and spawner items when the spawner has `expireItems`. Those spawns also run during a load, so the override applies there too.
- Not affected: items with an explicit time, such as an expiration zone, and items the game spawns with no timer (`noExpirationTimer`, used for crime and animal drops). Timers already running are not restarted, because the game only makes default-time calls on new items.
- The override is kept in memory only. It is lost on restart.
- The hook has its own owner, `obenseuer.moddingframework.worlditems.expiry`. If it fails, the events are not affected and `SetExpiryOverride` returns `false`.
- The patch never throws into the game. A failing prefix is logged once and the game's default is used.

## If a hook cannot be installed

The framework installs six hooks under four group owners, `obenseuer.moddingframework.worlditems.pickup`, `.drop`, `.used` and `.saved`, one per group below:

- `CollectibleItem.PerformPickup` postfix (`PickedUp`).
- `ItemData.DropItem(int, bool)` prefix/finalizer, which marks a player drop in progress.
- `ItemOperations.SpawnItemToPosition` postfix, which raises `Dropped` only while a drop is in progress.
- `Item.InvokeAction(ItemData, Item.ItemAction, bool)` prefix/postfix (`Used`).
- `Item.InvokeActions(ItemData, bool)` prefix/finalizer/postfix (`Used`, once per liquid use). While it runs, the nested `InvokeAction` calls raise nothing.
- `DestructibleList.OnLoadingGameDestructibleList()` postfix, which records the dropped items that the save restored. `IdOf` uses that record to choose the id form.

If a hook is missing, only its group owner is removed, and one warning is logged. `WorldItems.IsAvailable` is then false. Each event needs only its own group: `PickedUp` needs `.pickup`, `Dropped` needs `.drop`, and `Used` needs `.used`. `IdOf` returns null for a restored destructible item only when `.saved` is missing. The other groups keep firing. Other facades are not affected. The expiry default hook (`Collectible.StartExpirationTimer(int?)` prefix) is installed separately under `obenseuer.moddingframework.worlditems.expiry`, so its failure does not touch these events.

## Threading and safety

- Main thread only.
- `WorldItems.IsAvailable` is false when any of the four core groups (`.pickup`, `.drop`, `.used`, `.saved`) was not installed or the owner was disabled.
- With no subscribers, the patches return before any allocation.
- Subscriber exceptions are caught per handler. A postfix exception is logged once and never reaches the game.
- Nothing is cancelable. The events are observers only.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
