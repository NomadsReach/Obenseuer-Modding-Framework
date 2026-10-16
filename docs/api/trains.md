---
title: Trains API
parent: API reference
nav_order: 83
---

# Trains API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.trains`.
Files: `src/Api/Trains.cs`, `src/Api/TrainsPatches.cs`.

Everything not listed here is internal. The player's "is driving the minitrain" check is already `Travel.UsingMinitrain` (`docs/api/player.md`), and this area does not repeat it.

## Threading and safety

- Main thread only.
- `Trains.IsAvailable` is false when the `Update` hook was not installed or its owner was disabled. The reads keep working, and `Departed` and `Arrived` stop. If the `Crash` hook is missing, only `Crashed` stops.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list or `null`.
- Reads never throw. The writes in the switches and loaders section return `bool` and return `false` during a save or load. Their only throws are `ArgumentNullException` for a null target and `ArgumentOutOfRangeException` for a negative branch.
- Events are raised synchronously from the game's `TrackTrain.Update`. A handler that throws is logged and the other handlers still run.

## Minitrain

```csharp
public static class Trains
{
    public static MinitrainInfo Minitrain { get; }
}

public sealed class MinitrainInfo
{
    public float Fuel { get; }             // the tank's current fuel amount
    public float Capacity { get; }         // the tank's maximum fuel amount
    public bool Running { get; }           // the minitrain's power switch (isOn)
    public float Speed { get; }            // the driven track train's speed
}
```

- `Trains.Minitrain` is the minitrain the player is using (`Minitrain.playerUsing`, public, `Minitrain`; the game sets it in `ChangePlayerUsingState`, `Minitrain`, and restores it on load). When the player is not using one, it is the first loaded `Minitrain`. It is `null` when none is loaded. `Travel.UsingMinitrain` answers "the player is driving".
- `Fuel` and `Capacity` are `fuelTank.currentFuelAmount` and `fuelTank.maxFuelAmount` (`Minitrain`, `MinitrainFuelTank`). They are `0` when the minitrain has no tank.
- `Running` is `Minitrain.isOn`. The game sets it false itself when the tank runs dry.
- `Speed` is `GetTrain().currentSpeed`.

No write is exposed. `AddFuel(amount)` is not here, because the fuel tank has no method that takes an amount. Its only fill method, `FillTankFromInventory`, takes the fuel out of the player's inventory, which is the game's own refuel action, not a generic add.

## Track trains

```csharp
public static List<TrackTrainInfo> All();

public sealed class TrackTrainInfo
{
    public string Id { get; }              // the train's GUID
    public float Speed { get; }            // currentSpeed, negative when reversing
    public Vector3 Position { get; }       // the train's transform.position
    public string FrontCoupledTo { get; }  // GUID of the train on the front coupler, null when not coupled
    public string BackCoupledTo { get; }   // GUID of the train on the back coupler, null when not coupled
    public string WagonType { get; }       // MinitrainWagonData.wagonType by name, "None" when the train has no wagon data
    public string CargoGuid { get; }       // the wagon's cargo Storage GUID, null when none
}
```

- `All` reads the game's own registry, `TrainController.instance.TrackTrains`, keyed by GUID. It returns an empty list when the controller is not loaded.
- A coupler's partner is `TrainCoupler.coupledTo`, and its train is that coupler's `parent` (`TrackTrain`). A train with no partner on that side gets `null`.
- `WagonType` is the name of `MinitrainWagonData.WagonType`: `None`, `Orewagon`, `Flatbed`, `Tanker` or `Passanger`. The game spells the last one `Passanger`, and the framework passes the name through unchanged, so match that spelling.
- The wagon data is found with `GetComponentInChildren` on the train, inactive objects included. `MinitrainWagonData.Awake` finds its train with `GetComponentInParent<TrackTrain>`, so the data sits on the train or under it. A train with no such component gets `"None"`.
- `CargoGuid` is `connectedStorage.GUID`. The game does not serialize that field. It is set at run time only where the game assigns it (for example `InstantiatedPrefabRef`, as `guid + "storage"`), so it can be empty. An empty GUID is reported as `null`, the same as a missing link. Pass a non-null value to `Containers.FindByGuid` (`docs/api/inventory.md`) to get the cargo container. `connectedStorage` is filled in `MinitrainWagonData.Awake` from `GetComponentInChildren<Storage>`, so a wagon with no storage gets `null`.

## Switches and loaders

```csharp
public static class Trains
{
    public static List<GameObject> Switches { get; }   // every MinitrainSwitch in the active scene
    public static List<GameObject> Loaders { get; }    // every MinitrainWagonLoader in the active scene

    public static int GetBranch(GameObject target);                          // currentBranchIndex, -1 when no switch
    public static bool SetBranch(GameObject target, int branch, bool force);
    public static bool SetLoaderOn(GameObject target, bool on);
    public static bool SetLoaderFreeToTake(GameObject target, bool freeToTake);
}
```

- `GetBranch` reads `MinitrainSwitch.currentBranchIndex`. It throws `ArgumentNullException` for a null `target`, and returns -1 when the object has no `MinitrainSwitch`.
- `SetBranch` with `force` false first runs the game's own occupancy check (`ChangeTrack`, `MinitrainSwitch`): it returns `false` when the switch's track segment is occupied, or when `isReversed` is set and the previous segment is occupied. Otherwise it calls `ChangeTrack(branch)`, and `true` means the change was accepted and the lever animates. `GetBranch` updates when the animation ends (`ChangeTrackCO`), not when the call returns. The game waits for the segment to clear during the animation as well. `force` true calls `ChangeTrackForce(branch)` with no occupancy check. It sets `currentBranchIndex` at once and then animates.
- `SetBranch` returns `false` when `branch` is not less than the number of branches. The count is the length of the private `MinitrainSwitch.branchRotations`, read through reflection. A negative `branch` throws `ArgumentOutOfRangeException`.
- `SetLoaderOn` calls `MinitrainWagonLoader.Toggle(on)`. It returns `true` when `active` equals `on` afterwards.
- `SetLoaderFreeToTake` calls `SetOwnerPermissions(freeToTake)`, which sets `permissionToTake`. When it is `true`, the loader tags each newly stocked item (owner ID above 1) with `ItemOwnerPermission { ownerPermission = true }`, so the player can take it without stealing. It returns `true` when `permissionToTake` equals `freeToTake` afterwards.

```csharp
foreach (GameObject sw in Trains.Switches)
{
    if (Trains.GetBranch(sw) == 0) Trains.SetBranch(sw, 1, force: false);
}
```

## Track segments

```csharp
public static List<TrackSegmentInfo> Segments();

public sealed class TrackSegmentInfo
{
    public string Id { get; }              // the save GUID, or the scene path
    public bool Occupied { get; }          // Track_Segment.occupied
    public string OccupiedBy { get; }      // the occupying train's GUID, null when free
    public int ActiveBranch { get; }       // activeBranchIndex
    public string Previous { get; }        // previousSegment's Id, null when none
    public List<string> Next { get; }      // one entry per branch: that branch's nextSegment Id, null when it has none
}
```

- `Segments` finds every active `Track_Segment` in the active scene with `FindObjectsByType`, as `Switches` does. An inactive segment is not listed. It returns an empty list when no level is loaded.
- `Occupied` and `OccupiedBy` are `Track_Segment.occupied` and `occupiedBy`.
- `ActiveBranch` is `activeBranchIndex`. A minitrain switch writes it when its own branch changes, through `SetBranchIndex` (`MinitrainSwitch` for a forced change). It therefore matches the switch's `currentBranchIndex` once the switch has changed.
- `Previous` is `previousSegment`. `Next` lists each branch's `nextSegment` (`SegmentBranch.nextSegment`) in branch order.
- No write is exposed. `Track_Segment.SetBranchIndex` changes the routing without moving the switch, so a switch and its segment would disagree. Use `SetBranch` for a switch.

## Events

```csharp
public static event Action<TrainArgs> Departed;
public static event Action<TrainArgs> Arrived;
public static event Action<TrainCrashArgs> Crashed;

public sealed class TrainArgs
{
    public string Id { get; }              // the train's GUID
    public float Speed { get; }            // currentSpeed after the change
}

public sealed class TrainCrashArgs
{
    public string Id { get; }              // the crashed train's GUID
    public float Speed { get; }            // the signed speed the train had before the crash stopped it
    public string OtherTrainId { get; }    // the GUID of the train it hit, or null when it hit something else
}
```

- `Departed` fires when a track train's speed goes from `0` to non-zero. `Arrived` fires when it goes from non-zero back to `0`. Both are checked around one `TrackTrain.Update` call, so a change the game makes inside its update is reported (the engine, braking, the driver's throttle, hitting the end of the track).
- `Crashed` fires once per crash. A train-to-train crash runs the game's `TrackTrain.Crash` for both trains, but the second report is dropped: within one frame, a train already named in a reported crash (as the train or as `OtherTrainId`) does not fire again. The first report is the one the game makes inside the other train's `Crash`, usually the train that was hit. Two separate crashes in one frame that share a train report only the first. The game ignores a crash during its cooldown, with a train still coupled to the object, with the same object as the last crash, or with a train already at speed `0`. Those calls fire nothing. `Crashed` does not fire `Arrived`, and the bounce after the crash fires nothing.
- Only the driving unit of a consist reports. A coupled wagon gets its speed written by the driving unit (`ApplyValuesOnWagons`, `TrackTrain`), not inside its own update, so wagons never fire. `Id` is the driving train's GUID.
- `Arrived` does not mean the train reached a station. The game has no station event for track trains, and a stop at the end of the track also counts. `Arrived.Speed` is `0`.
- A train that is activated or loaded without moving does not fire either event. The game's `TrainActivated` is not used for `Departed`, because the game also calls it on scene start.

## Ceilings and risks

- Speed changes made outside `TrackTrain.Update` are not reported by `Departed` or `Arrived`. These are `SetSpeedInstantly`, `ForceStop`, the stop inside `Crash` (a trigger callback, reported by `Crashed` instead) and the bounce after a crash or the end of the track (`CrashBounceback`, a coroutine). The hook compares the speed at the start and end of one update, so a stop written between updates is seen as no change. A forced stop therefore fires no `Arrived`. The end of the track (`EndOfTrackReached`, called inside the update) fires `Arrived` at the impact, then the bounce and its final stop fire nothing.
- Flapping: not expected. The game clamps a stopping train to exactly `0` and a reversing train passes through zero without being exactly `0` at the end of an update, so `0 -> tiny -> 0` is not produced. The check is exactly `!= 0`, with no threshold.
- Read positions and speeds are the values at the moment of the call. The game moves trains in its update, so two reads in one frame can differ.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
