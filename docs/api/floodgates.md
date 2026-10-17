---
title: Flood gates
parent: Objects and devices
grand_parent: API reference
has_children: true
nav_order: 6
---

# Flood gates

## What it is

A flood gate is a big powered gate that slides open or shut. It has a green light for open and a red light for closed, and it usually takes about ten seconds to move. This API lets your mod list the flood gates in the scene, check whether each one is open and has power, open or close one, switch its power on or off, and get told when one is commanded open or closed.

## When to use it

- Open or close a gate as part of a quest or a puzzle.
- Cut or restore a gate's power.
- React when the player or the game opens or closes a gate.

## Quick example

This mod writes a log line whenever a flood gate is told to open or close. Once a save is loaded, it also logs every gate in the scene and whether it is open and powered.

```csharp
using ObenseuerCore;

[ModInfo("example.gatewatch", "Gate Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class GateWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        FloodGates.Changed += args => Log.Info("Flood gate " + args.Id + (args.IsOpen ? " is opening." : " is closing."));
        Events.WhenReady(() =>
        {
            foreach (FloodGateInfo gate in FloodGates.All())
                Log.Info(gate.Name + " is " + (gate.IsOpen ? "open" : "closed") + (gate.IsPowered ? "" : " and has no power"));
        });
    }
}
```

`Changed` is an event: the framework calls your code each time it happens. `Events.WhenReady` waits until a save is loaded before it runs your code. See [Using events](../guides/getting-started/using-events.md).
