---
title: Trains
parent: Places and travel
grand_parent: API reference
has_children: true
nav_order: 17
---

# Trains

Status: experimental.

## What it is

The game has trains that run on rails. This API lets your mod look at them: every train on the track with its speed, position, wagon type and cargo, the small train the player can drive (the minitrain) and its fuel, the track switches and wagon loaders, and the pieces of track and which ones are taken. It lets you flip a track switch and turn a wagon loader on or off. It also tells your mod when a train sets off, stops or crashes.

To drive a train (engine, speed, brakes, coupling wagons), see [Train actions](trainactions.md). To ask whether the player is driving the minitrain right now, use `Travel.UsingMinitrain` on the [player page](player.md). This page does not repeat it.

## When to use it

- Show or log where the trains are and how fast they go.
- React when a train sets off, stops or crashes.
- Read the fuel left in the minitrain.
- Send a train down another track by flipping a switch.
- Find out which pieces of track a train is on.

## Quick example

This mod writes a log line when a train sets off, stops or crashes. Once a save is loaded, it also logs the minitrain's fuel.

```csharp
using ObenseuerCore;

[ModInfo("example.trainwatch", "Train Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TrainWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Trains.Departed += args => Log.Info("Train " + args.Id + " set off at speed " + args.Speed);
        Trains.Arrived += args => Log.Info("Train " + args.Id + " stopped.");
        Trains.Crashed += args => Log.Info("Train " + args.Id + " crashed at speed " + args.Speed);
        Events.WhenReady(() =>
        {
            MinitrainInfo minitrain = Trains.Minitrain;
            if (minitrain != null) Log.Info("Minitrain fuel: " + minitrain.Fuel + " of " + minitrain.Capacity);
        });
    }
}
```

`Departed`, `Arrived` and `Crashed` are events: the framework calls your code each time they happen. `Events.WhenReady` runs your code once a save is loaded, or at once if one already is. See [Using events](../guides/getting-started/using-events.md).
