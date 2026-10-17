---
title: Node passes
parent: Drones
grand_parent: Objects and devices
nav_order: 4
---

# Node passes

`NodePassed` tells your mod each time a drone reaches an air node.

```csharp
public static class Drones
{
    public static event Action<DroneNodePassedArgs> NodePassed;   // a drone reached an air node
}

public sealed class DroneNodePassedArgs    // what you get when a drone passes a node
{
    public string NodeId { get; }          // the id of the node that was reached
    public string DroneId { get; }         // the id of the drone that reached it; null when the framework cannot tell
    public bool IsRouteEnd { get; }        // true when the node has no next node, so this is the end of the route
}
```

- **`NodePassed` fires once for each node a drone reaches.** Every handler receives the same `DroneNodePassedArgs` object. It fires just before the game runs that node's own outputs and moves the drone on.
  - Technical: it is raised at the start of `Air_Node.Pass`, from a Harmony prefix (a change that runs before the game method), inside `Drone.Update`. At that moment the drone's `targetNode` is still the node it just reached.
- **A new target set from inside a `NodePassed` handler is overwritten straight away.** The game then picks the drone's next target itself, or switches the drone off at the end of its route. To stop the drone from your handler, call `SetActive(id, false)`: that does work, and the drone stops on its next update.
  - Technical: the game moves `targetNode` to the next node, or turns the drone off at the route end, after the handlers run. Because of that, `Drones.Get` read inside the handler still shows the node that was just passed as the target.
- **`DroneId` is the drone that reached the node.** The game's pass call does not say which drone it was, so the framework notes which drone is updating and reads that note. It is null if the pass did not come from a drone's own update, or if that drone is not heading for this node. The game never does this by itself, but a scene output or a `SendMessage` call can trigger a pass by name.
  - Technical: `Air_Node.Pass` takes no drone. A prefix on `Drone.Update` stores the running drone in a static field, and `Drone.Update` is the only caller of `Pass` in the game code. `DroneId` is null when `Pass` runs outside a drone's `Update`, or when the stored drone's `targetNode` is not this node.
- **`IsRouteEnd` is `true` at the last node of a route.** If the drone is set to switch off at the end of its path, `Changed` fires too, a moment later.
  - Technical: it is true when `Air_Node.NextNode` is null. The game then turns the drone off when `Drone.deActivateOnPathEnd` is set.
