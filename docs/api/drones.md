---
title: Drones
parent: Objects and devices
grand_parent: API reference
has_children: true
nav_order: 5
---

# Drones

## What it is

Drones are machines in the game that follow a route. The route is a chain of air nodes, which are waypoints the drone moves toward one after another. This API lets your mod list the drones and air nodes in the scene you are in, switch a drone on or off, send it to a node, change its top speed, and get told when a drone changes state or passes a node.

## When to use it

- Start or stop a drone from your own code, for example when the player finishes a quest step.
- Send a drone to a different air node, then start it.
- Slow a drone down or speed it up.
- Log or react when a drone switches on, switches off, or passes a node.

## Quick example

This mod writes a log line whenever a drone is switched on or off, and another whenever a drone passes a node. When the game is ready, it also logs how many drones are in the scene.

```csharp
using ObenseuerCore;

[ModInfo("example.dronewatch", "Drone Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class DroneWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Drones.Changed += args => Log.Info("drone " + args.Id + " is now " + (args.Active ? "on" : "off"));
        Drones.NodePassed += args => Log.Info("drone " + args.DroneId + " passed node " + args.NodeId);
        Events.WhenReady(() => Log.Info(Drones.All().Count + " drones in this scene"));
    }
}
```

`Changed` and `NodePassed` are events: the framework calls your code each time they happen. `DroneId` is null when the framework cannot tell which drone passed the node, which is why a real mod should check it before using it. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.
