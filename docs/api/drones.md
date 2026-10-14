---
title: Drones API
parent: API reference
nav_order: 18
---

# Drones API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.drones`.
Files: `src/Api/Drones.cs`, `src/Api/DronesPatches.cs`.

Everything not listed here is internal.

## If a hook cannot be installed

The framework installs the hooks under one owner: `TurnOn` and `TurnOff` on `Drone` (for `Changed`), `Update` on `Drone` and `Pass` on `Air_Node` (for `NodePassed`). All are needed. If any is missing, the owner is removed, `Drones.IsAvailable` becomes `false`, and the error is logged to `BepInEx\logs\obenseuer.moddingframework.drones.log`. The reads and `SetActive` keep working; only `Changed` and `NodePassed` stop.

## Threading and safety

- Main thread only.
- `Drones.IsAvailable` is `false` when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. With no drone loaded, or on the main menu, reads return an empty list, `null` or `false`.
- Writes return `bool`. Nothing throws, so `SetActive` returns `false` for an unknown or empty id, and while a save loads or saves (`SaveData.Busy`).
- `Changed` is raised synchronously, inside the game's own `TurnOn` or `TurnOff` call, after the game has changed `active` and fired its outputs. A handler that throws is logged and the other handlers still run. If a `TurnOn` output turns the same drone straight back off, only the final `Changed(false)` fires.
- `NodePassed` is raised inside the game's `Drone.Update`, at the start of `Air_Node.Pass`, before the node's `OnPass` outputs run. At that point the drone's `targetNode` is still the node. The game then moves `targetNode` to the next node, or turns the drone off at the route end, so a `SetTarget` made in the handler is overwritten in the same `Update`. A `SetActive(id, false)` in the handler does take effect: the drone stops in the next `Update`. A handler that throws is logged and the other handlers still run.

## Drones

```csharp
public static class Drones
{
    public static List<DroneInfo> All();
    public static DroneInfo Get(string id);
    public static bool SetActive(string id, bool active);
    public static bool SetTarget(string id, string nodeId);
    public static bool SetMaxSpeed(string id, float maxSpeed);

    public static event Action<DroneChangedArgs> Changed;
    public static event Action<DroneNodePassedArgs> NodePassed;

    public static bool IsAvailable { get; }
}

public sealed class DroneInfo
{
    public string Id { get; }              // the save GUID, or the scene path
    public bool Active { get; }            // Drone.active
    public Vector3 Position { get; }       // transform.position
    public float MaxSpeed { get; }         // Drone.maxSpeed, the configured cap, not the live speed
    public string TargetNodeId { get; }    // Drone.targetNode, null when the drone has no target
    public string NextNodeId { get; }      // targetNode.NextNode, null when there is none
}

public sealed class DroneChangedArgs
{
    public string Id { get; }              // save GUID, or the scene path
    public bool Active { get; }            // the state after the change
}
```

- `id` is the save GUID when the drone has one, otherwise the scene path. The public `Drone.GUID` field is not assigned by any game code that mentions `Drone`, so the facade does not use it.
- `All` returns every drone in the active scene, in the order `FindObjectsByType` returns them. Unity's find calls are expected to skip inactive GameObjects.
- `Get` returns `null` for an unknown or empty id.
- `SetActive(id, true)` calls the game's `Drone.TurnOn()` and `SetActive(id, false)` calls `Drone.TurnOff()`. Both are the game's own methods, so the outputs, sounds and `Changed` run as they do in game. The return value is `true` when `active` now equals the requested value. Calling it with the state the drone already has returns `true` and does nothing.
- `Changed` fires when `active` really changes (`Drone.TurnOn` or `TurnOff` changes state). A drone that the game turns off at the end of its route also fires `Changed`. A call that changes nothing does not fire.

## Air nodes

```csharp
public static class Drones
{
    public static List<AirNodeInfo> Nodes();
}

public sealed class AirNodeInfo
{
    public string Id { get; }                  // the save GUID (Air_Node.GUID), or the scene path
    public Vector3 Position { get; }           // transform.position
    public List<string> LinkedNodeIds { get; } // the ids of Air_Node.nextNodes, in route order, nulls skipped
}
```

- `Nodes` is read only. It lists every `Air_Node` in the active scene with `FindObjectsByType`, so nodes that are not in the scene are not listed.
- `LinkedNodeIds` is the full `nextNodes` list, not only the active index (`Air_Node.activeNodeIndex`). A node with no links returns an empty list.
- The ids match `DroneInfo.TargetNodeId` and `SetTarget`, because both use the same id rule.
- `nextNodes` is set in the editor. Linked ids cannot be written: the facade has no node-link writer, and the drone route is still set through `SetTarget`.

## Node passes

```csharp
public static event Action<DroneNodePassedArgs> NodePassed;

public sealed class DroneNodePassedArgs
{
    public string NodeId { get; }          // the save GUID, or the scene path
    public string DroneId { get; }         // id of the drone that passed the node, null when none matched
    public bool IsRouteEnd { get; }        // the node's NextNode is null when the pass runs
}
```

- `Air_Node.Pass` does not take the drone. The facade hooks `Drone.Update` to record which drone is running, then reads that record in a prefix on `Pass`. `Drone.Update` is the only caller of `Pass` in the game code, so `DroneId` is the drone that passed in the game's own flow.
- `DroneId` is `null` when `Pass` runs outside a drone's `Update`, or when the recorded drone's `targetNode` is not this node. In the game code that does not happen, but a scene output or `SendMessage` can call `Pass` by name.
- `IsRouteEnd` is `true` when `Air_Node.NextNode` is `null`. With `deActivateOnPathEnd` set, the game then turns the drone off, so `Changed` fires too.
- One event per `Pass` call. Each handler gets the same args object.
- The drone's `targetNode` changes after the handlers run, so `TargetNodeId` in `Get` read inside the handler still shows the passed node.

## Not exposed

- The live speed and the route position while flying. The game keeps them in protected fields (`currentSpeed`, `currentPosition`). `MaxSpeed` is the configured value.
- The route as a list. Only the current target and the next node are reported. The facade does not walk the route.

## Route

- `SetTarget(id, nodeId)` finds the drone with `Find` and the node by its id, then calls the game's `Drone.SetTarget`. That method only assigns `targetNode`. The drone does not start, so a drone that is off keeps the new target until `SetActive(id, true)`. It returns `true` when the drone's target is now that node. It returns `false` for an unknown drone, an unknown node id, an empty id, or while `SaveData.Busy` is set.
- `SetTarget` refuses a null node. With `deActivateOnPathEnd` set (the default), a null target turns the drone off on its next `Update`, and the facade does not start that side effect.
- `SetMaxSpeed(id, maxSpeed)` calls the game's `Drone.SetMaxSpeed`, which assigns the public `maxSpeed`. It returns `false` for zero, a negative value, NaN, infinity, an unknown drone, or while `SaveData.Busy` is set. A zero `maxSpeed` would divide by zero in `Update`, so the facade refuses it. A target node with its own `maxSpeed` above 0 overrides this value.
- Saved state, from the field attributes: the target is saved through `targetNodeRef`, which stores the node's `Air_Node.GUID` and resolves it on load through `Air_Node.GetNode`. A node with an empty GUID saves an empty reference and loads with no target. `maxSpeed` is a public field without `[fsIgnore]`, so the save code is expected to keep it.

## Ceilings and risks

- `Changed` depends on the two patches. If the game renames or inlines `TurnOn` or `TurnOff`, the owner is disabled at startup.
- `Changed` fires from inside the game's `Update` (path end). Handlers run on the main thread in that frame.
- `NodePassed` fires from inside the game's `Update` too, for every node a drone reaches. `DroneId` is the drone whose `Update` is running, so it is exact per pass.
- The `Update` hook runs for every drone every frame whether or not `NodePassed` has a subscriber. It writes one static field before and clears it after. Not measured in game.
- `Id` for a drone without a save GUID is a scene path. Renaming or moving the GameObject changes the id.
- `NextNodeId` reads `Air_Node.NextNode`, which depends on the node's `activeNodeIndex`. It is the node the game would pick next, not a promise.

## Save loads

The framework does not gate `Changed` or `NodePassed` on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written. A drone restored in range of its target node can pass it during the load, and `NodePassed` reports that pass.
