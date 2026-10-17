---
title: WorldClock
parent: World
grand_parent: Places and travel
nav_order: 7
---

# WorldClock

`WorldClock` reads the calendar and sets the time of day and the date. `WorldClock.Now` gives you every part of the time in one read, as a `WorldTime`.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WorldTime</code> <span>the time in the game, in one read</span></p>
<div class="api-pair" markdown="1">

```csharp
public struct WorldTime
{
    public readonly int Year;
    public readonly int Month;
    public readonly int CalendarDay;
    public readonly int Day;
    public readonly DayOfWeek Weekday;
    public readonly int Hour;
    public readonly int Minute;
}
```

| Property | Type | What it means |
|---|---|---|
| `Year` | `int` | The year. |
| `Month` | `int` | The month, 1 to 12. |
| `CalendarDay` | `int` | The day of the month. |
| `Day` | `int` | The game's day counter. It is a different number from `CalendarDay`. |
| `Weekday` | `DayOfWeek` | The day of the week. |
| `Hour` | `int` | The hour, 0 to 23. |
| `Minute` | `int` | The minute, 0 to 59. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WorldClock</code> <span>read the calendar, set the time and date</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class WorldClock
{
    public static int Year { get; }
    public static int Month { get; }
    public static int CalendarDay { get; }
    public static bool IsAvailable { get; }
    public static WorldTime Now { get; }

    public static bool SetTimeOfDay(int hour, int minute);
    public static bool SetDate(int year, int month, int day);
}
```

| Property | Type | What it means |
|---|---|---|
| `Year` | `int` | The year. |
| `Month` | `int` | The month. |
| `CalendarDay` | `int` | The day of the month. `GameTime.Day` is the game's day counter, a different value. |
| `IsAvailable` | `bool` | `true` when the game clock exists. |
| `Now` | `WorldTime` | Every part of the time in one read. All zero when not available. |

| Method | Returns | What it does |
|---|---|---|
| `SetTimeOfDay(int hour, int minute)` | `bool` | Moves the clock forward to this time today. Hour 0 to 23, minute 0 to 59. |
| `SetDate(int year, int month, int day)` | `bool` | Sets the calendar date. Year 1 to 9999, month 1 to 12, day 3 to the last day of that month. |

</div>
</div>

## Things to know

- `WorldTime` is a read-only snapshot. `Now` fills it from the same calls as the separate reads, so it equals reading them one by one. When `IsAvailable` is `false`, `Now` is all zero (`Weekday` is `Sunday`). <span class="tech">(`Day` is `GameTime.Day`, `Weekday` is `GameTime.DayOfWeek`, `Hour` and `Minute` are `GameTime.Hour` and `GameTime.Minute`.)</span>
- `WorldClock` has no minute event. `TimeControl.MinutePassed` fires once for each game minute, and `Events.HourPassed` once for each game hour.
- Use `GameTime` to read the time of day and the day counter. `WorldClock` adds the calendar reads and the two setters that `GameTime` does not have.
- `SetTimeOfDay` only moves the clock forward. The minute is placed in the middle of its minute so it reads back as that minute. <span class="tech">(It calls the game's `TimeOfDayAzure.SetTimeline`, which takes hours from 0 to 24.)</span>
- `SetTimeOfDay` returns `false` when the time is earlier than the game's current time, seconds included, because the game does not move the day back. Use a later time, or wait for the next day. It also returns `false` while a save is loading or being written, and when the game's timeline value cannot be read.
- Moving the clock backwards with the game's own calls makes the game think midnight passed, and it then advances its timers by the difference it sees. `SetTimeOfDay` refuses an earlier time, so it never does this. <span class="tech">(The calls are `TimeOfDayAzure.SetTimeline` and the developer console.)</span>
- `SetDate` sets the calendar date only. It throws an `ArgumentException` for a year outside 1 to 9999, a month outside 1 to 12, or a day outside 3 to the last day of the month. It works forwards and backwards. <span class="tech">(It calls the game's `TimeOfDayAzure.SetDate`, the call the game itself makes on a save load. The game's calendar throws on year 0 and clamps a bad day.)</span>
- `SetDate` changes nothing but the calendar. The time of day, the game's day counter (`GameTime.Day`) and every timer stay as they are, and it raises no `Events.DayPassed`.
- Things that depend on the weekday or the day of the month react at the next minute. The weekday follows the date. NPC schedules, rent day (Sunday), and the weekly, biweekly and monthly subscription, invoice and bank payment checks all notice the new date. A low day of the month can make a payment come due. The date is saved with the game. <span class="tech">(`PlayerSubscription.CheckDateAndTrigger` compares the day of the month with the days since its last check.)</span>
- Setting the time does not fire `Events.HourPassed` or `Events.DayPassed` by itself. Those follow the game's own clock.
- `GameTime.IsDaytime` is the game's own rule for day. Day is the hours 6 to 20, so 6:00 to 20:59. The game's plants use the same rule. The answer depends on the hour alone, not on the sun's position, and it can lag `GameTime.Hour` slightly. <span class="tech">(It is `TimeOfDayAzure.IsItDay`. It reads the cached `currentTimeAndDay`, which is refreshed once per in-game minute and not while time updates are disabled.)</span>

## Limits

- `SetDate` returns `false` in some cases: while a save is loading or being written, when the clock is not available, and while the game has time updates off, which is just after a scene starts. A date set then would be taken for a new day.
- Days 1 and 2 of a month can't be set. The game never holds those days: when a day change lands on day 1, the game moves the date to day 3. So the date checks that compare the day of the month with a stored one have never run on those days. <span class="tech">(`TimeOfDayAzure.IncreaseDayCounter` does the move. The checks are in `PlayerSubscription`.)</span>
- Timers, perishables, mail and crops do not follow the calendar date. They use the game's day counter, the hour and the minute, which `SetDate` does not touch.
- You can't set the game's day counter. The game has no setter for it, only a call that adds a day and fires the day event. <span class="tech">(`dayCounter` has only `IncreaseDayCounter`.)</span>
