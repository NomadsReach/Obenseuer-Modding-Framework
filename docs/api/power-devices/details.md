---
title: Details
parent: Power devices
grand_parent: Objects and devices
nav_order: 5
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.powerdevices.log`. Source files: `src/Api/PowerDevices.cs` and `src/Api/PowerDevicesPatches.cs`.

Everything not listed in this reference is internal and may change without notice.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game only works safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)). This goes for the code that handles the events too.
- It is safe to call any of it at any time, even on the main menu or with no save loaded. `All()` gives an empty list, `Get` gives `null`, and `SetOn` gives `false`. Nothing throws. A `null` or empty id simply finds nothing.
- Your event handlers run inside the game's own call, after the state has changed. If one throws an error, the framework logs it and still runs the other handlers.

## If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod about power changes. If a game update breaks one of them, only that feature turns off, and the framework writes error lines and a warning to the log. The rest of the framework keeps working. This page has three feature groups.

- Furniture power changes use the feature group `obenseuer.moddingframework.powerdevices`. If any of its changes is missing, the group is turned off, and `PoweredChanged` stops for furniture. `IsAvailable` is `false`. The reads and `SetOn` keep working. <span class="tech">(The changed methods are `FurnitureStats.Awake`, `OutputON` and `OutputOFF`.)</span>
- The fuel run-out event uses the feature group `obenseuer.moddingframework.powerdevices.fuel`, and the battery part of `PoweredChanged` uses `obenseuer.moddingframework.powerdevices.battery`. The framework only sets these two up after the furniture group worked. If one is missing, only its own event stops, and `IsAvailable` does not change. If the furniture group fails, neither is set up. <span class="tech">(The changed methods are `Fuel.OnFuelConsumeStopped` (fuel) and `BatteryPowerSource.PowerON` and `PowerOFF` (battery).)</span>
