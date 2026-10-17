---
title: Common tasks
parent: Breakables
grand_parent: Objects and devices
nav_order: 1
---

# Common tasks

Count the broken breakables in the scene the player is in. The list only holds objects that are switched on in that scene.

```csharp
int broken = 0;
foreach (BreakableInfo breakable in Breakables.All())
    if (breakable.IsBroken) broken++;
Log.Info(broken + " breakables are broken.");
```

Break one breakable. `Break` returns true when the breakable is broken afterwards, and false when nothing happened.

```csharp
foreach (BreakableInfo breakable in Breakables.All())
{
    if (breakable.IsBroken) continue;
    if (Breakables.Break(breakable.Id)) Log.Info("Broke " + breakable.Name);
    break;
}
```

Repair a broken breakable. This only works for a breakable that has a repair timer.

```csharp
foreach (BreakableInfo breakable in Breakables.All())
    if (breakable.IsBroken && Breakables.Repair(breakable.Id))
        Log.Info("Repaired " + breakable.Name);
```

List the locks that are still whole, and break the first one.

```csharp
foreach (BreakableLockInfo breakableLock in Breakables.Locks())
{
    if (breakableLock.Strength <= 0) continue;
    Breakables.BreakLock(breakableLock.Id);
    break;
}
```
