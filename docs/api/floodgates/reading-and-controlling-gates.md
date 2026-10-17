---
title: Reading and controlling gates
parent: Flood gates
grand_parent: Objects and devices
nav_order: 2
---

# Reading and controlling gates

This is the full list of what the API gives you. In the code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`FloodGates` reads and controls the flood gates in the scene you are in. Each gate is a `FloodGateInfo`, a snapshot of its state when you asked.

```csharp
public static class FloodGates
{
    public static List<FloodGateInfo> All();                // every flood gate in the scene you are in
    public static FloodGateInfo Get(string id);             // the gate with this id; null when none has it
    public static bool IsOpen(string id);                   // true when the gate is open or opening; false when closed or the id is unknown
    public static bool IsPowered(string id);                // true when the gate has power; false when it has none or the id is unknown
    public static bool Open(string id);                     // tells the gate to open; true when it started to open
    public static bool Close(string id);                    // tells the gate to close; true when it started to close
    public static bool SetPowered(string id, bool on);      // turns the gate's power on or off; true when the power changed

    public static event Action<FloodGateChangedArgs> Changed;   // a gate has just been told to open or to close

    public static bool IsAvailable { get; }                 // false when the change event could not be set up
}

public sealed class FloodGateInfo    // a snapshot of one flood gate
{
    public string Id { get; }              // the gate's id: its save id, or its place in the scene if it has none
    public string Name { get; }            // the name of the gate's object in the scene
    public bool IsOpen { get; }            // true when the gate is open or is opening
    public bool IsPowered { get; }         // false when the gate's power has been turned off
}

public sealed class FloodGateChangedArgs    // what the Changed event tells you
{
    public string Id { get; }              // the same id as FloodGateInfo.Id
    public bool IsOpen { get; }            // true when the gate was told to open, false when it was told to close
}
```

- **`All()` lists the gates that are switched on in the scene you are in.** A gate that is switched off in the scene does not appear. The id is how you point at one gate in the other calls.
  - Technical: the id is the object's save GUID (see the [glossary](../../glossary.md#guid)), or the scene path when it has none. `All` looks at active `FloodGateControl` objects only.
- **`IsOpen` follows the command, not the gate's position.** It becomes `true` the moment the gate is told to open, and it becomes `false` the moment it is told to close. A gate that is still closing already reads as closed.
  - Technical: it reads the game's `open` flag.
- **`IsPowered` is `false` once the gate's power has been turned off.**
  - Technical: it is the game's `powerOff` flag, turned around.
- **`Open(id)` and `Close(id)` ask the gate to move, and they return `true` only when the state really changed.** They return `false` for an unknown or empty id, for a gate that is already in that state, and while a save is loading or saving. The game also refuses in these cases, and then you get `false` too: the gate is in an alarm, it has no power, or it is moving and its "press while blinking" setting is off.
  - Technical: they call the game's `OpenFloodGate` and `CloseFloodGate`. The game refuses when `alert` or `powerOff` is set, or when `isBlinking` is set and `allowPressOnBlinking` is false.
- **`SetPowered(id, on)` turns the gate's power on or off.** It returns `true` when the gate's power is now what you asked for. It returns `false` for an unknown or empty id, when the gate already has that power state, and while a save is loading or saving. It does not pretend to succeed when nothing changed.
  - Technical: it calls the game's `TurnOn` or `TurnOff`.
- **The power is saved with the game.** A power state you set survives a save and a load.
  - Technical: the `powerOff` field is a `[fsProperty]` that `OnSavingGame` writes.
- **`Changed` fires when a gate is told to open or to close, no matter who caused it.** It fires only when `IsOpen` really changes. It does not fire while a save is loading.
- **Changing the power does not fire `Changed`.** Only a change of open or closed does. But switching the power back on while a gate is half way can turn it around, and that does fire `Changed`.
  - Technical: see "Turning the power back on" below.
- **`IsAvailable` is `false` when the change event could not be set up.** Then `Changed` never fires. The reads, `Open`, `Close` and `SetPowered` keep working.
