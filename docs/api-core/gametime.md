---
title: GameTime
parent: Framework core classes
grand_parent: Framework basics
nav_order: 6
---

# GameTime

`GameTime` reads the game's own clock: which day it is, the day of the week, the hour and the minute. It only reads. To change the date or the time of day, see [World](../api/world.md). To make the clock run faster or slower, see [Time control](../api/timecontrol.md).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>GameTime</code> <span>the game's clock, read only</span></p>

Read `Day`, `DayOfWeek`, `Hour` or `Minute` when your code needs the time. `IsDaytime` tells you whether it is day, and `IsPaused` whether the clock has stopped.

<div class="api-pair" markdown="1">

```csharp
public static class GameTime
{
    public static int Day { get; }
    public static System.DayOfWeek DayOfWeek { get; }
    public static int Hour { get; }
    public static int Minute { get; }
    public static bool IsDaytime { get; }
    public static bool IsPaused { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Day` | `int` | The day number. The first day is 0. |
| `DayOfWeek` | `System.DayOfWeek` | The day of the week. |
| `Hour` | `int` | The hour, 0 to 23. |
| `Minute` | `int` | The minute, 0 to 59. |
| `IsDaytime` | `bool` | `true` when the game says it is day. `false` when it is night or the clock is not ready. |
| `IsPaused` | `bool` | `true` when the game is paused. |

</div>
</div>

## Putting it together

Logs the day when the clock reaches 9:00 and the game is not paused.

<div class="api-example" markdown="1">

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

| Step | What it does |
|---|---|
| `GameTime.IsPaused` | Skips the check while the game is paused. |
| `GameTime.Hour == 9 && GameTime.Minute == 0` | Matches the minute of 9:00. |
| `GameTime.Day` | Adds the day number to the line. |

</div>

## Things to know

- Before a save is loaded, the day and time read as `0`, and `IsDaytime` reads as `false`. The clock does not exist on the main menu. <span class="tech">(`Day` reads `dayCounter` and `IsDaytime` calls the game's `IsItDay()`, both on `TimeOfDayAzure.instance`. With no instance they return `0` and `false`.)</span>
- `IsPaused` is true when the game stops its clock, not only when the pause menu is open. Anything that sets the game's time scale to 0 pauses it. <span class="tech">(It is `Time.timeScale == 0`.)</span>
