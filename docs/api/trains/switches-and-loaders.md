---
title: Switches and loaders
parent: Trains
grand_parent: Places and travel
nav_order: 4
---

# Switches and loaders

A track switch is a lever that sends a train down one of several branches of track. A wagon loader is a machine that puts goods in a wagon.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Trains</code> <span>switches and wagon loaders</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Trains
{
    public static List<GameObject> Switches { get; }
    public static List<GameObject> Loaders { get; }
    public static int GetBranch(GameObject target);
    public static bool SetBranch(GameObject target, int branch, bool force);
    public static bool SetLoaderOn(GameObject target, bool on);
    public static bool SetLoaderFreeToTake(GameObject target, bool freeToTake);
}
```

| Method | Returns | What it does |
|---|---|---|
| `GetBranch(GameObject target)` | `int` | Which branch a switch points at. `-1` when the object is not a switch. |
| `SetBranch(GameObject target, int branch, bool force)` | `bool` | Points a switch at another branch. `true` when the change was accepted. |
| `SetLoaderOn(GameObject target, bool on)` | `bool` | Turns a wagon loader on or off. `true` when it ends up in that state. |
| `SetLoaderFreeToTake(GameObject target, bool freeToTake)` | `bool` | Sets whether the player may take the loader's goods without stealing. |

| Property | Type | What it means |
|---|---|---|
| `Switches` | `List<GameObject>` | Every track switch in the scene you are in. |
| `Loaders` | `List<GameObject>` | Every wagon loader in the scene you are in. |

</div>
</div>

## Things to know

- `Switches` and `Loaders` list the objects that are switched on in the scene you are in. Pass one of them to the calls below. <span class="tech">(They are the `MinitrainSwitch` and `MinitrainWagonLoader` objects found among the active objects of the active scene.)</span>
- `GetBranch` tells you which branch a switch points at. The branch number only changes when the lever has finished moving, not when you call `SetBranch`. <span class="tech">(It reads `MinitrainSwitch.currentBranchIndex`, which is updated when the animation ends (`ChangeTrackCO`).)</span>
- `SetBranch` with `force` set to `false` follows the game's own rule: it won't move a switch that has a train on it. If the change is accepted, it starts the lever and returns `true`. The game keeps waiting for the track to clear during the lever's movement as well. <span class="tech">(`false` here is the game's occupancy check in `ChangeTrack`, `MinitrainSwitch`: the switch's `trackSegment.occupied`, or, when `isReversed` is set, the previous segment's `occupied`. On success it calls `ChangeTrack(branch)`.)</span>
- `SetBranch` with `force` set to `true` moves the switch even with a train on it. The branch changes at once, and then the lever moves. <span class="tech">(It calls `ChangeTrackForce(branch)`, which sets `currentBranchIndex` at once and then animates.)</span>
- `SetLoaderOn` turns a wagon loader on or off. It returns `true` when the loader ends up in the state you asked for. <span class="tech">(It calls `MinitrainWagonLoader.Toggle(on)` and compares `active`.)</span>
- `SetLoaderFreeToTake` sets whether the player may take the goods the loader stocks without that counting as stealing. When it is on, each item the loader newly stocks is marked as allowed to take. It returns `true` when the loader ends up with the value you asked for. <span class="tech">(It calls `SetOwnerPermissions(freeToTake)`, which sets `permissionToTake`. When it is `true`, the loader tags each newly stocked item (owner ID above 1) with `ItemOwnerPermission { ownerPermission = true }`.)</span>

## Limits

- `GetBranch` throws an `ArgumentNullException` for a `null` object.
- `SetBranch` returns `false` when a train is on the switch's piece of track, or, for a reversed switch, on the piece before it.
- `SetBranch` returns `false` when the switch has no such branch, and throws an `ArgumentOutOfRangeException` for a negative branch. It also returns `false` when the object is not a switch, and while a save is loading or being written. <span class="tech">(The number of branches is the length of the private `MinitrainSwitch.branchRotations`, read through reflection.)</span>
- `SetBranch`, `SetLoaderOn` and `SetLoaderFreeToTake` throw an `ArgumentNullException` for a `null` object. They return `false` while a save is loading or being written, and `false` when the object is not the right kind of object.
