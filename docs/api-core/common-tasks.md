---
title: Common tasks
parent: Framework core classes
grand_parent: Framework basics
nav_order: 1
---

# Common tasks

Write to your own log file. The file is `BepInEx\logs\example.mymod.log`.

```csharp
CoreLog log = CoreLog.For("example.mymod");
log.Info("started");
log.Warn("something looks odd");
```

Do something at the start of every game hour.

```csharp
Events.HourPassed += hour => CoreLog.For("example.mymod").Info("hour " + hour);
```

Keep a number in the player's save. `s ?? new Counter()` handles a save made before your mod existed.

```csharp
Counter state = new Counter();
SaveData.Register<Counter>("example.mymod", () => state, s => state = s ?? new Counter());
```

where `Counter` is a small class of your own:

```csharp
[System.Serializable]
public class Counter
{
    public int visits;
}
```

Read a need and set it. This heals the player to full when the health value drops below 20.

```csharp
if (Survival.Get(Stat.Health) < 20f) Survival.Set(Stat.Health, 100f);
```

Switch the camera to third person if the game allows it.

```csharp
if (CameraView.IsAvailable) CameraView.SetThirdPerson();
```
