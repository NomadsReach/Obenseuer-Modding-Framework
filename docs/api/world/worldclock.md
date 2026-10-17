---
title: WorldClock
parent: World
grand_parent: Places and travel
nav_order: 7
---

# WorldClock

`WorldClock` reads the calendar and sets the time of day and the date. `WorldClock.Now` gives you every part of the time in one read, as a `WorldTime`.

```csharp
public struct WorldTime    // the time in the game, in one read; every field is read only
{
    public readonly int Year;             // the year
    public readonly int Month;            // the month, 1 to 12
    public readonly int CalendarDay;      // the day of the month
    public readonly int Day;              // the game's day counter, a different number from CalendarDay
    public readonly DayOfWeek Weekday;    // the day of the week
    public readonly int Hour;             // the hour, 0 to 23
    public readonly int Minute;           // the minute, 0 to 59
}

public static class WorldClock
{
    public static int Year { get; }
    public static int Month { get; }
    public static int CalendarDay { get; }           // the day of the month; GameTime.Day is the game day counter, a different value
    public static bool IsAvailable { get; }          // true when the game clock exists
    public static WorldTime Now { get; }             // every part of the time in one read; all zero when not available

    public static bool SetTimeOfDay(int hour, int minute);     // moves the clock forward to this time today; hour 0..23, minute 0..59
    public static bool SetDate(int year, int month, int day);  // sets the calendar date; year 1..9999, month 1..12, day 3..days in that month
}
```

- **`WorldTime` is a read-only snapshot.** `Now` fills it from the same calls as the separate reads, so it equals reading them one by one. When `IsAvailable` is `false`, `Now` is all zero (`Weekday` is `Sunday`).
  - Technical: `Day` is `GameTime.Day`, `Weekday` is `GameTime.DayOfWeek`, `Hour` and `Minute` are `GameTime.Hour` and `GameTime.Minute`.
- **`WorldClock` has no minute event.** `TimeControl.MinutePassed` fires once for each game minute, and `Events.HourPassed` once for each game hour.
- **Use `GameTime` to read the time of day and the day counter.** `WorldClock` adds the calendar reads and the two setters that `GameTime` does not have.
- **`SetTimeOfDay` only moves the clock forward.** The minute is placed in the middle of its minute so it reads back as that minute. It returns `false` when the time is earlier than the game's current time, seconds included, because the game does not move the day back. Use a later time, or wait for the next day. It also returns `false` while a save is loading or being written, and when the game's timeline value cannot be read.
  - Technical: it calls the game's `TimeOfDayAzure.SetTimeline`, which takes hours from 0 to 24.
- **Moving the clock backwards with the game's own calls makes the game think midnight passed.** The game then advances its timers by the difference it sees. `SetTimeOfDay` refuses an earlier time, so it never does this.
  - Technical: the calls are `TimeOfDayAzure.SetTimeline` and the developer console.
- **`SetDate` sets the calendar date only.** It throws an `ArgumentException` for a year outside 1 to 9999, a month outside 1 to 12, or a day outside 3 to the last day of the month. It works forwards and backwards.
  - Technical: it calls the game's `TimeOfDayAzure.SetDate`, the call the game itself makes on a save load. The game's calendar throws on year 0 and clamps a bad day.
- **Days 1 and 2 of a month can't be set.** The game never holds those days: when a day change lands on day 1, the game moves the date to day 3. So the date checks that compare the day of the month with a stored one have never run on those days.
  - Technical: `TimeOfDayAzure.IncreaseDayCounter` does the move. The checks are in `PlayerSubscription`.
- **`SetDate` returns `false` in some cases.** While a save is loading or being written, when the clock is not available, and while the game has time updates off, which is just after a scene starts. A date set then would be taken for a new day.
- **`SetDate` changes nothing but the calendar.** The time of day, the game's day counter (`GameTime.Day`) and every timer stay as they are, and it raises no `Events.DayPassed`.
- **Things that depend on the weekday or the day of the month react at the next minute.** The weekday follows the date. NPC schedules, rent day (Sunday), and the weekly, biweekly and monthly subscription, invoice and bank payment checks all notice the new date. A low day of the month can make a payment come due. The date is saved with the game.
  - Technical: `PlayerSubscription.CheckDateAndTrigger` compares the day of the month with the days since its last check.
- **Timers, perishables, mail and crops do not follow the calendar date.** They use the game's day counter, the hour and the minute, which `SetDate` does not touch.
- **Setting the time does not fire `Events.HourPassed` or `Events.DayPassed` by itself.** Those follow the game's own clock.
- **You can't set the game's day counter.** The game has no setter for it, only a call that adds a day and fires the day event.
  - Technical: `dayCounter` has only `IncreaseDayCounter`.
- **`GameTime.IsDaytime` is the game's own rule for day.** Day is the hours 6 to 20, so 6:00 to 20:59. The game's plants use the same rule. The answer depends on the hour alone, not on the sun's position, and it can lag `GameTime.Hour` slightly.
  - Technical: it is `TimeOfDayAzure.IsItDay`. It reads the cached `currentTimeAndDay`, which is refreshed once per in-game minute and not while time updates are disabled.
