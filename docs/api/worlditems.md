---
title: World item events
parent: Items and crafting
grand_parent: API reference
has_children: true
nav_order: 15
---

# World item events

## What it is

A world item is something that lies in the world and can be picked up: a dropped apple, a tool on a shelf, a coin on the floor. This API lets your mod get told when the player picks one up, drops one, or uses an item they hold. It also gives each world item an id so you can find it again, lets you remove one from the world, and lets you change how long new items last before they spoil or vanish.

## When to use it

- React when the player picks up or drops a certain item.
- Count how often the player uses held items.
- Find a particular item lying in the scene, or remove it.
- Make items disappear sooner or later than the game normally does.

## Quick example

This mod writes a log line each time the player picks up or drops an item.

```csharp
using ObenseuerCore;

[ModInfo("example.itemwatch", "Item Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ItemWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        WorldItems.PickedUp += args => Log.Info("Picked up " + args.Amount + " of item " + args.ItemId + " at " + args.Position);
        WorldItems.Dropped += args => Log.Info("Dropped " + args.Amount + " of item " + args.ItemId + " at " + args.Position);
    }
}
```

`PickedUp` and `Dropped` are events: the framework calls your code each time they happen. See [Using events](../guides/getting-started/using-events.md) for more on events.
