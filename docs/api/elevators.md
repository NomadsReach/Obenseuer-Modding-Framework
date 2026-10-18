---
title: Elevators
parent: Places and travel
grand_parent: API reference
nav_order: 2
---

# Elevators

## What it is

The game has elevators that carry the player between floors. This API lets your mod list the elevators in the scene you are in, and see which floor each car is on and whether it is moving. It can send a car to a floor, and tells you when a car arrives.

## When to use it

- Send an elevator to a floor from your own button or hotkey.
- Find out which floor a car is on.
- Run code when a car stops at a floor.

## Quick example

This mod writes a log line each time an elevator car stops at a floor. Once a save is loaded, it also logs how many elevators the scene has.

```csharp
using ObenseuerCore;

[ModInfo("example.liftwatch", "Lift Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class LiftWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Elevators.Arrived += args => Log.Info("Elevator " + args.Id + " stopped at floor " + args.Floor);
        Events.WhenReady(() => Log.Info("This scene has " + Elevators.All().Count + " elevators."));
    }
}
```

`Arrived` is an event: the framework calls your code each time it happens. Floors count from 0. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.

## Common tasks

List the elevators and where each car is.

```csharp
foreach (ElevatorInfo elevator in Elevators.All())
    Log.Info(elevator.Id + " is on floor " + elevator.CurrentFloor + " of " + elevator.FloorCount + (elevator.Moving ? " and moving." : "."));
```

Send an elevator to a floor. `elevatorId` is the `Id` of an elevator from `Elevators.All()`. `GoToFloor` gives back `false` when the game does not take the call, for example when the car is already moving.

```csharp
if (!Elevators.GoToFloor(elevatorId, 2)) Log.Info("The elevator did not take the call.");
```

Stay safe with a floor number you do not know. A floor outside the elevator's range throws an error, so check it first.

```csharp
ElevatorInfo elevator = Elevators.Get(elevatorId);
if (elevator != null && floor >= 0 && floor < elevator.FloorCount) Elevators.GoToFloor(elevatorId, floor);
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail you can skip on a first read.

### Finding elevators

Each elevator is addressed by an id. `Elevators.All()` and `Elevators.Get(id)` give you an `ElevatorInfo`, a snapshot taken when you asked. The snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Elevators</code> <span>find elevators and read their floors</span></p>

Call `All()` or `Get(id)` to read elevators, and `Floor(id)` or `Moving(id)` to check one car.

<div class="api-pair" markdown="1">

```csharp
public static class Elevators
{
    public static List<ElevatorInfo> All();
    public static ElevatorInfo Get(string id);
    public static int Floor(string id);
    public static bool Moving(string id);
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<ElevatorInfo>` | Every elevator in the scene you are in that has an id. |
| `Get(id)` | `ElevatorInfo` | One elevator. `null` when the id is not known. |
| `Floor(id)` | `int` | The floor the car is on. `0` when the id is not known. |
| `Moving(id)` | `bool` | `true` while the car is moving. `false` when the id is not known. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when the `Arrived` event works. See Details. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ElevatorInfo</code> <span>a snapshot of one elevator</span></p>

`Elevators.All()` and `Get(id)` give you these, one for each elevator that has an id. <span class="tech">(It is built from the game's `Elevator` object.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class ElevatorInfo
{
    public string Id { get; }
    public int FloorCount { get; }
    public int CurrentFloor { get; }
    public bool Moving { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The elevator's id. Use it with the other calls. |
| `FloorCount` | `int` | How many floors it has. |
| `CurrentFloor` | `int` | The floor the car is on, counted from 0. |
| `Moving` | `bool` | `true` when the car is between floors or stopping. |

</div>
</div>

#### Things to know

- An elevator without an id is left out of `All()`, because nothing could address it. The id is the elevator's own save id. <span class="tech">(The id is the `Elevator.GUID` field, and elevators with an empty `GUID` are skipped. `All()` lists them in the order `FindObjectsByType` returns them.)</span>
- Two elevators can share an id, and then you get the first one. Ids are only unique inside one scene, not between scenes.
- `Get`, `Floor`, `Moving` and `GoToFloor` give a plain default for an id the game does not know. `Floor` gives `0` in that case, which is also a real floor, so use `Get` when you need to tell "not found" from "floor 0".
- `CurrentFloor` is the nearest floor to the car. While the car is between floors you get the floor it is closest to. <span class="tech">(It is the car's `Position` rounded to the nearest whole number.)</span>

### Sending an elevator

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Elevators</code> <span>send a car to a floor</span></p>

Call `GoToFloor` with the car's id and the floor you want. It returns `false` when the game does not take the call.

<div class="api-pair" markdown="1">

```csharp
public static class Elevators
{
    public static bool GoToFloor(string id, int floor);
}
```

| Method | Returns | What it does |
|---|---|---|
| `GoToFloor(id, floor)` | `bool` | Asks the car to go to a floor. `true` when the game took the call. |

</div>
</div>

#### Things to know

- `GoToFloor` presses the same call button the player would. The game's own queue, doors and switches then work as usual. <span class="tech">(It calls the game's public `Elevator.Call(int)`, which the call buttons and `MoveToOneFloorUp` and `MoveToOneFloorDown` also use.)</span>
- `GoToFloor` gives `true` when the game put the floor in its queue.

#### Limits

- `GoToFloor` gives `false` when the id is not known, or while a save is loading or being written.
- `GoToFloor` gives `false` when the car is moving. The framework does not interrupt a car.
- `GoToFloor` gives `false` when the floor is the one the car is on. Calling it would only set off that floor's switches and open the doors again, so the framework skips it.
- `GoToFloor` gives `false` when the game refuses the call. This happens when the elevator has no power, when the game has disabled that floor, or when its queue is full. <span class="tech">(It also returns `false` when the framework cannot read the game's private `moving` field or `Queue` list. The queue limit is `QueueMax`.)</span>
- A floor outside the elevator's range throws an error. The error is an `ArgumentException`. The floors of an elevator are `0` up to `FloorCount - 1`, so check with `Get(id).FloorCount` first. An unknown elevator never throws.

### The Arrived event

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Elevators</code> <span>the arrival event</span></p>

Subscribe to `Arrived` to hear when a car stops at a floor.

<div class="api-pair" markdown="1">

```csharp
public static class Elevators
{
    public static event Action<ElevatorArrivedArgs> Arrived;
}
```

| Event | Gives you | When it fires |
|---|---|---|
| `Arrived` | `ElevatorArrivedArgs` | A car has just stopped at a floor. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ElevatorArrivedArgs</code> <span>what happened</span></p>

`Arrived` gives you one of these with the car's `Id` and the `Floor` it stopped at.

<div class="api-pair" markdown="1">

```csharp
public sealed class ElevatorArrivedArgs
{
    public string Id { get; }
    public int Floor { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The elevator's id, the same as `ElevatorInfo.Id`. |
| `Floor` | `int` | The floor the car stopped at. |

</div>
</div>

#### Things to know

- `Arrived` fires once each time a car comes to a stop at a floor. If a save was made while the car was between floors, it fires when the car stops after you load. <span class="tech">(It fires when the game's `moving` value goes from non-zero to zero inside `FixedUpdate`, after the game has stopped the car. `Floor` is the rounded position at that moment.)</span>

#### Limits

- `Arrived` does not fire when a save loads or when a car is put straight on a floor. The car did not stop from moving, so it is not an arrival. <span class="tech">(The game's `OnFloorreached` call from `SetPosition` is not a stop from motion.)</span>
- `Arrived` does not fire when the power is cut while the car is moving. The car stops, but it did not arrive. <span class="tech">(`TogglePower` clears `moving` outside `FixedUpdate`.)</span>
- `Arrived` only comes from the game's own movement. A car moved some other way would not raise it, and no such way is known.

## Details

- Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
- Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.elevators.log`.
- Everything not listed in this reference is internal and may change without notice.

### If something fails to set up

The framework changes one of the game's methods when it starts, so it can tell your mod when a car arrives. If a game update breaks it, only the `Arrived` event turns off, and the framework writes error lines and a warning to the log. The reads and `GoToFloor` keep working. The change belongs to a feature group, `obenseuer.moddingframework.elevators`, the name you will see in the log.

- `Elevators.IsAvailable` tells you whether `Arrived` works. It is `false` when the change could not be made or the feature group was turned off.
- `GoToFloor` and `Moving` need private game values. If the framework cannot find the car's `moving` value when it starts, `Moving` gives `false` for every elevator. If it cannot find that value or the car's `Queue` list, `GoToFloor` gives `false` for every elevator. <span class="tech">(Both are read through reflection. They are the protected `Elevator.moving` field and `Elevator.Queue` list. The game's own movement is not changed.)</span>

### When you can call it

- Call everything from your mod's normal code (OnLoad, event handlers, Update), not from a background thread you started. The game only runs safely on the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call any of it at any time, even on the main menu. With no elevator loaded, the reads give an empty list, `null`, `0` or `false`.
- The only call that can throw is `GoToFloor`, for a floor outside the range of a known elevator.
- Your `Arrived` handlers run inside the game's physics update, one at a time. The game has already stopped the car by then. If a handler throws an error, the framework logs it and still runs the other handlers.

### Limits

- Two elevators with the same id give you the first one. Ids written into the game's scenes are unique inside a scene, not across scenes.

### Save loads

- The framework does not hold back the `Arrived` event while a save loads. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is `true` while a save is loading or being written.
