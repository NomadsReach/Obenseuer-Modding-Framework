---
title: Levels
parent: Places and travel
grand_parent: API reference
has_children: true
nav_order: 8
---

# Levels

## What it is

The game world is split into levels, and the player moves from one to another through level changers: doors with a lock, other use points, and invisible areas that take the player to the next level when they walk in. This API lists the level changers in the scene you are in, and tells you where each one leads. It can lock and unlock doors, use a changer for the player, and move the player to any level by name. It also tells you when the game is about to change level.

## When to use it

- Lock a door to another level until a quest step, and unlock it afterwards.
- Take the player to another level from your own code, for example from a menu.
- React when the player is about to leave the level, for example to save your mod's own data.

## Quick example

This mod logs where the player is going each time the game changes level. Once a save is loaded, it also logs every level changer in the scene with its destination and whether it is locked.

```csharp
using ObenseuerCore;

[ModInfo("example.levelwatch", "Level Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class LevelWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Levels.Changing += args => Log.Info("Changing level to " + args.OtherLevel + " at " + args.OtherEntrypoint);
        Events.WhenReady(() =>
        {
            foreach (LevelDoorInfo door in Levels.All())
                Log.Info(door.Id + " leads to " + door.OtherLevel + ", locked=" + door.Locked);
        });
    }
}
```

`Changing` is an event: the framework calls your code each time it happens. `WhenReady` runs your code once the game is ready. See [Using events](../guides/getting-started/using-events.md) for more on both.
