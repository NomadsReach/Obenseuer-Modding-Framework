---
title: Reading and controlling gates
parent: Flood gates
grand_parent: Objects and devices
nav_order: 2
---

# Reading and controlling gates

`FloodGates` reads and controls the flood gates in the scene you are in. Each gate is a `FloodGateInfo`, a snapshot of its state when you asked.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FloodGates</code> <span>read and control the flood gates</span></p>

This is the class you start from. `All()` and `Get(id)` find gates, `Open` and `Close` move them, and `Changed` tells you when a gate has been told to open or to close.

<div class="api-pair" markdown="1">

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
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<FloodGateInfo>` | Every flood gate in the scene you are in. |
| `Get(id)` | `FloodGateInfo` | The gate with this id. `null` when none has it. |
| `IsOpen(id)` | `bool` | `true` when the gate is open or opening. `false` when it is closed or the id is unknown. |
| `IsPowered(id)` | `bool` | `true` when the gate has power. `false` when it has none or the id is unknown. |
| `Open(id)` | `bool` | Tells the gate to open. `true` when it started to open. |
| `Close(id)` | `bool` | Tells the gate to close. `true` when it started to close. |
| `SetPowered(id, on)` | `bool` | Turns the gate's power on or off. `true` when the power changed. |

| Event | Gives you | When it fires |
|---|---|---|
| `Changed` | `FloodGateChangedArgs` | A gate has just been told to open or to close. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the change event could not be set up. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FloodGateInfo</code> <span>a snapshot of one flood gate</span></p>

`All()` and `Get(id)` give you this. It is a copy of the gate's state when you asked, so ask again for a current `IsOpen`. <span class="tech">(It is filled from the game's `FloodGateControl` on the gate's object.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class FloodGateInfo
{
    public string Id { get; }
    public string Name { get; }
    public bool IsOpen { get; }
    public bool IsPowered { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The gate's id: its save id, or its place in the scene if it has none. |
| `Name` | `string` | The name of the gate's object in the scene. |
| `IsOpen` | `bool` | `true` when the gate is open or is opening. |
| `IsPowered` | `bool` | `false` when the gate's power has been turned off. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FloodGateChangedArgs</code> <span>what the Changed event tells you</span></p>

`Changed` gives you this. The framework builds it from a hook on the game's gate commands, and only when the open state really changes, so it is the framework's own type.

<div class="api-pair" markdown="1">

```csharp
public sealed class FloodGateChangedArgs
{
    public string Id { get; }
    public bool IsOpen { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The same id as `FloodGateInfo.Id`. |
| `IsOpen` | `bool` | `true` when the gate was told to open, `false` when it was told to close. |

</div>
</div>

## Things to know

- `All()` lists the gates that are switched on in the scene you are in. A gate that is switched off in the scene does not appear. The id is how you point at one gate in the other calls. <span class="tech">(The id is the object's save GUID (see the [glossary](../../glossary.md#guid)), or the scene path when it has none. `All` looks at active `FloodGateControl` objects only.)</span>
- `IsOpen` follows the command, not the gate's position. It becomes `true` the moment the gate is told to open, and it becomes `false` the moment it is told to close. A gate that is still closing already reads as closed. <span class="tech">(It reads the game's `open` flag.)</span>
- `IsPowered` is `false` once the gate's power has been turned off. <span class="tech">(It is the game's `powerOff` flag, turned around.)</span>
- `Open` and `Close` ask the gate to move, and they return `true` only when the state really changed.
- `SetPowered` turns the gate's power on or off. It returns `true` when the gate's power is now what you asked for. It does not pretend to succeed when nothing changed. <span class="tech">(It calls the game's `TurnOn` or `TurnOff`.)</span>
- The power is saved with the game. A power state you set survives a save and a load. <span class="tech">(The `powerOff` field is a `[fsProperty]` that `OnSavingGame` writes.)</span>
- `Changed` fires when a gate is told to open or to close, no matter who caused it. It fires only when `IsOpen` really changes.
- Changing the power does not fire `Changed`. Only a change of open or closed does. But switching the power back on while a gate is half way can turn it around, and that does fire `Changed`. <span class="tech">(See [Turning the power back on](turning-the-power-back-on.md).)</span>

## Limits

- `Open` and `Close` return `false` for an unknown or empty id, for a gate that is already in that state, and while a save is loading or saving. The game also refuses in these cases, and then you get `false` too: the gate is in an alarm, it has no power, or it is moving and its "press while blinking" setting is off. <span class="tech">(They call the game's `OpenFloodGate` and `CloseFloodGate`. The game refuses when `alert` or `powerOff` is set, or when `isBlinking` is set and `allowPressOnBlinking` is false.)</span>
- `SetPowered` returns `false` for an unknown or empty id, when the gate already has that power state, and while a save is loading or saving.
- `Changed` does not fire while a save is loading.
- `IsAvailable` is `false` when the change event could not be set up. Then `Changed` never fires. The reads, `Open`, `Close` and `SetPowered` keep working.
