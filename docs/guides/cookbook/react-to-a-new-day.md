---
title: React to a new day
parent: Cookbook
nav_order: 3
---

# React to a new day

Write a log line each new game day, and set the clock to 07:00 on Mondays. Use this when your mod should do something at the start of each day.

`Events.DayPassed` is the signal for a new day. `GameTime.DayOfWeek` says which weekday it is, and `WorldClock.SetTimeOfDay` moves the clock.

```csharp
using System;
using ObenseuerCore;

[ModInfo("example.newday", "New Day", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class NewDay : FrameworkMod
{
    protected override void OnLoad()
    {
        // Run OnDayPassed every time the game day changes.
        Events.DayPassed += OnDayPassed;
    }

    private void OnDayPassed(int day)
    {
        // Write the new day number and its weekday to the log.
        Log.Info("day " + day + " is " + GameTime.DayOfWeek);
        // Do nothing while a save or load is running.
        if (SaveData.Busy) return;
        // On Mondays, move the clock to 07:00.
        if (GameTime.DayOfWeek == DayOfWeek.Monday) WorldClock.SetTimeOfDay(7, 0);
    }
}
```

Replace the code in `Plugin.cs` of your project from [Your first mod](../getting-started/first-mod.md) with the code above. Then run `dotnet build -c Release -p:DeployToGame=true` with the game closed, and start the game. Load a save and sleep or wait until the next day. Open `BepInEx\logs\example.newday.log` to see the day lines.

Notes:

- `Events.DayPassed` gives the new day counter. `GameTime.DayOfWeek` gives the weekday.
- Check `SaveData.Busy` before you change game state. `WorldClock.SetTimeOfDay` returns `false` when the clock is not ready, during a save, or when the time is earlier than the current game time.
- Use `Events.HourPassed` if you need a check on every hour. See [Using events](../getting-started/using-events.md).

## Where to go next

Next: [Add a Mods menu page](mods-menu-page.md).
