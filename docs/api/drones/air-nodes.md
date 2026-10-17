---
title: Air nodes
parent: Drones
grand_parent: Objects and devices
nav_order: 3
---

# Air nodes

An air node is a waypoint on a drone's route. `Drones.Nodes()` lists them so your mod can find a node id to send a drone to.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Drones</code> <span>the air nodes in the scene</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Drones
{
    public static List<AirNodeInfo> Nodes();
}
```

| Method | Returns | What it does |
|---|---|---|
| `Nodes()` | `List<AirNodeInfo>` | Every air node in the scene you are in. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>AirNodeInfo</code> <span>a snapshot of one air node</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class AirNodeInfo
{
    public string Id { get; }
    public Vector3 Position { get; }
    public List<string> LinkedNodeIds { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | Names this node. It is the same kind of id as `DroneInfo.Id`. |
| `Position` | `Vector3` | Where the node is, in world coordinates. |
| `LinkedNodeIds` | `List<string>` | The ids of the nodes this one links to, in route order. |

</div>
</div>

## Things to know

- `Nodes()` only lists the nodes in the scene you are in, and you can't change that. A node in another scene is not listed. <span class="tech">(It uses `FindObjectsByType`, so inactive objects are left out.)</span>
- `LinkedNodeIds` lists every node this one links to, not just the one the drone will take next. A node with no links gives an empty list. <span class="tech">(It is the full `Air_Node.nextNodes` list, with empty entries skipped. The node the game picks next depends on `Air_Node.activeNodeIndex`.)</span>
- The ids match the ones `DroneInfo.TargetNodeId` and `SetTarget` use. You can take an id from `Nodes()` and pass it straight to `SetTarget`.

## Limits

- You can't change which nodes link to which. The links are set when the map is built, so to move a drone along a different route, send it to a node with `SetTarget`. <span class="tech">(`nextNodes` is set in the Unity editor, and the framework has no node-link writer.)</span>
