---
title: Details
parent: Drones
grand_parent: Objects and devices
nav_order: 6
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.drones.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts, so it can tell your mod when a drone changes state or passes a node. These changes are grouped as a feature group. This page's feature group is `obenseuer.moddingframework.drones`, the name you will see in the log.

## When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)). This goes for every member on this page and for the code that handles each event.
- **It is safe to call any of it at any time, even on the main menu or while a save is loading.** If there is no drone to read, you get an empty list, `null` or `false`. Nothing throws: the writing calls return `false` instead.
- **Your event handlers run inside the game's own call, one at a time.** If one of your handlers throws an error, the framework logs it and still runs the other handlers.
  - Technical: `Changed` is raised synchronously inside the game's `TurnOn` or `TurnOff`, after the game has changed `active` and fired its outputs. `NodePassed` is raised inside `Drone.Update`.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **All the changes behind `Changed` and `NodePassed` are needed.** If any one of them cannot be made, the whole feature group turns off, `Drones.IsAvailable` becomes `false`, and the error goes to `BepInEx\logs\obenseuer.moddingframework.drones.log`. The reads and `SetActive` keep working. Only `Changed` and `NodePassed` stop.
  - Technical: the changes are on `Drone.TurnOn` and `Drone.TurnOff` (for `Changed`), `Drone.Update` and `Air_Node.Pass` (for `NodePassed`).

## Limits

- **The writing calls are refused while a save is loading or being written.** They return `false`. The events are not held back during a load: see [While a save is loading](#while-a-save-is-loading).
- **The framework watches every drone on every frame, even when nobody listens to `NodePassed`.** The cost is tiny: it stores one value before the drone's update and clears it after.
- **A drone without a save id gets a new id if you rename or move its object.** Do not store those ids between sessions.
- **`NextNodeId` is the node the game would pick next, not a promise.** It depends on the node's current choice of next node, which can change.
  - Technical: it reads `Air_Node.NextNode`, which depends on `activeNodeIndex`.

## What you can't do (yet)

- **You can't read how fast a drone is flying right now, or where it is along its route.** The game keeps those in private fields. `MaxSpeed` is only the setting.
  - Technical: the fields are the protected `currentSpeed` and `currentPosition`.
- **You can't read a drone's whole route as a list.** The API gives the current target and the node after it. It does not walk the route for you, but you can follow `LinkedNodeIds` yourself.

## While a save is loading

The framework does not hold back `Changed` or `NodePassed` while a save is loading. A drone that is put back near its target node can pass it during the load, and `NodePassed` reports that. If your handler should ignore events raised during a load, check `SaveData.Busy` first. It is `true` while a save is loading or being written.
