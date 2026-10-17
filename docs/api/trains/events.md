---
title: Events
parent: Trains
grand_parent: Places and travel
nav_order: 6
---

# Events

```csharp
public static class Trains
{
    public static bool IsAvailable { get; }                // false when the set-off and stop events could not be set up
    public static event Action<TrainArgs> Departed;        // a train set off
    public static event Action<TrainArgs> Arrived;         // a train stopped
    public static event Action<TrainCrashArgs> Crashed;    // a train crashed
}

public sealed class TrainArgs    // what Departed and Arrived tell you
{
    public string Id { get; }       // the train's id
    public float Speed { get; }     // its speed right after the change; 0 for Arrived
}

public sealed class TrainCrashArgs    // what Crashed tells you
{
    public string Id { get; }              // the id of the train that crashed
    public float Speed { get; }            // the speed it had before the crash stopped it; negative when reversing
    public string OtherTrainId { get; }    // the id of the train it hit; null when it hit something else
}
```

- **`Departed` fires when a train starts moving, and `Arrived` fires when it stops.** They fire for changes the game makes during the train's normal movement: its engine, braking, the driver's throttle, or the end of the track.
  - Technical: both are checked around one `TrackTrain.Update` call. `Departed` is a speed that goes from `0` to non-zero. `Arrived` is the reverse. The check is exactly `!= 0`, with no threshold.
- **Only the driving unit of a joined group of trains reports.** The joined wagons get their speed from the driving unit, so they never fire an event of their own. The `Id` you get is that of the driving train.
  - Technical: wagon speeds are written by `ApplyValuesOnWagons`, `TrackTrain`, not inside their own update.
- **`Arrived` does not mean the train reached a station.** The game has no station event for track trains, and a stop at the end of the track counts as well.
- **A train that is switched on or loaded without moving fires nothing.**
  - Technical: the game's `TrainActivated` is not used for `Departed`, because the game also calls it at scene start.
- **A stop that your mod or the game forces between two updates does not fire `Arrived`.** The framework compares the speed at the start and end of one update, so a speed set in between looks like no change. That covers an instant speed change, a forced stop, and the bounce after a crash or at the end of the track. A stop at the impact with the end of the track does fire `Arrived`, but the bounce and its final stop fire nothing.
  - Technical: the calls not seen are `SetSpeedInstantly`, `ForceStop`, the stop inside `Crash` (reported by `Crashed` instead) and `CrashBounceback`, a coroutine. `EndOfTrackReached` is called inside the update, so it fires `Arrived`.
- **A train is not expected to flap between set-off and stop.** The game sets a stopping train to exactly `0`, and a reversing train passes through zero without being exactly `0` at the end of an update, so a quick `0`, tiny, `0` is not produced.
- **`Crashed` fires once for each crash.** When two trains crash, the game reports the crash for both trains, but the framework drops the second report: within one frame, a train that was already named in a reported crash does not fire again. The report you get is the one for the train that was hit. Two separate crashes in one frame that share a train report only the first. The bounce after a crash fires nothing, and `Crashed` does not fire `Arrived`.
  - Technical: it is raised after `TrackTrain.Crash`, when the speed before was not `0` and the speed after is `0`. The first report is the one the game makes inside the other train's `Crash`, usually the train that was hit. The game ignores a crash during its cooldown, with a train still coupled to the object, with the same object as the last crash, or with a train already at speed `0`. Those calls fire nothing.
- **`IsAvailable` tells you whether `Departed` and `Arrived` work.** It is `false` when the framework could not set them up. The reads keep working. If only the crash event could not be set up, only `Crashed` stops and `IsAvailable` stays `true`.
- **The values you read are the values at the moment you asked.** The game moves trains while it runs, so two reads in one frame can differ.
