---
title: Common tasks
parent: Drones
grand_parent: Objects and devices
nav_order: 1
---

# Common tasks

Switch a drone on. `SetActive` returns true when the drone is in the state you asked for.

```csharp
if (Drones.SetActive("some-drone-id", true)) Log.Info("the drone is running");
```

Send a drone to an air node and then start it. A drone that is off keeps its new target until you start it.

```csharp
if (Drones.SetTarget("some-drone-id", "some-node-id")) Drones.SetActive("some-drone-id", true);
```

Set a drone's top speed. The value must be above 0.

```csharp
Drones.SetMaxSpeed("some-drone-id", 5f);
```

Read where a drone is and where it is heading.

```csharp
DroneInfo drone = Drones.Get("some-drone-id");
if (drone != null) Log.Info(drone.Position + " heading for " + drone.TargetNodeId + ", then " + drone.NextNodeId);
```

List the air nodes and the nodes each one links to.

```csharp
foreach (AirNodeInfo node in Drones.Nodes())
    Log.Info(node.Id + " links to " + node.LinkedNodeIds.Count + " nodes");
```
