---
title: Common tasks
parent: Time control
grand_parent: Places and travel
nav_order: 1
---

# Common tasks

Run the clock four times slower, and go back to normal speed later.

```csharp
TimeControl.SetTimeScale(Mod.Id, 0.25f);
TimeControl.ClearTimeScale(Mod.Id);
```

Run something after 10 in-game minutes, and cancel it if you no longer want it. `In` gives you a handle, a number that names that one timer.

```csharp
int handle = TimeControl.In(Mod.Id, 10, () => Log.Info("10 minutes later"));
if (handle > 0) TimeControl.Cancel(Mod.Id, handle);
```

Do something every in-game minute.

```csharp
TimeControl.MinutePassed += () => Log.Info("A minute passed.");
```

Find out how long the game's day is, in real minutes.

```csharp
float minutes = TimeControl.DayLengthMinutes;
Log.Info("A day lasts " + minutes + " real minutes.");
```
