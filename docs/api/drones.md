# Drones API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.drones`.
Files: `src/Api/Drones.cs`, `src/Api/DronesPatches.cs`.

Everything not listed here is internal.

## Wiring

```csharp
internal static void Drones.Init();
```

Core calls this from `Core.Awake`.

`Init` installs the `Changed` hooks under one owner (`TurnOn` and `TurnOff` on `Drone`). Both are needed. If either is missing, the owner is removed, `Drones.IsAvailable` becomes `false`, and the error is logged to `BepInEx\logs\obenseuer.moddingframework.drones.log`. The reads and `SetActive` keep working; only `Changed` stops.

## Threading and safety

- Main thread only.
- `Drones.IsAvailable` is `false` when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. With no drone loaded, or on the main menu, reads return an empty list, `null` or `false`.
- Writes return `bool`. Nothing throws, so `SetActive` returns `false` for an unknown or empty id.
- `Changed` is raised synchronously, inside the game's own `TurnOn` or `TurnOff` call, after the game has changed `active` and fired its outputs. A handler that throws is logged and the other handlers still run (`Events.Raise`). If a `TurnOn` output turns the same drone straight back off, only the final `Changed(false)` fires.

## Drones

```csharp
public static class Drones
{
    public static List<DroneInfo> All();
    public static DroneInfo Get(string id);
    public static bool SetActive(string id, bool active);

    public static event Action<DroneChangedArgs> Changed;

    public static bool IsAvailable { get; }
}

public sealed class DroneInfo
{
    public string Id { get; }              // SceneIds.Of(drone): the save GUID, or the scene path
    public bool Active { get; }            // Drone.active
    public Vector3 Position { get; }       // transform.position
    public float MaxSpeed { get; }         // Drone.maxSpeed, the configured cap, not the live speed
    public string TargetNodeId { get; }    // Drone.targetNode, null when the drone has no target
    public string NextNodeId { get; }      // targetNode.NextNode, null when there is none
}

public sealed class DroneChangedArgs
{
    public string Id { get; }              // SceneIds.Of(drone)
    public bool Active { get; }            // the state after the change
}
```

- `id` is `SceneIds.Of(drone)`: the save GUID when the drone has one, otherwise the scene path. The public `Drone.GUID` field (`Items/Drone.cs:10`) is not assigned in any decompiled file that mentions `Drone`, so the facade does not use it.
- `All` returns every loaded drone, in the order `FindObjectsByType` returns them. Unity's find calls are expected to skip inactive GameObjects; not verified in game.
- `Get` returns `null` for an unknown or empty id.
- `SetActive(id, true)` calls the game's `Drone.TurnOn()` and `SetActive(id, false)` calls `Drone.TurnOff()`. Both are the game's own methods (`Items/Drone.cs:255`, `:274`), so the outputs, sounds and `Changed` run as they do in game. The return value is `true` when `active` now equals the requested value. Calling it with the state the drone already has returns `true` and does nothing.
- `Changed` fires when `active` really changes (`Drone.TurnOn` or `TurnOff` changes state). A drone that the game turns off at the end of its route also fires `Changed` (`Items/Drone.cs:153`, `:247`). A call that changes nothing does not fire.

## Not exposed

- The live speed and the route position while flying. The game keeps them in protected fields (`currentSpeed`, `currentPosition`, `Items/Drone.cs:34`, `:51`). `MaxSpeed` is the configured value.
- Arrival at a node. `Air_Node.Pass` (`AI/Air_Node.cs:151`) does not receive the drone, so it cannot say which drone passed. Only the route's end is reported, through `Changed`.
- Route editing. `SetTarget` and `SetMaxSpeed` exist on the game's `Drone` (`Items/Drone.cs:293`, `:309`) but are not wrapped: they change the route and speed without the game's start and stop side effects. Not exposed until a modder needs them.

## Ceilings and risks

- `Changed` depends on the two patches. If the game renames or inlines `TurnOn` or `TurnOff`, the owner is disabled at `Init`. Not verified in game.
- `Changed` fires from inside the game's `Update` (path end). Handlers run on the main thread in that frame.
- `Id` for a drone without a save GUID is a scene path. Renaming or moving the GameObject changes the id.
- `NextNodeId` reads `Air_Node.NextNode`, which depends on the node's `activeNodeIndex` (`AI/Air_Node.cs:84`). It is the node the game would pick next, not a promise.
- Nothing is verified in game yet.

## Smoke test

1. Load a save with a drone in the scene. In the BepInEx console or a small test plugin, call `Drones.All()` and check the count and `Active` values.
2. Call `Drones.SetActive(id, true)` on an inactive drone. It should return `true`, the propellers should start, and `Drones.Get(id).Active` should be `true`.
3. Subscribe with `Drones.Changed += a => Log(a.Id + " " + a.Active);`. Call `SetActive(id, false)`. It should log once with `Active` false.
4. Call `SetActive(id, false)` again. It should return `true` and log nothing.
5. Let a drone with a route reach the end. It should turn off and `Changed` should fire once.
