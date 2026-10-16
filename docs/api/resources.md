---
title: Resource nodes API
parent: API reference
nav_order: 63
---

# Resource nodes API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.resources`.
Files: `src/Api/ResourceNodes.cs`, `src/Api/ResourceNodesPatches.cs`.

A resource node is a `WorkableResourceSource`: a tree, rock or similar that gives items when it is hit with a tool and refills over time. The mining skill and its gain stay in `Skills` (`src/Api/Skills.cs`). This area does not add skill reads or skill writes.

## If a hook cannot be installed

The framework checks the three private stock fields and installs one hook, `WorkableResourceSource.Damage`. If a field is missing or the hook fails, the owner is disabled and one warning is logged. The reads keep working through the same fields, so a missing field makes them return empty results.

## Threading and safety

- Main thread only.
- `ResourceNodes.IsAvailable` is false when the hook was not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list or `null`, and writes return `false`.
- Nothing throws except `ArgumentException` for a negative `stock` or a negative `radius` (or NaN).
- Events are raised synchronously from the game's own `Damage` call. A handler that throws is logged and the other handlers still run.

## API

```csharp
public static class ResourceNodes
{
    public static List<ResourceNodeInfo> All();
    public static List<ResourceNodeInfo> Near(Vector3 position, float radius);
    public static ResourceNodeInfo Get(string id);
    public static bool SetStock(string id, int stock);

    public static bool IsAvailable { get; }
    public static event Action<ResourceHitArgs> Hit;
    public static event Action<ResourceDepletedArgs> Depleted;
}

public sealed class ResourceNodeInfo
{
    public string Id { get; }                  // save GUID, or the scene path
    public Vector3 Position { get; }           // world position
    public List<int> ItemIds { get; }          // item ids the node can give (ItemReference.ID)
    public int Stock { get; }                  // items left now
    public int MaxStock { get; }               // the node refills up to this
    public int OwnerId { get; }                // NPC id of the owner, 0 when none
    public bool PermissionToTake { get; }      // a taken item carries the owner permission
    public bool RespawnEnabled { get; }        // the node refills over time
}

public sealed class ResourceHitArgs
{
    public string NodeId { get; }
    public int Stock { get; }                  // stock after the hit
    public bool Yielded { get; }               // true when this hit gave an item
}

public sealed class ResourceDepletedArgs
{
    public string NodeId { get; }
}
```

- `All` lists every resource node in the active scene.
- `Near` lists the nodes whose position is within `radius` of `position`, inclusive. It throws `ArgumentException` for a negative or NaN radius.
- `All`, `Near` and `Get` scan the active scene with `FindObjectsByType` on every call (O(nodes)); do not call them every frame, cache the result or use `Hit`.
- Item ids are the plain `ItemReference.ID` ints, as everywhere in the framework. The reference's `itemnamespace` is not exposed.
- `Get(id)` returns one `ResourceNodeInfo`, or `null` when no node has that id.
- `SetStock(id, stock)` sets the node's stock. A `stock` above `MaxStock` is clamped to `MaxStock`. It returns `false` when no node has that id, and while a save loads or saves (`SaveData.Busy`). It throws `ArgumentException` for a negative `stock`.
- `Hit` fires after the game accepts a hit on a node that has stock (the game's `Damage` passed the tool check). `Yielded` is true when the hit gave an item, and `Stock` is the stock after the hit. A hit that does not yield (the node is not finished yet) still fires.
- `Depleted` fires once, when a hit takes the stock to `0`. It fires after `Hit` for the same hit.

## What SetStock changes

`SetStock` is the game's own stock field, `currentStock`. That field is saved, so the new value is kept when the game saves. The method also does what the game does when the stock changes:

- It fires the node's `onStock` and `onCurrentStock` relays, so relay-driven visuals and scripts follow the new value.
- It starts or stops the node's respawn timer (`ChangeDeltaSecondsStatus`) depending on whether the stock is below `MaxStock`.
- It refreshes the node's collider (`UpdateState`).

`SetStock` does not raise `Hit` or `Depleted`, because nothing was hit. It does not reset the node's damage progress toward the next item.

## Not wrapped

- `Extractable`. It has no stock, owner or item id. It spawns prefabs and tracks health, so it is a different system.
- The respawn event. Respawn is driven by the game's own timer (`WorkableResourceSource.DeltaSeconds`, `SpawnSettings.TimeToSpawn`). Only the `RespawnEnabled` flag is exposed.
- The mining skill gain on a hit. It belongs to `Skills`.
- Writes to `MaxStock`, the item list, the owner or the permission. `OwnerId`, `PermissionToTake` and `ItemIds` are read-only snapshots.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
