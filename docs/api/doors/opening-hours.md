---
title: Opening hours
parent: Doors
grand_parent: Places and travel
nav_order: 4
---

# Opening hours

Some doors follow a schedule. The game locks them outside their opening hours and unlocks them inside. A door has opening hours only when a schedule lists it, and most doors have none.

```csharp
public sealed class DoorOpenTime    // one window of time when a door is open
{
    public DoorOpenTime(DayOfWeek day, int opens, int closes);
    public DayOfWeek Day { get; }   // the day of the week this window is on
    public int Opens { get; }       // the first hour the door is open, 0 to 23
    public int Closes { get; }      // the hour the door shuts, above Opens and at most 24
}

public static class Doors
{
    public static List<DoorOpenTime> OpeningTimes(string id);                       // the door's hours; null when it has none
    public static bool SetOpeningTimes(string id, IList<DoorOpenTime> times);       // replaces the door's hours; true when something changed
}
```

- **A window runs from `Opens` up to, but not including, `Closes`.** A door with `Opens` 9 and `Closes` 17 is open from 9:00 to 16:59. A window cannot go past midnight, so for a night shift add a second window on the next day.
- **One day can have several windows.**
- **`OpeningTimes` gives `null` for a door with no schedule.** For a schedule with an empty list it gives an empty list, and that door stays locked all week. If a door is in more than one schedule, you get the first one found.
  - Technical: it reads `DoorSchedule.OpenTimes`. The game counts Monday as day 0, and the framework converts to .NET `DayOfWeek`.
- **`SetOpeningTimes` replaces all of the hours and gives `true` when it changed at least one schedule.** It gives `false` when:
  - the id is not known, or the door has no schedule;
  - the list is `null`, or one entry in it is `null`;
  - an entry has `Opens` outside 0 to 23, or `Closes` that is not above `Opens`, or above 24;
  - a save is loading or being written.
  - An empty list is allowed. It keeps the door locked all week.
- **Changing the hours of one door changes them for every door on that schedule.** One schedule often serves several doors and the shop signs that go with them.
- **The change shows within about a game minute, not at once, and the new hours are not saved.** After a reload the scene's own hours come back until you set yours again. The locked or unlocked state that the schedule produced is saved with the doors.
  - Technical: at the next game minute the game locks the schedule's doors outside the hours and unlocks them inside (`Door.Lock`, `Door.Unlock`). The framework resets the schedule's `lastCheck` so it runs again. A lock that the player opened with a key or a lockpick is closed again only when the schedule moves from open to shut. `Locks.Locked` and `Locks.Unlocked` report those changes. The schedule drives locks, not the swing of the door.
