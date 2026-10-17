---
title: Resource nodes
parent: Items and crafting
grand_parent: API reference
nav_order: 13
---

# Resource nodes

This page uses `Vector3` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Status: experimental.

## What it is

A resource node is something in the world that the player hits with a tool to get items, such as a tree or a rock. It holds a limited stock of items, gives one up every few hits, and refills over time. This API lets your mod list the nodes in the scene you are in, read how much stock each one has and which items it gives, change the stock, and get told when a node is hit or runs out.

The mining skill the player gains from hitting a node is on the [player page](player/skills.md#skills). This API does not read or change it.

## When to use it

- Find the resource nodes near a place, for example to put a marker on them.
- Refill a node, or empty it, from your mod.
- React when the player hits a node, or takes the last item from it.

## Quick example

This mod writes a log line each time the player hits a resource node, and another when a node runs out.

```csharp
using ObenseuerCore;

[ModInfo("example.nodewatch", "Node Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NodeWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        ResourceNodes.Hit += hit => Log.Info("Node " + hit.NodeId + " was hit, " + hit.Stock + " left" + (hit.Yielded ? ", and it gave an item." : "."));
        ResourceNodes.Depleted += depleted => Log.Info("Node " + depleted.NodeId + " is empty.");
    }
}
```

`Hit` and `Depleted` are events: the framework calls your code each time they happen. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

List every node in the scene with its stock.

```csharp
foreach (ResourceNodeInfo node in ResourceNodes.All())
    Log.Info(node.Id + ": " + node.Stock + " of " + node.MaxStock);
```

Find the nodes within 10 metres of a place.

```csharp
void ShowNear(UnityEngine.Vector3 position)
{
    foreach (ResourceNodeInfo node in ResourceNodes.Near(position, 10f))
        Log.Info(node.Id + " is " + UnityEngine.Vector3.Distance(position, node.Position) + " metres away.");
}
```

Refill a node to its full stock. `SetStock` stops at the node's largest stock, so a big number is safe.

```csharp
foreach (ResourceNodeInfo node in ResourceNodes.All())
    ResourceNodes.SetStock(node.Id, node.MaxStock);
```

## Reference

This is the full list of what the API gives you.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ResourceNodes</code> <span>list, read and refill resource nodes</span></p>

Use it to list the nodes in the scene, find the ones near a place, read or refill one, and hear when a node is hit or runs out.

<div class="api-pair" markdown="1">

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
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<ResourceNodeInfo>` | Every node in the scene you are in. |
| `Near(position, radius)` | `List<ResourceNodeInfo>` | The nodes within `radius` metres of a position, including those exactly `radius` away. |
| `Get(id)` | `ResourceNodeInfo` | The node with this id. `null` when nobody has it. |
| `SetStock(id, stock)` | `bool` | Sets how many items the node has left. `true` when it was set. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the `Hit` and `Depleted` events could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Hit` | `ResourceHitArgs` | A node was hit. |
| `Depleted` | `ResourceDepletedArgs` | A node ran out. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ResourceNodeInfo</code> <span>a snapshot of one node</span></p>

`ResourceNodes.All()`, `Near` and `Get` give you these. Pass the `Id` to `Get` or `SetStock`.

<div class="api-pair" markdown="1">

```csharp
public sealed class ResourceNodeInfo
{
    public string Id { get; }
    public Vector3 Position { get; }
    public List<int> ItemIds { get; }
    public int Stock { get; }
    public int MaxStock { get; }
    public int OwnerId { get; }
    public bool PermissionToTake { get; }
    public bool RespawnEnabled { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The node's id. Pass it to `Get` and `SetStock`. |
| `Position` | `Vector3` | Where the node is, in world coordinates. |
| `ItemIds` | `List<int>` | The numbers of the items the node can give. |
| `Stock` | `int` | How many items it has left now. |
| `MaxStock` | `int` | The most it holds. It refills up to this. |
| `OwnerId` | `int` | The number of the character who owns it. `0` when nobody does. |
| `PermissionToTake` | `bool` | `true` when items taken from it count as the owner letting you take them. |
| `RespawnEnabled` | `bool` | `true` when the node refills over time. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ResourceHitArgs</code> <span>what Hit tells you</span></p>

`ResourceNodes.Hit` hands you one of these after each hit. `Yielded` says whether that hit gave the player an item.

<div class="api-pair" markdown="1">

```csharp
public sealed class ResourceHitArgs
{
    public string NodeId { get; }
    public int Stock { get; }
    public bool Yielded { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `NodeId` | `string` | The id of the node that was hit. |
| `Stock` | `int` | How many items it has left after the hit. |
| `Yielded` | `bool` | `true` when this hit gave the player an item. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ResourceDepletedArgs</code> <span>what Depleted tells you</span></p>

`ResourceNodes.Depleted` hands you one of these when a hit takes the last item, with the id of the node that ran out.

<div class="api-pair" markdown="1">

```csharp
public sealed class ResourceDepletedArgs
{
    public string NodeId { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `NodeId` | `string` | The id of the node that ran out. |

</div>
</div>

### Things to know

- `All()` lists the resource nodes that are switched on in the scene you are in, and it is slow, so do not call it every frame. It searches every object in the scene each time. Call it when something happens, or on a timer, and keep the result. `Near` and `Get` search the same way. To follow hits, use `Hit` instead of asking again and again. <span class="tech">(They search the active objects of the active scene for `WorkableResourceSource`, which is O(nodes) per call.)</span>
- A node id is a piece of text that names one node. `All()`, `Near` and `Get` give it to you. It is stable as long as the scene does not change. A null or empty id finds nothing. <span class="tech">(The id is the node's save GUID when it has one, otherwise its scene path, such as `Scene:Root[0]/Child[2]/Rock[1]`.)</span>
- `Near(position, radius)` lists the nodes up to `radius` metres from `position`, including those exactly `radius` away.
- `Get(id)` gives you a snapshot, not a live view. The values do not change by themselves, so call `Get` again for fresh values. It gives `null` when no node has that id.
- `ItemIds` are the plain item numbers used everywhere in the framework. A node picks one of these items at random each time it gives something up. <span class="tech">(They are `ItemReference.ID` values. The reference's `itemnamespace` is not given.)</span>
- `SetStock(id, stock)` sets how many items the node has left. A number above `MaxStock` is lowered to `MaxStock`. The value is saved with the game. <span class="tech">(It writes the game's own `currentStock` field.)</span>
- `SetStock` also does what the game does when a node's stock changes. Anything wired to the node follows the new value, the node starts or stops its refill timer depending on whether it is below its largest stock, and its collider is refreshed. It does not reset how much damage the node has taken toward its next item, and it does not fire `Hit` or `Depleted`, because nothing was hit. <span class="tech">(It triggers the node's `onStock` and `onCurrentStock` relay outputs, calls `ChangeDeltaSecondsStatus(stock < maximumStock)` and then `UpdateState`.)</span>
- `Hit` fires when the game accepts a hit on a node that still has stock. A hit that does not make the node give up an item yet still fires, with `Yielded` set to `false`. <span class="tech">(It is raised after `WorkableResourceSource.Damage` when the stock before the call was at least 1 and `CheckDamageType` accepted the damage type. `Yielded` is true when the stock went down.)</span>
- `Depleted` fires once, when a hit takes the last item. It fires right after `Hit` for the same hit. It does not fire again until the node has refilled and run out again, and it does not fire when your mod empties a node with `SetStock`.
- `IsAvailable` tells you whether `Hit` and `Depleted` work. The reads and `SetStock` can still work.

### Limits

- `Near` throws an `ArgumentException` for a negative radius, or a number that is not a real number. A negative `stock` given to `SetStock` throws an `ArgumentException`.
- `SetStock` returns `false` when no node has that id, and while a save is loading or being written.
- A hit of a kind the node does not accept, or a hit on a node that is already empty, does not fire `Hit`.
- You can't watch a node refill, and you can't change its largest stock, its items or its owner. The node refills on the game's own timer, so there is no event for it. You can only see from `RespawnEnabled` that it does. `MaxStock`, `ItemIds`, `OwnerId` and `PermissionToTake` are read only. <span class="tech">(Respawn is driven by `WorkableResourceSource.DeltaSeconds` and `SpawnSettings.TimeToSpawn`.)</span>
- Other things you may mistake for resource nodes are not covered. The game's extractables have no stock, owner or item list. They spawn objects and keep a health value, so they are a different system.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Files: `src/Api/ResourceNodes.cs`, `src/Api/ResourceNodesPatches.cs`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes some of the game's methods when it starts, so it can tell your mod when a node is hit or runs out. All of those changes belong to one feature group, `obenseuer.moddingframework.resources`, the name you will see in the log.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for every member on this page, and for the code that handles `Hit` and `Depleted`.
- It is safe to call any of it at any time, even on the main menu or while a save is loading. If there is nothing to read yet, you get an empty list or `null`, and `SetStock` gives `false`.
- Your event handlers run inside the game's own call, right after the hit. If one of your handlers throws an error, the framework logs it and still runs the other handlers.
- The framework does not hold back `Hit` and `Depleted` while a save is loading or being written. A handler that should ignore those moments can check `SaveData.Busy`, which is `true` during a load or a save.

### What to check for and what comes back

- Only two calls throw an error for a bad value. `SetStock` throws an `ArgumentException` for a negative `stock`, and `Near` throws one for a negative or not-a-number `radius`.
- If the game itself fails while the framework works, you get an empty answer instead of an error. The answer is the plain default (an empty list, `null` or `false`), and the first failure of each member goes to the log as an error. Later failures of that member are not logged again.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the node events can't be set up, only `Hit` and `Depleted` stop. The framework checks three private fields on the node (`currentStock`, `maximumStock`, `outputItems`) and changes the game's `WorkableResourceSource.Damage`. If any of that fails, the feature group `obenseuer.moddingframework.resources` turns off, error lines and a warning go to the log, and `IsAvailable` is `false`. <span class="tech">(The reads use the same three fields, so a missing field makes them return empty results as well.)</span>
