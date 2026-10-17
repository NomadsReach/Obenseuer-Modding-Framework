---
title: Pieces of track
parent: Trains
grand_parent: Places and travel
nav_order: 5
---

# Pieces of track

A track segment is one piece of track. Each piece can lead on to one or more next pieces.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Trains</code> <span>lists the pieces of track</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Trains
{
    public static List<TrackSegmentInfo> Segments();
}
```

| Method | Returns | What it does |
|---|---|---|
| `Segments()` | `List<TrackSegmentInfo>` | Lists the pieces of track that are switched on in the scene you are in. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TrackSegmentInfo</code> <span>a snapshot of one piece of track</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class TrackSegmentInfo
{
    public string Id { get; }
    public bool Occupied { get; }
    public string OccupiedBy { get; }
    public int ActiveBranch { get; }
    public string Previous { get; }
    public List<string> Next { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The piece's id. |
| `Occupied` | `bool` | `true` when a train is on it. |
| `OccupiedBy` | `string` | The id of the train on it. `null` when it is free. |
| `ActiveBranch` | `int` | The number of the branch the piece is set to send trains down. |
| `Previous` | `string` | The id of the piece before it. `null` when there is none. |
| `Next` | `List<string>` | The pieces after it, one for each branch. `null` for a branch that leads nowhere. |

</div>
</div>

## Things to know

- `Segments()` gives an empty list when no level is loaded. <span class="tech">(It searches the active objects of the active scene for `Track_Segment`, as `Switches` does.)</span>
- A piece's id is its save GUID, or a scene path when it has none.
- `ActiveBranch` follows the switch that controls the piece. After the switch has changed, it matches the switch's branch. <span class="tech">(A minitrain switch writes `activeBranchIndex` through `SetBranchIndex` when its own branch changes, for a forced change too.)</span>

## Limits

- You can't set a piece of track's branch yourself. Doing so would send trains down one branch while the switch still shows another. Use `SetBranch` on the switch instead. <span class="tech">(`Track_Segment.SetBranchIndex` changes the routing without moving the switch.)</span>
