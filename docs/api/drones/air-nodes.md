---
title: Air nodes
parent: Drones
grand_parent: Objects and devices
nav_order: 3
---

# Air nodes

An air node is a waypoint on a drone's route. `Drones.Nodes()` lists them so your mod can find a node id to send a drone to.

```csharp
public static class Drones
{
    public static List<AirNodeInfo> Nodes();   // every air node in the scene you are in
}

public sealed class AirNodeInfo    // a snapshot of one air node
{
    public string Id { get; }                   // names this node; the same kind of id as DroneInfo.Id
    public Vector3 Position { get; }            // where the node is, in world coordinates
    public List<string> LinkedNodeIds { get; }  // the ids of the nodes this one links to, in route order
}
```

- **`Nodes()` only lists the nodes in the scene you are in, and you can't change it.** A node in another scene is not listed.
  - Technical: it uses `FindObjectsByType`, so inactive objects are left out.
- **`LinkedNodeIds` lists every node this one links to, not just the one the drone will take next.** A node with no links gives an empty list.
  - Technical: it is the full `Air_Node.nextNodes` list, with empty entries skipped. The node the game picks next depends on `Air_Node.activeNodeIndex`.
- **The ids match the ones `DroneInfo.TargetNodeId` and `SetTarget` use.** You can take an id from `Nodes()` and pass it straight to `SetTarget`.
- **You can't change which nodes link to which.** The links are set when the map is built, so to move a drone along a different route, send it to a node with `SetTarget`.
  - Technical: `nextNodes` is set in the Unity editor, and the framework has no node-link writer.
