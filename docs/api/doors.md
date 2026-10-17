---
title: Doors
parent: Places and travel
grand_parent: API reference
has_children: true
nav_order: 1
---

# Doors

## What it is

The doors in the game swing or slide open and shut, and some of them follow opening hours, like a shop door that is locked at night. This API lets your mod list the doors in the scene you are in, open or close one, read or change a door's opening hours, and get told when a door has finished opening or closing.

## When to use it

- Open a door for the player, or close one behind them.
- Check whether a door is open before your mod acts.
- Change the hours a shop door is open.
- Run code when a door has finished opening or closing.
- Find where an NPC stands while it waits for a door.

## Quick example

This mod writes a log line each time a door has fully opened or fully closed. Once a save is loaded, it also logs how many doors the scene has.

```csharp
using ObenseuerCore;

[ModInfo("example.doorwatch", "Door Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class DoorWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Doors.Opened += args => Log.Info("Door opened: " + Doors.Get(args.Id)?.Name);
        Doors.Closed += args => Log.Info("Door closed: " + Doors.Get(args.Id)?.Name);
        Events.WhenReady(() => Log.Info("This scene has " + Doors.All().Count + " doors."));
    }
}
```

`Opened` and `Closed` are events: the framework calls your code each time the thing happens. `Doors.Get(id)` gives the door's details, or `null` when the id is not known. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.
