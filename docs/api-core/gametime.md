---
title: GameTime
parent: Framework core classes
grand_parent: Framework basics
nav_order: 6
---

# GameTime

`GameTime` reads the game's own clock: which day it is, the day of the week, the hour and the minute. It only reads. To change the date or the time of day, see [World](../api/world.md). To make the clock run faster or slower, see [Time control](../api/timecontrol.md).

```csharp
public static class GameTime
{
    public static int Day { get; }                 // the day number; the first day is 0
    public static System.DayOfWeek DayOfWeek { get; }   // the day of the week
    public static int Hour { get; }                // the hour, 0 to 23
    public static int Minute { get; }              // the minute, 0 to 59
    public static bool IsDaytime { get; }          // true when the game says it is day; false when it is night or the clock is not ready
    public static bool IsPaused { get; }           // true when the game is paused
}
```

- **Before a save is loaded, the day and time read as `0` and `IsDaytime` reads as `false`.** The clock does not exist on the main menu.
  - Technical: `Day` reads `dayCounter` and `IsDaytime` calls the game's `IsItDay()`, both on `TimeOfDayAzure.instance`; with no instance they return `0` and `false`.
- **`IsPaused` is true when the game stops its clock, not only when the pause menu is open.** Anything that sets the game's time scale to 0 pauses it.
  - Technical: it is `Time.timeScale == 0`.

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

public class NineOClock : BaseUnityPlugin
{
    private void Update()
    {
        if (!GameTime.IsPaused && GameTime.Hour == 9 && GameTime.Minute == 0)
        {
            Logger.LogInfo("it is nine o'clock on day " + GameTime.Day);
        }
    }
}
```
