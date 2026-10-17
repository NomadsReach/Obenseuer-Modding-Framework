---
title: Details
parent: Flood gates
grand_parent: Objects and devices
nav_order: 4
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.floodgates.log`. Source files: `src/Api/FloodGates.cs` and `src/Api/FloodGatesPatches.cs`.

Everything not listed in this reference is internal and may change without notice.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game only works safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)). This goes for the code that handles `Changed` too.
- It is safe to call any of it at any time, even on the main menu or with no save loaded. Reads give an empty list, `null` or `false`. The calls that change a gate give `false` and nothing throws to you. An unknown or empty id gives `false`.
- Your `Changed` handler runs inside the game's own call, right after the command. If your handler throws an error, the framework logs it and still runs the other handlers.

## If something fails to set up

The framework changes two of the game's methods when it starts, so it can tell your mod when a gate is told to open or close. If a game update breaks one of them, only this feature turns off, and the framework writes error lines and a warning to the log. The rest of the framework keeps working. The name you will see in the log is the feature group `obenseuer.moddingframework.floodgates`.

- If either method cannot be changed, both are put back and `Changed` stops. `IsAvailable` is `false`. The reads, `Open`, `Close` and `SetPowered` keep working. <span class="tech">(The methods are `FloodGateControl.OpenFloodGate` and `CloseFloodGate`. The framework also needs the game's private `open` and `powerOff` fields. If one is missing, the feature group is turned off.)</span>
