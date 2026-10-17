---
title: Reading and controlling drones
parent: Drones
grand_parent: Objects and devices
nav_order: 2
---

# Reading and controlling drones

This is the full list of what the API gives you. `Drones` reads and controls the drones in the scene you are in. Each drone is a `DroneInfo`, a snapshot of its state at the moment you asked, and the snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Drones</code> <span>read and control drones</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Drones
{
    public static List<DroneInfo> All();
    public static DroneInfo Get(string id);
    public static bool SetActive(string id, bool active);
    public static bool SetTarget(string id, string nodeId);
    public static bool SetMaxSpeed(string id, float maxSpeed);
    public static event Action<DroneChangedArgs> Changed;
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<DroneInfo>` | Every drone in the scene you are in. |
| `Get(string id)` | `DroneInfo` | The drone with this id. `null` when there is none. |
| `SetActive(string id, bool active)` | `bool` | Switches a drone on or off. `true` when it ends up in that state. |
| `SetTarget(string id, string nodeId)` | `bool` | Points a drone at an air node. `true` when that node is now its target. |
| `SetMaxSpeed(string id, float maxSpeed)` | `bool` | Sets the top speed. `true` when the drone now has that top speed. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the `Changed` or `NodePassed` events could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Changed` | `DroneChangedArgs` | A drone was switched on or off. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DroneInfo</code> <span>a snapshot of one drone</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class DroneInfo
{
    public string Id { get; }
    public bool Active { get; }
    public Vector3 Position { get; }
    public float MaxSpeed { get; }
    public string TargetNodeId { get; }
    public string NextNodeId { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | Names this drone. See the first bullet below. |
| `Active` | `bool` | `true` when the drone is switched on. |
| `Position` | `Vector3` | Where the drone is, in world coordinates. |
| `MaxSpeed` | `float` | The top speed it is set to, not the speed it is flying at right now. |
| `TargetNodeId` | `string` | The id of the air node it is heading for. `null` when it has no target. |
| `NextNodeId` | `string` | The id of the node after that one. `null` when there is none. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DroneChangedArgs</code> <span>what you get when a drone is switched on or off</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class DroneChangedArgs
{
    public string Id { get; }
    public bool Active { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The id of the drone, as `DroneInfo.Id`. |
| `Active` | `bool` | `true` when the drone is now on, `false` when it is now off. |

</div>
</div>

## Things to know

- A drone's `Id` names one drone in the scene you are in. If the game gave the drone a save id, that is the id. If not, the id is the drone's place in the scene, for example `Scene:Root[0]/Child[2]`, and it changes when the object is renamed or moved. <span class="tech">(The framework uses a non-empty public `Drone.GUID` field first, then the id the game's save system gives the object (`SaveController.GetObjectGUID`), then the scene path. No game code that mentions `Drone` ever sets `Drone.GUID`, so in practice you get the save id or the path.)</span>
- `All()` lists the drones in the scene you are in. A drone that is switched off at the object level (the whole object is hidden) is not listed. `Get` returns null when you pass an id that nothing matches, or an empty id. <span class="tech">(`All` and `Get` use `FindObjectsByType` with inactive objects left out, and the order is whatever Unity returns.)</span>
- `SetActive` runs the game's own switch on or off, so you get the same sounds and effects as in the game. It returns `true` when the drone is in the state you asked for, including when it was already in that state, in which case nothing happens. It returns `false` for an unknown id, an empty id, or while a save is loading or being written. <span class="tech">(It calls `Drone.TurnOn()` or `Drone.TurnOff()`, so the drone's outputs and sounds run as normal, and `Changed` fires. The save check is `SaveData.Busy`.)</span>
- `Changed` fires when a drone really changes state. It also fires when the game switches a drone off at the end of its route. It does not fire when a call changes nothing. <span class="tech">(It is raised from a Harmony postfix (a change to the game method that runs after it) on `Drone.TurnOn` and `Drone.TurnOff`, only when `active` differs from its value before the call. If a `TurnOn` output switches the same drone straight back off, only the last `Changed(false)` fires.)</span>
- `MaxSpeed` is the speed the drone is set to, not how fast it is flying right now. You can't read the live speed (see [What you can't do (yet)](details.md#what-you-cant-do-yet)).
