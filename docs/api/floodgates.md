# Flood Gates API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.floodgates`.
Files: `src/Api/FloodGates.cs`, `src/Api/FloodGatesPatches.cs`.

Everything not listed here is internal.

## Wiring

```csharp
internal static void FloodGates.Init();
```

Core calls this from `Core.Awake`.

`Init` installs the `OpenFloodGate` and `CloseFloodGate` hooks under one owner. If either is missing, both are removed, `FloodGates.IsAvailable` becomes false, and one warning is logged. The reads, `Open` and `Close` keep working; only the event stops.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no scene loaded, or on the main menu, reads return an empty list, `null` or `false`.
- Writes return `bool`. Nothing throws to the caller. An unknown or empty id returns `false`.
- `Open` and `Close` go through the game's `OpenFloodGate` and `CloseFloodGate`. They return `true` only when the gate's state actually changed. The game refuses while the gate is in an alarm, while it is powered off, or while it is moving with the press-on-blinking setting off, and then `Open` and `Close` return `false`.
- `Changed` is raised synchronously through `Events.Raise`. It is not raised while a save loads.
- `Open` returns `false` while a save loads (`SaveController.Loading`).
- `Close` returns `false` while a save loads (`SaveController.Loading`).

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

    public static event Action<FloodGateChangedArgs> Changed;

    public static bool IsAvailable { get; }
}

public sealed class FloodGateInfo
{
    public string Id { get; }              // SceneIds.Of: the save GUID, or a scene path when it has none
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
- `Changed` fires only when `IsOpen` changes. `TurnOn` and `TurnOff` are not wrapped, so a power change does not raise `Changed`.
