---
title: Flood Gates API
parent: API reference
nav_order: 22
---

# Flood Gates API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.floodgates`.
Files: `src/Api/FloodGates.cs`, `src/Api/FloodGatesPatches.cs`.

Everything not listed here is internal.

## If a hook cannot be installed

The framework installs the `OpenFloodGate` and `CloseFloodGate` hooks under one owner. If either is missing, both are removed, `FloodGates.IsAvailable` becomes false, and one warning is logged. The reads, `Open` and `Close` keep working; only the event stops.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no scene loaded, or on the main menu, reads return an empty list, `null` or `false`.
- Writes return `bool`. Nothing throws to the caller. An unknown or empty id returns `false`.
- `Open` and `Close` go through the game's `OpenFloodGate` and `CloseFloodGate`. They return `true` only when the gate's state actually changed. The game refuses while the gate is in an alarm, while it is powered off, or while it is moving with the press-on-blinking setting off, and then `Open` and `Close` return `false`.
- `Changed` is raised synchronously. It is not raised while a save loads.
- `Open` returns `false` while a save is loading or being written.
- `Close` returns `false` while a save is loading or being written.

## Flood gates

```csharp
public static class FloodGates
{
    public static List<FloodGateInfo> All();
    public static FloodGateInfo Get(string id);
    public static bool IsOpen(string id);
    public static bool IsPowered(string id);
    public static bool Open(string id);
    public static bool Close(string id);
    public static bool SetPowered(string id, bool on);

    public static event Action<FloodGateChangedArgs> Changed;

    public static bool IsAvailable { get; }
}

public sealed class FloodGateInfo
{
    public string Id { get; }              // the save GUID, or a scene path when it has none
    public string Name { get; }            // the GameObject name
    public bool IsOpen { get; }            // the gate is commanded open, including while it is still moving
    public bool IsPowered { get; }         // false after the gate was turned off
}

public sealed class FloodGateChangedArgs
{
    public string Id { get; }              // the same id as FloodGateInfo.Id
    public bool IsOpen { get; }            // the new open state
}
```

## Notes

- `IsOpen` reads the game's `open` flag. The flag changes when the gate is commanded, not when it reaches the end of its travel. A gate that is still closing reads as closed.
- `IsPowered` reads the game's `powerOff` flag, inverted.
- `Changed` fires only when `IsOpen` changes. A power change through `SetPowered` does not raise `Changed`, because `Changed` tracks `IsOpen` only. `SetPowered(id, true)` on a gate that is moving can still reverse it through the game's `OpenFloodGate` or `CloseFloodGate`, and that raises `Changed`.

## Power

- `SetPowered(id, on)` calls the game's `TurnOn` or `TurnOff`. It returns `true` when `IsPowered` now equals `on`. It returns `false` for an unknown or empty id, when the gate is already in that power state, and while `SaveData.Busy` is set. It refuses, like `Open` and `Close`, instead of repeating the state.
- Power on after a power-off during travel has two results, by the game's `allowPressOnBlinking` (default true, `FloodGateControl`). Power off stops the travel and timer coroutines (`TurnOff` calls `StopCoroutines`), and the blinking flag stays set, so the next power-on takes the move's branch in `TurnOn`. With `allowPressOnBlinking` true the move reverses (`OpenFloodGate` or `CloseFloodGate`, which raise `Changed`). With it false, the gate snaps: `TurnOn` calls `FullyClosed` or `FullyOpen` (`FloodGateControl`, `FullyClosed`), which sets the rest light, fires the closed or open relay and clears the blinking flag, so the gate reads as at the end of the move in progress at once. No travel coroutine runs on that path. The physical position of the gate after the snap is not known.
- Saved: the power flag is `powerOff`, a `[fsProperty]` field, written by `OnSavingGame`. A power state set through the API survives a save and load.
- `TurnOn` fires the game's relay for the state the gate is in: `onClosed` for a closed gate, `onOpen` for an open one, and `onAlert` for an alarmed one, which also restarts the alarm.
- `TurnOff` fires no relay. It stops the travel coroutines and turns the lights off.
- Risk: `TurnOff` does not clear `isBlinking`. A gate turned off mid-travel stays marked as moving. A save made then restores the travel on load, and `OnLoadingGame` starts `Open()` or `Close()` without checking `powerOff`. So the gate can move while unpowered after a load. No game code calls `TurnOn` or `TurnOff`, so the game reaches them through scene wiring that is not in the code.
