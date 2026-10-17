---
title: Common tasks
parent: Crafting stations
grand_parent: Items and crafting
nav_order: 1
---

# Common tasks

Make only one kind of station slower. This makes the process stations take twice as long. Kinds are explained in [Speed](speed.md#speed).

```csharp
CraftingSpeed.SetSpeedFactor(Mod.Id, "manufacturingProcess", 0.5f);
```

Make everything the player crafts one and a half times its normal quality.

```csharp
if (!CraftingQuality.SetQualityFactor(Mod.Id, 1.5f))
    Log.Warn("Could not set the quality factor.");
```

Remove your speed and quality changes.

```csharp
CraftingSpeed.Clear(Mod.Id);
CraftingQuality.Clear(Mod.Id);
```

Start a craft at every station, and log how many started. The station must already have a recipe selected, as explained in [Start and stop](start-and-stop.md#start-and-stop).

```csharp
int started = 0;
foreach (CraftingStationInfo station in Crafting.All())
    if (CraftingStations.Start(Mod.Id, station.Id)) started++;
Log.Info(started + " stations started.");
```

Pause every station that is running a timed batch, then let them carry on.

```csharp
foreach (CraftingStationInfo station in Crafting.All())
    CraftingStations.Pause(Mod.Id, station.Id);

foreach (CraftingStationInfo station in Crafting.All())
    CraftingStations.Resume(Mod.Id, station.Id);
```
