---
title: Time control
parent: Places and travel
grand_parent: API reference
has_children: true
nav_order: 14
---

# Time control

## What it is

The game has a clock that runs through the day and night. This API lets your mod make that clock run faster or slower, run a piece of code after a number of in-game minutes, and get told each time an in-game minute passes. It does not change the date or the time of day. To set those, see [World](world.md).

## When to use it

- Speed time up or slow it down for a while, for example during an event.
- Run something "in 30 minutes" of game time, such as a delivery arriving.
- Do something every in-game minute, such as a small clock or a counter.

## Quick example

This mod runs the clock twice as fast. It also logs a line when 30 in-game minutes have passed since the mod started. The first argument to each call is your mod's id.

```csharp
using ObenseuerCore;

[ModInfo("example.timewatch", "Time Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TimeWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        TimeControl.SetTimeScale(Mod.Id, 2f);
        TimeControl.In(Mod.Id, 30, () => Log.Info("30 in-game minutes passed."));
    }
}
```

The framework forgets your speed and your timers when the player loads a save or starts a new game, so to keep a speed or timer you set it again after the load. See "When the framework forgets your settings" below. `Mod.Id` is your mod's id, the one in your `ModInfo`.
