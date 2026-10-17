---
title: Interaction and locks
parent: Places and travel
grand_parent: API reference
has_children: true
nav_order: 5
---

# Interaction and locks

## What it is

When the player looks at a door, a switch or a chest and gets a prompt like "Open (Use)", that is an interaction. This API lets your mod give any object its own prompt, hear when the player uses something, and see what the player is looking at. It also covers locks. Your mod can check whether a door, chest or keypad is locked, lock and unlock it, change how hard it is to pick, start the lockpicking minigame, and get told when a lock changes or a pick succeeds or fails.

## When to use it

- Make a prop in your area usable, for example a button that runs your code, and block it with a red reason such as "Too hungry".
- React when the player uses anything in the game, or when they pick a lock.
- Lock or unlock doors and chests from your mod, or make them easier or harder to pick.
- Open a keypad, or change the code of one.

## Quick example

This mod writes a log line each time the player uses something, and another each time they pick a lock or fail to.

```csharp
using ObenseuerCore;

[ModInfo("example.usewatch", "Use Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class UseWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Interactions.Used += use => Log.Info("The player used " + use.Name + " (" + use.Kind + ") from " + use.Distance + " metres");
        Locks.LockpickSucceeded += pick => Log.Info("The player picked the lock on " + pick.Name);
        Locks.LockpickFailed += pick => Log.Info("The player failed to pick the lock on " + pick.Name);
    }
}
```

`Used`, `LockpickSucceeded` and `LockpickFailed` are events: the framework calls your code each time they happen. See [Using events](../guides/getting-started/using-events.md) for more on events.
