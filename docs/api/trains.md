# Trains API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.trains`.
Files: `src/Api/Trains.cs`, `src/Api/TrainsPatches.cs`.

Everything not listed here is internal. The player's "is driving the minitrain" check is already `Travel.UsingMinitrain` (`docs/api/player.md`), and this area does not repeat it.

## Wiring

```csharp
internal static void Trains.Init();
```

Core calls this from `Core.Awake`.

`Init` installs the `TrackTrain.Update` hook under one owner. If that hook is missing, it is removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.trains")` becomes true, and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.trains.log`. The reads keep working; only `Departed` and `Arrived` stop.

## Threading and safety

- Main thread only.
- `Trains.IsAvailable` is false when the hook was not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list or `null`.
- Reads never throw. The writes in the switches and loaders section return `bool` and return `false` during a save or load. Their only throws are `ArgumentNullException` for a null target and `ArgumentOutOfRangeException` for a negative branch.
- Events are raised synchronously from the game's `TrackTrain.Update`. A handler that throws is logged and the other handlers still run (`Events.Raise`).

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

- `Trains.Minitrain` is the minitrain the player is using (`Minitrain.playerUsing`, public, `Minitrain.cs:15`; the game sets it in `ChangePlayerUsingState`, `Minitrain.cs:385`, and restores it on load). When the player is not using one, it is the first loaded `Minitrain`. It is `null` when none is loaded. `Travel.UsingMinitrain` answers "the player is driving".
- `Fuel` and `Capacity` are `fuelTank.currentFuelAmount` and `fuelTank.maxFuelAmount` (`Minitrain.cs:21`, `MinitrainFuelTank.cs:12`, `:20`). They are `0` when the minitrain has no tank.
- `Running` is `Minitrain.isOn` (`Minitrain.cs:17`). The game sets it false itself when the tank runs dry.
- `Speed` is `GetTrain().currentSpeed` (`Minitrain.cs:531`, `TrackTrain.cs:712`).

No write is exposed. `AddFuel(amount)` is not here, because the fuel tank has no method that takes an amount. Its only fill method, `FillTankFromInventory` (`MinitrainFuelTank.cs:116`), takes the fuel out of the player's inventory, which is the game's own refuel action, not a generic add.

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
}
```

- `All` reads the game's own registry, `TrainController.instance.TrackTrains` (`TrainController.cs:17`), keyed by GUID. It returns an empty list when the controller is not loaded.
- A coupler's partner is `TrainCoupler.coupledTo`, and its train is that coupler's `parent` (`TrackTrain.cs:145`, `:116`). A train with no partner on that side gets `null`.

## Switches and loaders

```csharp
public static class Trains
{
    public static List<GameObject> Switches { get; }   // every MinitrainSwitch in the scene
    public static List<GameObject> Loaders { get; }    // every MinitrainWagonLoader in the scene

    public static int GetBranch(GameObject target);                          // currentBranchIndex, -1 when no switch
    public static bool SetBranch(GameObject target, int branch, bool force);
    public static bool SetLoaderOn(GameObject target, bool on);
    public static bool SetLoaderFreeToTake(GameObject target, bool freeToTake);
}
```

- `GetBranch` reads `MinitrainSwitch.currentBranchIndex` (`World/MinitrainSwitch.cs:25`).
- `SetBranch` with `force` false first runs the game's own occupancy check (`ChangeTrack`, `MinitrainSwitch.cs:112`, `:114`): it returns `false` when the switch's track segment is occupied, or when `isReversed` is set and the previous segment is occupied. Otherwise it calls `ChangeTrack(branch)`, and `true` means the change was accepted and the lever animates. `GetBranch` updates when the animation ends (`ChangeTrackCO`, `:207`), not when the call returns. The game waits for the segment to clear during the animation as well. `force` true calls `ChangeTrackForce(branch)` (`:133`) with no occupancy check. It sets `currentBranchIndex` at once and then animates.
- `SetBranch` returns `false` when `branch` is not less than the number of branches. The count is the length of the private `MinitrainSwitch.branchRotations` (`World/MinitrainSwitch.cs:16`), read through reflection. A negative `branch` throws `ArgumentOutOfRangeException`.
- `SetLoaderOn` calls `MinitrainWagonLoader.Toggle(on)` (`World/MinitrainWagonLoader.cs:220`). It returns `true` when `active` equals `on` afterwards.
- `SetLoaderFreeToTake` calls `SetOwnerPermissions(freeToTake)` (`World/MinitrainWagonLoader.cs:230`), which sets `permissionToTake`. When it is `true`, the loader tags each newly stocked item (owner ID above 1) with `ItemOwnerPermission { ownerPermission = true }` (`MinitrainWagonLoader.cs:421-426`), so the player can take it without stealing. It returns `true` when `permissionToTake` equals `freeToTake` afterwards.

```csharp
foreach (GameObject sw in Trains.Switches)
{
    if (GetBranch(sw) == 0) SetBranch(sw, 1, force: false);
}
```

## Events

```csharp
public static event Action<TrainArgs> Departed;
public static event Action<TrainArgs> Arrived;

public sealed class TrainArgs
{
    public string Id { get; }              // the train's GUID
    public float Speed { get; }            // currentSpeed after the change
}
```

- `Departed` fires when a track train's speed goes from `0` to non-zero. `Arrived` fires when it goes from non-zero back to `0`. Both are checked around one `TrackTrain.Update` call (`TrackTrain.cs:1316`), so a change the game makes inside its update is reported (the engine, braking, the driver's throttle, hitting the end of the track).
- Only the driving unit of a consist reports. A coupled wagon gets its speed written by the driving unit (`ApplyValuesOnWagons`, `TrackTrain.cs:1364`), not inside its own update, so wagons never fire. `Id` is the driving train's GUID.
- `Arrived` does not mean the train reached a station. The game has no station event for track trains, and a stop at the end of the track also counts. `Arrived.Speed` is `0`.
- A train that is activated or loaded without moving does not fire either event. The game's `TrainActivated` (`TrackTrain.cs:1196`) is not used for `Departed`, because the game also calls it on scene start.

## Ceilings and risks

- Speed changes made outside `TrackTrain.Update` are not reported. These are `SetSpeedInstantly` (`TrackTrain.cs:1099`), `ForceStop` (`:1130`), `Crash` (`:1549`, a trigger callback, speed at `:1620`) and the bounce after a crash or the end of the track (`CrashBounceback`, `:1633`, a coroutine). The hook compares the speed at the start and end of one update, so a stop written between updates is seen as no change. A forced stop therefore fires no `Arrived`. A crash fires nothing. The end of the track (`EndOfTrackReached`, `:1648`, called inside the update) fires `Arrived` at the impact, then the bounce and its final stop fire nothing.
- Flapping: not expected. The game clamps a stopping train to exactly `0` (`TrackTrain.cs:1432`, `:1440`, `:1449`, `:1457`, `:1465`, `:1473`) and a reversing train passes through zero without being exactly `0` at the end of an update, so `0 -> tiny -> 0` is not produced. The check is exactly `!= 0`, with no threshold. Not tested in game.
- Read positions and speeds are the values at the moment of the call. The game moves trains in its update, so two reads in one frame can differ.

## Smoke test

1. Stand at the minitrain's fuel tank with a save loaded. `Trains.Minitrain` should be non-null. `Fuel` should match the gauge, and `Capacity` should be the tank's maximum.
2. Start the minitrain with the game's own controls. `Running` should become `true`, and `Speed` should rise when you push the lever forward.
3. Subscribe with `Trains.Departed += a => Log("depart " + a.Id);` and `Trains.Arrived += a => Log("arrive " + a.Id);`. Drive, then stop with the brake. `depart` should be logged once when the train starts moving and `arrive` once when it stops.
4. `Trains.All()` should list every track train. Couple two wagons, and `FrontCoupledTo` or `BackCoupledTo` should show the other wagon's GUID.
5. Reload the save. Neither event should fire during the load.
