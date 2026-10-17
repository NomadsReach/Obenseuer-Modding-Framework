---
title: Framework core classes
parent: Framework basics
grand_parent: API reference
has_children: true
nav_order: 3
---

# Framework core classes

Status: experimental.

## What it is

This page covers the small building blocks that nearly every mod uses, plus a few bigger ones. They are not tied to one part of the game. You get a log file for your mod, a safe way to change game methods, events for the clock and for saving and loading, the game's date and time, the player's OC balance, a place to keep your mod's own save data, the player's needs (hunger, thirst, health and so on), the camera, the built-in gameplay tweaks, the Mods menu, apps on the in-game computers, and the player's look. The list of game areas and the known API inconsistencies are in [API.md](API.md).

The samples on this page are written as BepInEx 6 plugins. On BepInEx 5, delete the `using BepInEx.Unity.Mono;` line from each sample. The plugin class still derives from `BaseUnityPlugin`. The first sample uses the simpler [framework mod](api/framework-mods.md) form, which needs no BepInEx line at all.

## When to use it

- Write a line to your own log file, and see it in `BepInEx\logs`.
- Do something when a new hour or a new day starts, or when a save has loaded.
- Keep a number or a list of your own in the player's save, so it is still there after a reload.
- Make the player hungrier, slow down thirst, or react when the player faints.
- Add a page to the Mods menu, or an app to the in-game computers.
- Change what a game method does, in a way that stays safe when another mod changes the same method.

## Quick example

This framework mod writes one log line at the start of every game day. Once a save is loaded, it also logs the hour and the player's hunger value. `Events.DayPassed` is an event: the framework calls your code each time it happens. `Events.WhenReady` waits until a save is loaded and then runs your code once. See [Using events](guides/getting-started/using-events.md) for more on both.

```csharp
using ObenseuerCore;

[ModInfo("example.coretour", "Framework Tour", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class CoreTour : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.DayPassed += day => Log.Info("A new day started: day " + day);
        Events.WhenReady(() => Log.Info("It is " + GameTime.Hour + ":00 and the hunger value is " + Survival.Get(Stat.Hunger)));
    }
}
```
