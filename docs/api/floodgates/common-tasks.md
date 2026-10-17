---
title: Common tasks
parent: Flood gates
grand_parent: Objects and devices
nav_order: 1
---

# Common tasks

Read one gate. Take the id from `FloodGates.All()`. `Get` gives `null` when no gate has that id.

```csharp
string id = "some id from FloodGates.All()";

FloodGateInfo gate = FloodGates.Get(id);
if (gate != null) Log.Info(gate.Name + ": open " + gate.IsOpen + ", powered " + gate.IsPowered);

Log.Info("Open: " + FloodGates.IsOpen(id) + ", powered: " + FloodGates.IsPowered(id));
```

Open a gate. The call gives `true` only when the gate really started to open. It gives `false` when the gate is already open, has no power, is in an alarm, or the game refuses for another reason.

```csharp
if (FloodGates.Open(id)) Log.Info("The gate started to open.");
else Log.Info("The gate did not move.");
```

Close a gate.

```csharp
FloodGates.Close(id);
```

Cut the power to a gate, then restore it. Each call gives `true` only when the power really changed.

```csharp
FloodGates.SetPowered(id, false);
FloodGates.SetPowered(id, true);
```

Check whether the open and close event works.

```csharp
if (!FloodGates.IsAvailable) Log.Warn("Flood gate events are off.");
```
