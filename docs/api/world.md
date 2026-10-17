---
title: World
parent: Places and travel
grand_parent: API reference
has_children: true
nav_order: 18
---

# World

## What it is

The world API is about the place the player is in and the conditions around them. It has six parts. `WorldWeather` reads and changes the weather, including local weather zones. `WorldPower` tells you about blackouts and generators and lets you control the player's wake-up after a blackout. `WorldWater` reads the water supply and the water the player swims in. `WorldRadiation` reads how much radiation the player has taken in. `WorldLocation` tells you which scene the player is in and which ones they have visited. `WorldClock` reads the calendar and sets the time of day and the date.

## When to use it

- React when the weather changes, or set the weather for a scene.
- React when a blackout starts or a generator is switched on or off.
- Check whether the player is swimming or underwater, or give them more breath.
- Show the player's radiation dose, or the reading on their dosimeter.
- Find out which scene the player is in, or which ones they have been to.
- Read the date, or move the clock forward.

## Quick example

This mod logs a line when the weather changes, a blackout starts or the player goes under water. Once a save is loaded, it logs the current weather, the date and the scene.

```csharp
using ObenseuerCore;

[ModInfo("example.worldwatch", "World Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class WorldWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        WorldWeather.WeatherChanged += args => Log.Info("Weather is now " + WorldWeather.NameOf(args.Index));
        WorldPower.BlackoutStarted += () => Log.Info("A blackout started.");
        WorldWater.PlayerSubmerged += () => Log.Info("The player went under water.");
        Events.WhenReady(() =>
        {
            WorldTime now = WorldClock.Now;
            Log.Info("Weather: " + WorldWeather.CurrentName + ", " + WorldWeather.Temperature + " degrees.");
            Log.Info("Date: " + now.Year + "-" + now.Month + "-" + now.CalendarDay + " " + now.Hour + ":" + now.Minute);
            Log.Info("Scene: " + WorldLocation.CurrentScene);
        });
    }
}
```

`WeatherChanged`, `BlackoutStarted` and `PlayerSubmerged` are events: the framework calls your code each time they happen. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.
