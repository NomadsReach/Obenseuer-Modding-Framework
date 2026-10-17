---
title: The calls
parent: Crafting stations
grand_parent: Items and crafting
nav_order: 2
---

# The calls

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

These are the three classes. A `modId` is your mod's id, the same one your mod uses for its settings. See [Owner](../../glossary.md#owner) in the glossary.

```csharp
public static class CraftingSpeed
{
    public const string AllStations = "*";    // pass this as the station kind to cover every kind of station
    public static bool SpeedFactorsAvailable { get; }    // false when the speed setup failed
    public static bool SetSpeedFactor(string modId, string stationKind, float speed);   // speed from 0.01 to 4; true when it was set
    public static bool Clear(string modId);   // removes this mod's speed changes
}

public static class CraftingQuality
{
    public static bool QualityFactorAvailable { get; }   // false when the quality setup failed
    public static bool SetQualityFactor(string modId, float factor);   // factor from 0 to 4; true when it was set
    public static bool Clear(string modId);   // removes this mod's quality change
}

public static class CraftingStations
{
    public static bool Start(string modId, string stationId);    // starts a craft; true when the station started
    public static bool Stop(string modId, string stationId);     // ends a liquid batch early; true when it stopped
    public static bool Pause(string modId, string stationId);    // freezes a running batch; true when it froze
    public static bool Resume(string modId, string stationId);   // unfreezes a batch; true when it carried on
}
```
