---
title: Pieces of track
parent: Trains
grand_parent: Places and travel
nav_order: 5
---

# Pieces of track

A track segment is one piece of track. Each piece can lead on to one or more next pieces.

```csharp
public static class Trains
{
    public static List<TrackSegmentInfo> Segments();   // every piece of track in the scene you are in
}

public sealed class TrackSegmentInfo    // a snapshot of one piece of track
{
    public string Id { get; }              // the piece's id
    public bool Occupied { get; }          // true when a train is on it
    public string OccupiedBy { get; }      // the id of the train on it; null when it is free
    public int ActiveBranch { get; }       // the number of the branch the piece is set to send trains down
    public string Previous { get; }        // the id of the piece before it; null when there is none
    public List<string> Next { get; }      // the pieces after it, one for each branch; null for a branch that leads nowhere
}
```

- **`Segments()` lists the pieces of track that are switched on in the scene you are in.** It gives an empty list when no level is loaded.
  - Technical: it searches the active objects of the active scene for `Track_Segment`, as `Switches` does.
- **A piece's id is its save GUID, or a scene path when it has none.**
- **`ActiveBranch` follows the switch that controls the piece.** After the switch has changed, it matches the switch's branch.
  - Technical: a minitrain switch writes `activeBranchIndex` through `SetBranchIndex` when its own branch changes (for a forced change too).
- **You can't set a piece of track's branch yourself.** Doing so would send trains down one branch while the switch still shows another. Use `SetBranch` on the switch instead.
  - Technical: `Track_Segment.SetBranchIndex` changes the routing without moving the switch.
