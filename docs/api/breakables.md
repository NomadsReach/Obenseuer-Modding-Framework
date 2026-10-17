---
title: Breakables
parent: Objects and devices
grand_parent: API reference
has_children: true
nav_order: 2
---

# Breakables

## What it is

Some objects in the game have a strength and break when it runs out. Locks can be broken the same way. Other objects spawn an item when a hit destroys them, which this page calls smashing. This API lets your mod list these objects, break or repair them, and hear when one breaks, is repaired or is smashed.

## When to use it

- Write a log line or show a message when a player breaks something.
- Break a breakable or a lock from your mod, for example as part of a scripted event.
- Repair a broken breakable that has a repair timer.
- Count what is broken in the scene the player is in.
- Find out what a smashed object would have dropped, and where it was.

## Quick example

This mod writes a log line each time a breakable breaks, is repaired, has its lock broken, or is smashed.

```csharp
using ObenseuerCore;

[ModInfo("example.breakwatch", "Break Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class BreakWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Breakables.Broken += args => Log.Info("Broken: " + args.Id);
        Breakables.Repaired += args => Log.Info("Repaired: " + args.Id);
        Breakables.LockBroken += args => Log.Info("Lock broken: " + args.Id);
        Breakables.Smashed += args => Log.Info("Smashed at " + args.Position + ", animal: " + args.IsAnimal);
    }
}
```

Each of these is an event: the framework calls your code every time it happens. See [Using events](../guides/getting-started/using-events.md) for more on events.
