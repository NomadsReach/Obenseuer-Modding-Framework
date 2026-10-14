---
title: TrainActions API
parent: API reference
nav_order: 96
---

# TrainActions API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. File: `src/Api/TrainActions.cs`.

This area drives track trains from a mod: engine on or off, target speed, brakes, and joining or releasing one end of a car to one end of another. Trains are found by their id from `Trains.All()` (`TrackTrainInfo.Id`).

## If a hook cannot be installed

This facade has no hooks.

## API

```csharp
public enum TrainEnd { Front = 0, Back = 1 }

public static class TrainActions
{
    public static bool SetEngine(string modId, string trainId, bool on);
    public static bool SetTargetSpeed(string modId, string trainId, float speed);
    public static bool Brake(string modId, string trainId, bool on);
    public static bool Couple(string modId, string trainId, TrainEnd end, string otherTrainId, TrainEnd otherEnd);
    public static bool Release(string modId, string trainId, TrainEnd end);
}
```

- Each call returns `false` for an empty mod id, an unknown train, a destroyed train, a NaN or infinite speed, or `SaveData.Busy`. `SetEngine`, `SetTargetSpeed` and `Brake` accept any non-empty mod id. `Couple` and `Release` also require a valid mod id: 1 to 64 characters of letters, digits, `.`, `_` or `-`.
- `SetEngine` returns `true` once the request is made. A train that belongs to a minitrain starts after the game's own start-up delay.
- `SetTargetSpeed` is clamped to the train's own maximum forward and reverse speed.
- `Brake` returns `true` only when the train's brake state is now the value asked for.
- `Couple` joins one end of a train to one end of another train, and returns `true` when the two couplers are now joined. It calls the game's `TrainCoupler.Couple`, the call the coupler trigger makes when two couplers touch. It changes nothing and returns `false` unless all of these hold:
  - the two trains are different trains, and they share no wagon;
  - each end is a coupler the train has, and neither end is already joined to anything;
  - both couplers' triggers are active and not in their crash cooldown;
  - the two triggers' colliders have overlapping bounds, so the cars already touch;
  - each train's absolute speed is at most its own coupler trigger's `maxSpeed`, when that is above 0.
- `Release` ends the joint at `end` and returns `true` when that end is free. It calls the game's `TrainCoupler.Release`, which splits the train and plays the uncoupling sound. It returns `false` when nothing is joined at that end.

## Example

```csharp
foreach (TrackTrainInfo train in Trains.All())
{
    TrainActions.SetEngine("com.example.mymod", train.Id, true);
    TrainActions.SetTargetSpeed("com.example.mymod", train.Id, 10f);
    TrainActions.Brake("com.example.mymod", train.Id, false);
}

TrainActions.Couple("com.example.mymod", frontCarId, TrainEnd.Back, rearCarId, TrainEnd.Front);
TrainActions.Release("com.example.mymod", frontCarId, TrainEnd.Back);
```

## Behaviour

- `SetEngine` uses the game's own minitrain start and stop when the train belongs to a minitrain, so the lights and the dashboard follow. Other trains use the game's `TrackTrain.Toggle`.
- A train without fuel reports a target speed of 0 in the game, so `SetTargetSpeed` does not move it.
- `Brake` uses the game's `ApplyBrakes`, so the game's braking outputs still fire.
- `Couple` and `Release` call the game's own `TrainCoupler` methods, so the wagon list, the driving locomotive and the orientation are the game's own merge and split.
- After a joint is made, the game moves the car that is not driving until the ends meet. `Couple` only joins cars whose triggers overlap, so the gap is small when it is made. A joint made here is saved the same way as one the player makes.

## Not exposed

- Adding a car to a train. The train shop does that, and `Couple` only joins two cars that are already in the world.
- Moving a car toward another. `Couple` does not slide a car. The game's own correction does that, and only over a gap that is already small.
- Crash handling. Use `Trains.Crashed` (see Trains).

## Ceilings and risks

- A mod can start or stop a train that a player is driving, and it can join or release a joint a player made. No owner is kept, as with `SetEngine`.
- `Couple` returns `true` when the joint is made. The game can release it on a later update if the gap is over the coupler's `maxDistance` (when that is above 0). Read `TrackTrainInfo.FrontCoupledTo` or `BackCoupledTo` from `Trains.All()` to see whether the joint is still there.
- The touching test uses the trigger colliders' axis-aligned bounds (`Collider.bounds`). Two colliders whose boxes overlap but whose shapes do not touch pass it. The game then decides, as it does for its own triggers.
- `Couple` is slightly stricter than the game's own coupling. Its speed test uses each train's absolute speed, and it checks each train against its own coupler trigger's `maxSpeed`, not the other train's. The game's trigger tests the signed speed, so this refuses some couplings the game allows, such as a train moving backward.

## Save loads

Nothing is kept by the framework. Train state is the game's own and is saved by the game.
