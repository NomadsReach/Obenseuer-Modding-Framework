---
title: Player movement
parent: Player life and skills
grand_parent: API reference
has_children: true
nav_order: 7
---

# Player movement

## What it is

This API is about how the player moves. You can read it: is the player on the ground, running or crouching, how fast are they going and where are they. You can change it: make the player walk, run or jump faster or slower, make them crouch or stand up, hold them in place for a scene, or lock where they look. Every change is tied to your mod's id, so two mods that change the same thing do not undo each other.

## When to use it

- Read how the player is moving, for example to show a message only when they are running.
- Make the player faster or slower for a while, or give them a higher jump.
- Hold the player still and turn the camera toward something during a scene.
- Stop the mouse look but let the player keep walking.
- Make the player crouch or stand up from your own code.

## Quick example

This mod waits for a save to load. Then it makes the player 1.5 times faster and logs what the game now says about their movement.

```csharp
using ObenseuerCore;

[ModInfo("example.speedy", "Speedy", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class Speedy : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            if (!PlayerMovement.SetSpeedMultiplier(Mod.Id, 1.5f)) Log.Warn("Could not change the speed.");
            PlayerMovementInfo state = PlayerMovement.State();
            if (state != null) Log.Info("Walk speed is now " + state.WalkSpeed + " and run speed is " + state.RunSpeed);
        });
    }
}
```

`Mod.Id` is your mod's own id. It is how the framework knows the change is yours, so you can take it back later with `PlayerMovement.ClearMultipliers(Mod.Id)`. `State()` returns `null` when no player is loaded, which is why the example checks for it. See [Using events](../guides/getting-started/using-events.md) for more on `WhenReady`.
