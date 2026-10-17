---
title: Events
parent: Trains
grand_parent: Places and travel
nav_order: 6
---

# Events

The events tell your mod when a train sets off, stops or crashes.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Trains</code> <span>train events and status</span></p>
Use these events to hear when a train departs, arrives or crashes.

<div class="api-pair" markdown="1">

```csharp
public static class Trains
{
    public static bool IsAvailable { get; }
    public static event Action<TrainArgs> Departed;
    public static event Action<TrainArgs> Arrived;
    public static event Action<TrainCrashArgs> Crashed;
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the set-off and stop events could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Departed` | `TrainArgs`: the train's id and its speed right after the change. | A train set off. |
| `Arrived` | `TrainArgs`: the train's id, and its speed, which is `0`. | A train stopped. |
| `Crashed` | `TrainCrashArgs`: the crashed train's id, its speed before the crash, and the id of the train it hit. | A train crashed. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TrainArgs</code> <span>what Departed and Arrived tell you</span></p>
`Departed` fires when a train starts moving and `Arrived` when it stops, and both give you this. `Speed` is the train's speed at that moment.

<div class="api-pair" markdown="1">

```csharp
public sealed class TrainArgs
{
    public string Id { get; }
    public float Speed { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The train's id. |
| `Speed` | `float` | Its speed right after the change. `0` for `Arrived`. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TrainCrashArgs</code> <span>what Crashed tells you</span></p>
`Crashed` gives you this when a moving train stops in a crash. `OtherTrainId` names the train it hit, or is `null` when the object it hit is not a train.

<div class="api-pair" markdown="1">

```csharp
public sealed class TrainCrashArgs
{
    public string Id { get; }
    public float Speed { get; }
    public string OtherTrainId { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The id of the train that crashed. |
| `Speed` | `float` | The speed it had before the crash stopped it. Negative when reversing. |
| `OtherTrainId` | `string` | The id of the train it hit. `null` when it hit something else. |

</div>
</div>

## Things to know

- `Departed` fires when a train starts moving, and `Arrived` fires when it stops. They fire for changes the game makes during the train's normal movement: its engine, braking, the driver's throttle, or the end of the track. <span class="tech">(Both are checked around one `TrackTrain.Update` call. `Departed` is a speed that goes from `0` to non-zero. `Arrived` is the reverse. The check is exactly `!= 0`, with no threshold.)</span>
- Only the driving unit of a joined group of trains reports. The joined wagons get their speed from the driving unit, so they never fire an event of their own. The `Id` you get is that of the driving train. <span class="tech">(Wagon speeds are written by `ApplyValuesOnWagons`, `TrackTrain`, not inside their own update.)</span>
- `Arrived` does not mean the train reached a station. The game has no station event for track trains, and a stop at the end of the track counts as well.
- A train that is switched on or loaded without moving fires nothing. <span class="tech">(The game's `TrainActivated` is not used for `Departed`, because the game also calls it at scene start.)</span>
- A train is not expected to flap between set-off and stop. The game sets a stopping train to exactly `0`, and a reversing train passes through zero without being exactly `0` at the end of an update, so a quick `0`, tiny, `0` is not produced.
- `Crashed` fires once for each crash. When two trains crash, the game reports the crash for both trains, but the framework drops the second report: within one frame, a train that was already named in a reported crash does not fire again. The report you get is the one for the train that was hit. <span class="tech">(It is raised after `TrackTrain.Crash`, when the speed before was not `0` and the speed after is `0`. The first report is the one the game makes inside the other train's `Crash`, usually the train that was hit. The game ignores a crash during its cooldown, with a train still coupled to the object, with the same object as the last crash, or with a train already at speed `0`. Those calls fire nothing.)</span>
- `IsAvailable` tells you whether `Departed` and `Arrived` work. It is `false` when the framework could not set them up. The reads keep working. If only the crash event could not be set up, only `Crashed` stops and `IsAvailable` stays `true`.
- The values you read are the values at the moment you asked. The game moves trains while it runs, so two reads in one frame can differ.

## Limits

- A stop that your mod or the game forces between two updates does not fire `Arrived`. The framework compares the speed at the start and end of one update, so a speed set in between looks like no change. <span class="tech">(The calls not seen are `SetSpeedInstantly`, `ForceStop`, the stop inside `Crash` (reported by `Crashed` instead) and `CrashBounceback`, a coroutine. `EndOfTrackReached` is called inside the update, so it fires `Arrived`.)</span>
- That covers an instant speed change, a forced stop, and the bounce after a crash or at the end of the track. A stop at the impact with the end of the track does fire `Arrived`, but the bounce and its final stop fire nothing.
- Two separate crashes in one frame that share a train report only the first. The bounce after a crash fires nothing, and `Crashed` does not fire `Arrived`.
