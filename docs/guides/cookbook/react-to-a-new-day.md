---
title: React to a new day
parent: Cookbook
nav_order: 3
---

# React to a new day

Log each new day, and set the clock to 07:00 on Mondays.


```csharp
using System;
using ObenseuerCore;

[ModInfo("example.newday", "New Day", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class NewDay : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.DayPassed += OnDayPassed;
    }

    private void OnDayPassed(int day)
    {
        Log.Info("day " + day + " is " + GameTime.DayOfWeek);
        if (SaveData.Busy) return;
        if (GameTime.DayOfWeek == DayOfWeek.Monday) WorldClock.SetTimeOfDay(7, 0);
    }
}
```

Replace the code in `Plugin.cs` of your project from [Your first mod](../getting-started/first-mod.md) with the code above. Then run `dotnet build -c Release -p:DeployToGame=true` and start the game.

Notes:

- `Events.DayPassed` gives the new day counter. `GameTime.DayOfWeek` gives the weekday.
- Check `SaveData.Busy` before you change game state. `WorldClock.SetTimeOfDay` returns `false` when the clock is not ready, during a save, or when the time is earlier than the current game time.
- Use `Events.HourPassed` if you need a check on every hour. See [Using events](../getting-started/using-events.md).
