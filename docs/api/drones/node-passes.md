---
title: Node passes
parent: Drones
grand_parent: Objects and devices
nav_order: 4
---

# Node passes

`NodePassed` tells your mod each time a drone reaches an air node.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Drones</code> <span>drone events</span></p>

Add a handler to `NodePassed`. Every handler gets the same args object, so read it and change nothing.

<div class="api-pair" markdown="1">

```csharp
public static class Drones
{
    public static event Action<DroneNodePassedArgs> NodePassed;
}
```

| Event | Gives you | When it fires |
|---|---|---|
| `NodePassed` | `DroneNodePassedArgs` | A drone reached an air node. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DroneNodePassedArgs</code> <span>what you get when a drone passes a node</span></p>

`NodePassed` gives you one of these for each node a drone reaches. `IsRouteEnd` marks the last node of a route, and `DroneId` can be `null`, so check it before you use it.

<div class="api-pair" markdown="1">

```csharp
public sealed class DroneNodePassedArgs
{
    public string NodeId { get; }
    public string DroneId { get; }
    public bool IsRouteEnd { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `NodeId` | `string` | The id of the node that was reached. |
| `DroneId` | `string` | The id of the drone that reached it. `null` when the framework cannot tell. |
| `IsRouteEnd` | `bool` | `true` when the node has no next node, so this is the end of the route. |

</div>
</div>

## Things to know

- `NodePassed` fires once for each node a drone reaches. Every handler receives the same `DroneNodePassedArgs` object. It fires just before the game runs that node's own outputs and moves the drone on. <span class="tech">(It is raised at the start of `Air_Node.Pass`, from a Harmony prefix (a change that runs before the game method), inside `Drone.Update`. At that moment the drone's `targetNode` is still the node it just reached.)</span>
- To stop the drone from your `NodePassed` handler, call `SetActive(id, false)`. That does work, and the drone stops on its next update.
- `DroneId` is the drone that reached the node. The game's pass call does not say which drone it was, so the framework notes which drone is updating and reads that note. <span class="tech">(`Air_Node.Pass` takes no drone. A prefix on `Drone.Update` stores the running drone in a static field, and `Drone.Update` is the only caller of `Pass` in the game code.)</span>
- `IsRouteEnd` is `true` at the last node of a route. If the drone is set to switch off at the end of its path, `Changed` fires too, a moment later. <span class="tech">(It is true when `Air_Node.NextNode` is null. The game then turns the drone off when `Drone.deActivateOnPathEnd` is set.)</span>

## Limits

- A new target set from inside a `NodePassed` handler is overwritten straight away. The game then picks the drone's next target itself, or switches the drone off at the end of its route. <span class="tech">(The game moves `targetNode` to the next node, or turns the drone off at the route end, after the handlers run. Because of that, `Drones.Get` read inside the handler still shows the node that was just passed as the target.)</span>
- `DroneId` is null when the pass did not come from a drone's own update, or when that drone is not heading for this node. The game never does this by itself, but a scene output or a `SendMessage` call can trigger a pass by name. <span class="tech">(`DroneId` is null when `Pass` runs outside a drone's `Update`, or when the stored drone's `targetNode` is not this node.)</span>
