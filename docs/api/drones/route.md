---
title: Route
parent: Drones
grand_parent: Objects and devices
nav_order: 5
---

# Route

`SetTarget` and `SetMaxSpeed` change how a drone moves.

- **`SetTarget(id, nodeId)` points a drone at an air node but does not start it.** A drone that is off keeps the new target until you call `SetActive(id, true)`. It returns `true` when the drone's target is now that node. It returns `false` for an unknown drone, an unknown node id, an empty id, or while a save is loading or being written.
  - Technical: it finds the drone and the node, then calls `Drone.SetTarget`, which only assigns `targetNode`.
- **You can't clear a drone's target.** Passing a null node is refused. A drone with no target switches itself off on its next update, so clearing would stop the drone as a side effect, and the framework does not start that.
  - Technical: with `deActivateOnPathEnd` set (the default), a null `targetNode` turns the drone off in `Update`.
- **`SetMaxSpeed(id, maxSpeed)` sets how fast the drone may fly, and it must be above 0.** It returns `false` for zero, a negative number, a value that is not a real number, an infinite value, an unknown drone, or while a save is loading or being written. A zero speed would break the drone's movement, so it is always refused. A target node can override your value: if the node has its own top speed above 0, that one wins.
  - Technical: it calls `Drone.SetMaxSpeed`, which assigns the public `maxSpeed`. A zero would divide by zero in `Update`.
- **The target is saved, and the top speed should be.** The game saves a drone's target as the node's save id, and finds the node again when the save loads. A node with no save id is saved as nothing and loads with no target.
  - Technical: the target is saved through `targetNodeRef`, which stores `Air_Node.GUID` and resolves it on load through `Air_Node.GetNode`. `maxSpeed` is a public field without `[fsIgnore]`, so the save code is expected to keep it. That has not been confirmed.
