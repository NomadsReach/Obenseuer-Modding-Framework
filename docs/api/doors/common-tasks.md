---
title: Common tasks
parent: Doors
grand_parent: Places and travel
nav_order: 1
---

# Common tasks

This page uses `Vector3` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Open one door. `Open` gives back `false` when the door is locked, so unlock it first if you need to.

```csharp
foreach (DoorInfo door in Doors.All())
{
    if (door.IsOpen) continue;
    if (Doors.Open(door.Id)) Log.Info("Opened " + door.Name);
    break;
}
```

Close a door that is open. In the next snippets, `doorId` is the `Id` of a door from `Doors.All()`.

```csharp
if (Doors.IsOpen(doorId)) Doors.Close(doorId);
```

Make a shop door open from 9 in the morning to 5 in the afternoon on weekdays. The hours are in 24-hour time.

```csharp
List<DoorOpenTime> hours = new List<DoorOpenTime>();
foreach (DayOfWeek day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
    hours.Add(new DoorOpenTime(day, 9, 17));
if (!Doors.SetOpeningTimes(doorId, hours)) Log.Warn("That door has no opening hours to change.");
```

Read the spot where NPCs wait for a door. It is `null` when the door has no waiting spot.

```csharp
Vector3? spot = Doors.Get(doorId)?.StandPosition;
if (spot.HasValue) Log.Info("NPCs wait at " + spot.Value);
```
