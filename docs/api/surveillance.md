---
title: Surveillance
parent: Law and crime
grand_parent: API reference
has_children: true
nav_order: 3
---

# Surveillance

Status: experimental.

## What it is

Surveillance is everything in the game that can watch the player: security cameras, people who can see a crime, and areas where the player is not allowed to be (trespassing areas). This API lets your mod list the cameras, switch one on or off, ask whether someone could see the player right now, ask whether the player is trespassing, and get told when a camera or a person spots the player or when the player enters or leaves a trespassing area. Crime records, fines and arrests are on the [Law page](../api-story/law-experimental-for-arrest.md#law-experimental-for-arrest). Police chases are on the [Pursuit](pursuit.md) page.

## When to use it

- Switch a security camera off as part of a heist or a puzzle.
- Check whether a crime would be seen before your mod lets the player do it.
- React when a camera or a person spots the player.
- Do something when the player walks into or out of a place they should not be.

## Quick example

This mod writes a log line when a camera or a person spots the player, and when the player enters or leaves a trespassing area. Once a save is loaded, it also logs how many cameras the scene has.

```csharp
using ObenseuerCore;

[ModInfo("example.eyeswatch", "Eyes Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class EyesWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Surveillance.PlayerSpotted += args => Log.Info("Spotted by a " + args.Source + ": " + args.Id);
        Surveillance.TrespassEntered += args => Log.Info("You are trespassing in " + args.AreaId);
        Surveillance.TrespassLeft += args => Log.Info("You left " + args.AreaId);
        Events.WhenReady(() =>
        {
            Log.Info("This scene has " + Surveillance.Cameras().Count + " cameras.");
        });
    }
}
```

`PlayerSpotted`, `TrespassEntered` and `TrespassLeft` are events: the framework calls your code each time they happen. `Events.WhenReady` waits until a save is loaded before it runs your code. See [Using events](../guides/getting-started/using-events.md).
