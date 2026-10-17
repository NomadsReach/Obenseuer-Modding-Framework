---
title: Common tasks
parent: Power devices
grand_parent: Objects and devices
nav_order: 1
---

# Common tasks

Show the fuel left in every device that burns fuel. The numbers are seconds of burn time.

```csharp
foreach (PowerDeviceInfo device in PowerDevices.All())
{
    if (device.MaxFuel > 0f)
        Log.Info(device.Id + " has " + device.FuelLevel + " of " + device.MaxFuel + " seconds of fuel");
}
```

Read one device. Take the id from `PowerDevices.All()`. `Get` gives `null` when no device has that id.

```csharp
string id = "some id from PowerDevices.All()";
PowerDeviceInfo one = PowerDevices.Get(id);
if (one != null) Log.Info("Switch on: " + one.IsOn + ", running: " + one.IsPowered);
```

Switch a device on. The call gives `true` when the device is in the state you asked for afterwards.

```csharp
bool on = PowerDevices.SetOn(id, true);
Log.Info(on ? "The device is switched on." : "Could not switch it on.");
```

Check whether the events work.

```csharp
if (!PowerDevices.IsAvailable) Log.Warn("Power events are off.");
```
