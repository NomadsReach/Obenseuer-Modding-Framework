# World item events API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.worlditems`.
Files: `src/Api/WorldItems.cs`, `src/Api/WorldItemsPatches.cs`.

Everything not listed here is internal. Item ids are the game's `int` `Item.ID`.

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
- `Used`: the player activates a held inventory item with the Use or secondary Use button. `ItemId` is the item whose action was dispatched. It is raised per dispatched action. A liquid container raises one event for each action of its liquid. A world item used directly does not fire. An active trade does not fire.

`Used` versus `Survival.Consumables.Consumed`: for food and drink use `Consumed`. It fires only after the game applied the effects, not when an intercept cancelled the use, and it also covers eating from the world and depression-forced uses. `Used` fires for every action of a held item (equip, read, tools too) before any outcome is known, and not for world use.

`Spawn.Item` never raises `Dropped`, even when called from a handler that runs inside a drop.

Save load and scene spawn never raise these events. The game does not call the hooked methods on load.

## Wiring

```csharp
internal static void WorldItems.Init();
```

Core calls this from `Core.Awake`.

`Init` installs four hooks under one owner, `obenseuer.moddingframework.worlditems`:

- `CollectibleItem.PerformPickup` postfix (`PickedUp`).
- `ItemData.DropItem(int, bool)` prefix/finalizer, which marks a player drop in progress.
- `ItemOperations.SpawnItemToPosition` postfix, which raises `Dropped` only while a drop is in progress.
- `Item.InvokeAction(ItemData, Item.ItemAction, bool)` prefix/postfix (`Used`).

If any hook is missing, every hook from this owner is removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.worlditems")` becomes true, and one warning is logged. Other facades are not affected.

## Threading and safety

- Main thread only.
- `WorldItems.IsAvailable` is false when the hooks were not installed or the owner was disabled.
- With no subscribers, the patches return before any allocation.
- Subscriber exceptions are caught per handler by `Events.Raise`. A postfix exception is logged once and never reaches the game.
- Nothing is cancelable. The events are observers only.

## Not tested in game

The hooks were checked for build and for member signatures against the real game DLL. They have not been run in game. See the in-game smoke test in the report.
