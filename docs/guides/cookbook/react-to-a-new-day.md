---
title: React to a new day
parent: Cookbook
nav_order: 3
---

# React to a new day

Log each new day, and set the clock to 07:00 on Mondays.

1. Build and copy the DLL, as in [Your first mod](../getting-started/first-mod.md).

```csharp
using System;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

[BepInPlugin("example.newday", "New Day", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
public class NewDay : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.newday");

    private void Awake()
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

On BepInEx 5, write `"0.11.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](../getting-started/packaging.md#supporting-bepinex-5-too).

Notes:

- `Events.DayPassed` gives the new day counter. `GameTime.DayOfWeek` gives the weekday.
- Check `SaveData.Busy` before you change game state. `WorldClock.SetTimeOfDay` returns `false` when the clock is not ready, during a save, or when the time is earlier than the current game time.
- Use `Events.HourPassed` if you need a check on every hour. See [Using events](../getting-started/using-events.md).
