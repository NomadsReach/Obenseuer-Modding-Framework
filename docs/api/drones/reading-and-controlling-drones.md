---
title: Reading and controlling drones
parent: Drones
grand_parent: Objects and devices
nav_order: 2
---

# Reading and controlling drones

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`Drones` reads and controls the drones in the scene you are in. Each drone is a `DroneInfo`, a snapshot of its state at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public static class Drones
{
    public static List<DroneInfo> All();                         // every drone in the scene you are in
    public static DroneInfo Get(string id);                      // the drone with this id; null when there is none
    public static bool SetActive(string id, bool active);        // switches a drone on or off; true when it ends up in that state
    public static bool SetTarget(string id, string nodeId);      // points a drone at an air node; true when that node is now its target
    public static bool SetMaxSpeed(string id, float maxSpeed);   // sets the top speed; true when the drone now has that top speed

    public static event Action<DroneChangedArgs> Changed;        // a drone was switched on or off

    public static bool IsAvailable { get; }                      // false when the Changed or NodePassed events could not be set up
}

public sealed class DroneInfo    // a snapshot of one drone
{
    public string Id { get; }              // names this drone; see the first bullet below
    public bool Active { get; }            // true when the drone is switched on
    public Vector3 Position { get; }       // where the drone is, in world coordinates
    public float MaxSpeed { get; }         // the top speed it is set to, not the speed it is flying at right now
    public string TargetNodeId { get; }    // the id of the air node it is heading for; null when it has no target
    public string NextNodeId { get; }      // the id of the node after that one; null when there is none
}

public sealed class DroneChangedArgs    // what you get when a drone is switched on or off
{
    public string Id { get; }              // the id of the drone, as DroneInfo.Id
    public bool Active { get; }            // true when the drone is now on, false when it is now off
}
```

- **A drone's `Id` names one drone in the scene you are in.** If the game gave the drone a save id, that is the id. If not, the id is the drone's place in the scene, for example `Scene:Root[0]/Child[2]`, and it changes when the object is renamed or moved.
  - Technical: the framework uses a non-empty public `Drone.GUID` field first, then the id the game's save system gives the object (`SaveController.GetObjectGUID`), then the scene path. No game code that mentions `Drone` ever sets `Drone.GUID`, so in practice you get the save id or the path.
- **`All()` lists the drones in the scene you are in.** A drone that is switched off at the object level (the whole object is hidden) is not listed. `Get` returns null when you pass an id that nothing matches, or an empty id.
  - Technical: `All` and `Get` use `FindObjectsByType` with inactive objects left out, and the order is whatever Unity returns.
- **`SetActive` runs the game's own switch on or off, so you get the same sounds and effects as in the game.** It returns `true` when the drone is in the state you asked for, including when it was already in that state, in which case nothing happens. It returns `false` for an unknown id, an empty id, or while a save is loading or being written.
  - Technical: it calls `Drone.TurnOn()` or `Drone.TurnOff()`, so the drone's outputs and sounds run as normal, and `Changed` fires. The save check is `SaveData.Busy`.
- **`Changed` fires when a drone really changes state.** It also fires when the game switches a drone off at the end of its route. It does not fire when a call changes nothing.
  - Technical: it is raised from a Harmony postfix (a change to the game method that runs after it) on `Drone.TurnOn` and `Drone.TurnOff`, only when `active` differs from its value before the call. If a `TurnOn` output switches the same drone straight back off, only the last `Changed(false)` fires.
- **`MaxSpeed` is the speed the drone is set to, not how fast it is flying right now.** You can't read the live speed (see [What you can't do (yet)](details.md#what-you-cant-do-yet)).
