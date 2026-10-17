---
title: Switches and loaders
parent: Trains
grand_parent: Places and travel
nav_order: 4
---

# Switches and loaders

A track switch is a lever that sends a train down one of several branches of track. A wagon loader is a machine that puts goods in a wagon.

```csharp
public static class Trains
{
    public static List<GameObject> Switches { get; }   // every track switch in the scene you are in
    public static List<GameObject> Loaders { get; }    // every wagon loader in the scene you are in

    public static int GetBranch(GameObject target);                                    // which branch a switch points at; -1 when the object is not a switch
    public static bool SetBranch(GameObject target, int branch, bool force);           // points a switch at another branch; true when the change was accepted
    public static bool SetLoaderOn(GameObject target, bool on);                        // turns a wagon loader on or off; true when it ends up in that state
    public static bool SetLoaderFreeToTake(GameObject target, bool freeToTake);        // sets whether the player may take the loader's goods without stealing
}
```

- **`Switches` and `Loaders` list the objects that are switched on in the scene you are in.** Pass one of them to the calls below.
  - Technical: they are the `MinitrainSwitch` and `MinitrainWagonLoader` objects found among the active objects of the active scene.
- **`GetBranch` tells you which branch a switch points at, and `-1` when the object is not a switch.** It throws an `ArgumentNullException` for a `null` object. The branch number only changes when the lever has finished moving, not when you call `SetBranch`.
  - Technical: it reads `MinitrainSwitch.currentBranchIndex`, which is updated when the animation ends (`ChangeTrackCO`).
- **`SetBranch` with `force` set to `false` follows the game's own rule: it won't move a switch that has a train on it.** It returns `false` when a train is on the switch's piece of track, or, for a reversed switch, on the piece before it. Otherwise it starts the lever and returns `true`. The game keeps waiting for the track to clear during the lever's movement as well.
  - Technical: `false` here is the game's occupancy check in `ChangeTrack`, `MinitrainSwitch`: the switch's `trackSegment.occupied`, or, when `isReversed` is set, the previous segment's `occupied`. On success it calls `ChangeTrack(branch)`.
- **`SetBranch` with `force` set to `true` moves the switch even with a train on it.** The branch changes at once, and then the lever moves.
  - Technical: it calls `ChangeTrackForce(branch)`, which sets `currentBranchIndex` at once and then animates.
- **`SetBranch` returns `false` when the switch has no such branch, and throws an error for a negative branch.** The negative number throws an `ArgumentOutOfRangeException`. It also returns `false` when the object is not a switch, and while a save is loading or being written.
  - Technical: the number of branches is the length of the private `MinitrainSwitch.branchRotations`, read through reflection.
- **`SetLoaderOn` turns a wagon loader on or off.** It returns `true` when the loader ends up in the state you asked for.
  - Technical: it calls `MinitrainWagonLoader.Toggle(on)` and compares `active`.
- **`SetLoaderFreeToTake` sets whether the player may take the goods the loader stocks without that counting as stealing.** When it is on, each item the loader newly stocks is marked as allowed to take. It returns `true` when the loader ends up with the value you asked for.
  - Technical: it calls `SetOwnerPermissions(freeToTake)`, which sets `permissionToTake`. When it is `true`, the loader tags each newly stocked item (owner ID above 1) with `ItemOwnerPermission { ownerPermission = true }`.
- **The writing calls throw an error for a `null` object.** `SetBranch`, `SetLoaderOn` and `SetLoaderFreeToTake` throw an `ArgumentNullException`. They return `false` while a save is loading or being written, and `false` when the object is not the right kind of object.
