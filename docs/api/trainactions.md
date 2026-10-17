---
title: Train actions
parent: Places and travel
grand_parent: API reference
nav_order: 15
---

# Train actions

## What it is

The game has trains that run on tracks. This API lets your mod drive them: turn an engine on or off, set how fast the train should go, apply the brakes, and join or separate two cars. You pick a train with its id, which you get from `Trains.All()` (see the [Trains API](trains.md)).

## When to use it

- Start a train when something happens in your mod, such as the player buying a ticket.
- Stop a train with the brakes, or send it off at a speed you choose.
- Join two cars so they run together, or split them again.

## Quick example

This mod waits until a save is loaded. It then turns on the engine of every train and sets its target speed to 10. It writes to the log which trains started.

```csharp
using ObenseuerCore;

[ModInfo("example.trainstart", "Train Start", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TrainStart : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (TrackTrainInfo train in Trains.All())
            {
                bool started = TrainActions.SetEngine(Mod.Id, train.Id, true);
                TrainActions.SetTargetSpeed(Mod.Id, train.Id, 10f);
                Log.Info("Train " + train.Id + (started ? " started" : " could not be started"));
            }
        });
    }
}
```

`WhenReady` waits until a save is loaded before it runs your code. See [Using events](../guides/getting-started/using-events.md) for more on it.

## Common tasks

The snippets below use the ids of two cars (`frontCarId` and `rearCarId`) that you got from `Trains.All()`. Apply the brakes and let them go again.

```csharp
TrainActions.Brake(Mod.Id, frontCarId, true);
TrainActions.Brake(Mod.Id, frontCarId, false);
```

Join the back of one car to the front of another, and separate them again. `Couple` only works when the two cars are already touching.

```csharp
if (TrainActions.Couple(Mod.Id, frontCarId, TrainEnd.Back, rearCarId, TrainEnd.Front))
    Log.Info("The cars are joined.");

TrainActions.Release(Mod.Id, frontCarId, TrainEnd.Back);
TrainActions.SetEngine(Mod.Id, frontCarId, false);
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TrainEnd</code> <span>the two ends of a car</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum TrainEnd
{
    Front = 0,
    Back = 1
}
```

| Value | Meaning |
|---|---|
| `Front` | The front end of a car. |
| `Back` | The back end of a car. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TrainActions</code> <span>drive trains and join or split cars</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class TrainActions
{
    public static bool SetEngine(string modId, string trainId, bool on);
    public static bool SetTargetSpeed(string modId, string trainId, float speed);
    public static bool Brake(string modId, string trainId, bool on);
    public static bool Couple(string modId, string trainId, TrainEnd end, string otherTrainId, TrainEnd otherEnd);
    public static bool Release(string modId, string trainId, TrainEnd end);
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetEngine(string modId, string trainId, bool on)` | `bool` | Turns the engine on or off. `true` once the request is made. |
| `SetTargetSpeed(string modId, string trainId, float speed)` | `bool` | Sets how fast the train should go. `true` when it was set. |
| `Brake(string modId, string trainId, bool on)` | `bool` | Applies or releases the brakes. `true` when the brakes are now in the state you asked for. |
| `Couple(string modId, string trainId, TrainEnd end, string otherTrainId, TrainEnd otherEnd)` | `bool` | Joins one end of a car to one end of another car. `true` when they are joined. |
| `Release(string modId, string trainId, TrainEnd end)` | `bool` | Separates a car at one end. `true` when that end is free afterwards. |

</div>
</div>

### Things to know

- `SetEngine` turns an engine on or off. It returns `true` once the request has been made. A train that belongs to a minitrain (the small train the player rides) starts only after the game's own start-up delay. The lights and the dashboard follow, as they do when the player starts it. <span class="tech">(for a minitrain it uses the game's own `StartMinitrain` and `StopMinitrain`. Other trains use `TrackTrain.Toggle`.)</span>
- `SetTargetSpeed` sets how fast the train should go, and it can't go faster than the train allows. The speed is held between the train's own highest forward and highest reverse speed.
- `Brake` applies or releases the brakes, and the game's brake outputs still fire. It returns `true` only when the brakes are really in the state you asked for. <span class="tech">(it calls the game's `ApplyBrakes`, so the game's braking outputs still fire.)</span>
- `Couple` joins one end of a train to one end of another, and returns `true` when they are joined. It does what the game does when two cars touch and their couplers meet. It changes nothing and returns `false` unless all of these are true:
  - the two cars are different, and they do not already share a wagon;
  - each end has a coupler, and neither end is already joined to something;
  - both couplers are ready, which means they are switched on and not in their short pause after a crash;
  - the two cars are already touching;
  - each car is moving no faster than its own coupler allows, when the coupler has a limit.
  - <span class="tech">(it calls the game's `TrainCoupler.Couple`. The couplers' triggers must be active and not in their crash cooldown. "Touching" means the bounds of the two triggers' colliders overlap. The speed limit is the absolute speed of each train against its own coupler trigger's `maxSpeed`, when that is above 0.)</span>
- `Release` separates a car at one end, and returns `true` when that end is free. It splits the train and plays the uncoupling sound. It returns `false` when nothing is joined at that end. <span class="tech">(it calls the game's `TrainCoupler.Release`.)</span>
- The game finishes the join after `Couple`. The wagon list, the engine that drives the string, and the way the cars face are all worked out by the game's own join and split. After a join, the game moves the car that is not driving until the ends meet. `Couple` only joins cars that are already touching, so that gap is small. A join made by your mod is saved the same way as one the player makes.
- `Couple` returning `true` does not mean the join lasts. The game can release it on a later update if the gap is over the coupler's allowed distance. Read `FrontCoupledTo` or `BackCoupledTo` from `Trains.All()` to see whether the join is still there. <span class="tech">(the distance is the coupler's `maxDistance`, when that is above 0.)</span>
- `Couple` can say yes to cars that only look close. The test uses a box around each coupler, so two boxes can overlap when the shapes inside them do not touch. The game then decides, as it does for its own couplers. <span class="tech">(it uses `Collider.bounds`, which are axis-aligned boxes.)</span>
- `Couple` is a little stricter than the game. It checks each car against its own coupler's speed limit, using the absolute speed, so it refuses some joins the game allows, such as a train moving backward. <span class="tech">(the game's trigger tests the signed speed.)</span>
- Your mod can interfere with a train the player is driving. It can start or stop that train, and it can join or separate a joint the player made. The framework keeps no record of who changed a train, so there is no owner to check.

### Limits

- Every call returns `false` and does nothing when it can't do its job. That is for an empty mod id, an unknown or destroyed train, a speed that is not a real number, and while the game is loading or saving a save. `Couple` and `Release` also need a valid mod id: 1 to 64 characters of letters, digits, `.`, `_` or `-`. The other three accept any mod id that is not empty. <span class="tech">(the checks are `SaveData.Busy`, the train lookup in `TrainController.TrackTrains`, and for the speed `float.IsNaN` and `float.IsInfinity`.)</span>
- A train without fuel reports a target speed of 0 in the game, so setting a speed does not move it.

### What you can't do (yet)

- You can't add a car to a train. The train shop does that. `Couple` only joins two cars that are already in the world.
- You can't slide a car toward another one. `Couple` does not move a car. The game's own correction does that, and only over a gap that is already small.
- You can't handle a crash from this page. Use `Trains.Crashed` (see the [Trains API](trains.md)).

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.trainactions.log`.

Everything not listed in this reference is internal and may change without notice.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only works safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- Train state belongs to the game, and the game saves it. The framework keeps nothing of its own.

### If something fails to set up

This API does not change any game method, so there is no feature group to turn off, and nothing for the framework to set up. If the game fails while one of the calls runs, the framework writes one error to the log and the call returns `false`.
