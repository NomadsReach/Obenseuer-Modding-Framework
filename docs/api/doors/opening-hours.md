---
title: Opening hours
parent: Doors
grand_parent: Places and travel
nav_order: 4
---

# Opening hours

Some doors follow a schedule. The game locks them outside their opening hours and unlocks them inside. A door has opening hours only when a schedule lists it, and most doors have none.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DoorOpenTime</code> <span>one window of time when a door is open</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class DoorOpenTime
{
    public DoorOpenTime(DayOfWeek day, int opens, int closes);
    public DayOfWeek Day { get; }
    public int Opens { get; }
    public int Closes { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `DoorOpenTime(DayOfWeek day, int opens, int closes)` | `DoorOpenTime` | Makes one window of time. |

| Property | Type | What it means |
|---|---|---|
| `Day` | `DayOfWeek` | The day of the week this window is on. |
| `Opens` | `int` | The first hour the door is open, 0 to 23. |
| `Closes` | `int` | The hour the door shuts. Above `Opens` and at most 24. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Doors</code> <span>reads and replaces opening hours</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Doors
{
    public static List<DoorOpenTime> OpeningTimes(string id);
    public static bool SetOpeningTimes(string id, IList<DoorOpenTime> times);
}
```

| Method | Returns | What it does |
|---|---|---|
| `OpeningTimes(string id)` | `List<DoorOpenTime>` | The door's hours. `null` when it has none. |
| `SetOpeningTimes(string id, IList<DoorOpenTime> times)` | `bool` | Replaces all of the door's hours. `true` when something changed. |

</div>
</div>

## Things to know

- A window runs from `Opens` up to, but not including, `Closes`. A door with `Opens` 9 and `Closes` 17 is open from 9:00 to 16:59. A window cannot go past midnight, so for a night shift add a second window on the next day.
- One day can have several windows.
- `OpeningTimes` gives `null` for a door with no schedule. For a schedule with an empty list it gives an empty list, and that door stays locked all week. If a door is in more than one schedule, you get the first one found. <span class="tech">(It reads `DoorSchedule.OpenTimes`. The game counts Monday as day 0, and the framework converts to .NET `DayOfWeek`.)</span>
- `SetOpeningTimes` replaces all of the hours and gives `true` when it changed at least one schedule. It gives `false` when the id is not known, when the door has no schedule, or when a save is loading or being written.
- `SetOpeningTimes` also gives `false` when the list is `null`, when one entry in it is `null`, when an entry has `Opens` outside 0 to 23, or when `Closes` is not above `Opens` or is above 24. An empty list is allowed. It keeps the door locked all week.
- Changing the hours of one door changes them for every door on that schedule. One schedule often serves several doors and the shop signs that go with them.
- The change shows within about a game minute, not at once, and the new hours are not saved. After a reload the scene's own hours come back until you set yours again. The locked or unlocked state that the schedule produced is saved with the doors. <span class="tech">(At the next game minute the game locks the schedule's doors outside the hours and unlocks them inside (`Door.Lock`, `Door.Unlock`). The framework resets the schedule's `lastCheck` so it runs again. A lock that the player opened with a key or a lockpick is closed again only when the schedule moves from open to shut. `Locks.Locked` and `Locks.Unlocked` report those changes. The schedule drives locks, not the swing of the door.)</span>
