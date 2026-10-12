# Elevators API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.elevators`.
Files: `src/Api/Elevators.cs`, `src/Api/ElevatorsPatches.cs`.

Everything not listed here is internal.

## Wiring

```csharp
internal static void Elevators.Init();
```

Core calls this from `Core.Awake`.

`Init` installs the `Arrived` hook under one owner. If that hook is missing, it is removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.elevators")` becomes true, and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.elevators.log`. The reads and `GoToFloor` keep working; only `Arrived` stops. `GoToFloor` also needs the game's private `moving` and `Queue` fields to be readable. If either is missing at `Init`, `GoToFloor` returns `false` and `Moving` returns `false`.

## Threading and safety

- Main thread only.
- `Elevators.IsAvailable` is false when the hook was not installed or the owner was disabled.
- Every member is safe at any time. With no elevator loaded, or on the main menu, reads return an empty list, `null`, `0` or `false`.
- Writes return `bool`. The only throw is `ArgumentException` for a floor outside `0..FloorCount-1` on a known elevator.
- `Arrived` is raised synchronously, from the game's physics step (`FixedUpdate`), after the game has stopped the car. A handler that throws is logged and the other handlers still run (`Events.Raise`).

## Elevators

```csharp
public static class Elevators
{
    public static List<ElevatorInfo> All();
    public static ElevatorInfo Get(string id);
    public static int Floor(string id);
    public static bool Moving(string id);
    public static bool GoToFloor(string id, int floor);

    public static event Action<ElevatorArrivedArgs> Arrived;

    public static bool IsAvailable { get; }
}

public sealed class ElevatorInfo
{
    public string Id { get; }              // the elevator's GUID
    public int FloorCount { get; }         // Floors.Length
    public int CurrentFloor { get; }       // Position rounded to the nearest floor
    public bool Moving { get; }            // the car is between floors or stopping
}

public sealed class ElevatorArrivedArgs
{
    public string Id { get; }              // the elevator's GUID
    public int Floor { get; }              // the floor the car stopped at
}
```

- `id` is the elevator's `GUID` field (`Elevator.GUID`, `Elevator.cs:26`). Elevators with an empty `GUID` are left out of `All`, because nothing can address them.
- `All` returns every loaded elevator with a GUID, in the order `FindObjectsByType` returns them.
- `Get`, `Floor`, `Moving` and `GoToFloor` return the neutral default for an unknown or empty id. `Floor` returns `0` in that case, so use `Get` to tell "unknown" apart from "floor 0".
- `GoToFloor` returns `true` when the game has queued the floor. It returns `false` when:
  - the car is moving (the car is not interrupted);
  - the floor is the current floor (`Call` is not made: the game would only re-fire that floor's relays and open the doors, `Elevator.cs:401`);
  - the game refuses the call (unpowered, the floor is disabled by the game, or the queue is full, `QueueMax`).
- `GoToFloor` calls the game's public `Elevator.Call(int)` (`Elevator.cs:374`), the same method the call buttons and `MoveToOneFloorUp`/`Down` use (`Elevator.cs:358`, `:366`). The game's own queue, doors and relays run as usual. A floor outside `0..FloorCount-1` throws `ArgumentException`.
- `Arrived` is raised when the car stops at a floor: the game's `moving` goes from non-zero to zero inside `FixedUpdate` (`Elevator.cs:183`, the stop branch at `:225`). `ElevatorArrivedArgs.Floor` is the rounded position at that moment.
- `Arrived` fires once per stop. A car saved mid-travel reports its `Arrived` when it stops after the load.
- `Arrived` does not fire on load or on `SetPosition`, because the game's `OnFloorreached` call there is not a stop from motion.
- `Arrived` does not fire when power is cut mid-travel (`TogglePower`, `Elevator.cs:492`, clears `moving` outside `FixedUpdate`). That is not an arrival.

## Ceilings and risks

- `Moving` and `GoToFloor` read the game's protected `moving` field and protected `Queue` list through reflection (`src/Api/Elevators.cs:Elevators.MovingField`, `Elevators.QueueField`). They do not change anything the game does not already change. Not verified in game.
- Two elevators with the same GUID resolve to the first one found. Authored GUIDs are unique per scene, not across scenes.
- Elevator events only come from the game's motion. A car that the game moves without `FixedUpdate` (none is known) would not raise `Arrived`.

## Smoke test

1. Load a save with an elevator in the scene. In the BepInEx console or a small test plugin, call `Elevators.All()` and check that `FloorCount` matches the number of floors you can see.
2. Call `Elevators.GoToFloor(id, other)` on a floor the car is not on, with the car idle. It should return `true`, the car should start moving, and `Elevators.Moving(id)` should be `true`.
3. While it moves, call `GoToFloor` again. It should return `false` and the car should keep going.
4. When the car stops, `Arrived` should fire once with `Floor` equal to the target floor. Subscribe with `Elevators.Arrived += a => Log(a.Id + " " + a.Floor);`.
5. Reload the save. `Arrived` should not fire during the load.
