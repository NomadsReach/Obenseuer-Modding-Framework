---
title: The calls
parent: Crafting stations
grand_parent: Items and crafting
nav_order: 2
---

# The calls

This is the full list of what the API gives you. A `modId` is your mod's id, the same one your mod uses for its settings. See [Owner](../../glossary.md#owner) in the glossary.

These are the three classes.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CraftingSpeed</code> <span>how fast a kind of station works</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class CraftingSpeed
{
    public const string AllStations = "*";
    public static bool SpeedFactorsAvailable { get; }
    public static bool SetSpeedFactor(string modId, string stationKind, float speed);
    public static bool Clear(string modId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetSpeedFactor(string modId, string stationKind, float speed)` | `bool` | Sets how fast one kind of station works for your mod. The speed is from 0.01 to 4. Returns `true` when it was set. |
| `Clear(string modId)` | `bool` | Removes all of this mod's speed changes. |

| Property | Type | What it means |
|---|---|---|
| `AllStations` | `string` | Pass this as the station kind to cover every kind of station. It is `"*"`. |
| `SpeedFactorsAvailable` | `bool` | `false` when the speed setup failed. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CraftingQuality</code> <span>how good what stations make is</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class CraftingQuality
{
    public static bool QualityFactorAvailable { get; }
    public static bool SetQualityFactor(string modId, float factor);
    public static bool Clear(string modId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetQualityFactor(string modId, float factor)` | `bool` | Sets how good everything a station makes is, for your mod. The factor is from 0 to 4. Returns `true` when it was set. |
| `Clear(string modId)` | `bool` | Removes this mod's quality change. |

| Property | Type | What it means |
|---|---|---|
| `QualityFactorAvailable` | `bool` | `false` when the quality setup failed. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CraftingStations</code> <span>start, stop, pause and resume a station</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class CraftingStations
{
    public static bool Start(string modId, string stationId);
    public static bool Stop(string modId, string stationId);
    public static bool Pause(string modId, string stationId);
    public static bool Resume(string modId, string stationId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Start(string modId, string stationId)` | `bool` | Starts a craft. Returns `true` when the station started. |
| `Stop(string modId, string stationId)` | `bool` | Ends a liquid batch early. Returns `true` when it stopped. |
| `Pause(string modId, string stationId)` | `bool` | Freezes a running batch. Returns `true` when it froze. |
| `Resume(string modId, string stationId)` | `bool` | Unfreezes a batch. Returns `true` when it carried on. |

</div>
</div>
