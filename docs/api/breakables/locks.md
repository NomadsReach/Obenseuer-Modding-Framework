---
title: Locks
parent: Breakables
grand_parent: Objects and devices
nav_order: 3
---

# Locks

A lock can break when it is hit hard enough. A lock is not a breakable: it is a different kind of object, so `Broken` and `Repaired` never fire for it. Use `Locks()`, `BreakLock` and `LockBroken` for locks. There is no call to repair a lock.

```csharp
public static class Breakables
{
    public static List<BreakableLockInfo> Locks();      // every lock in the scene the player is in
    public static bool BreakLock(string id);            // breaks a lock; true when it is broken afterwards

    public static event Action<BreakableArgs> LockBroken;   // a lock has just broken
}

public sealed class BreakableLockInfo    // a snapshot of one lock
{
    public string Id { get; }          // the id to pass to BreakLock
    public bool Locked { get; }        // the game's lock state; breaking a lock does not change it
    public int Strength { get; }       // how strong the lock is; 0 or less once it is broken
}
```

- **`Locks()` only lists locks that are switched on in the scene the player is in.** A lock that is switched off is skipped.
  - Technical: it searches the active scene with `FindObjectsByType` and skips inactive objects. A lock's id is made the same way as a breakable's id.
- **`BreakLock(id)` breaks the lock the way a hit would.** It returns `false` when no lock has that id, when the lock is already broken, or when a save is loading or being written. It returns `true` when the lock is broken afterwards.
  - Technical: it calls `BreakableLock.Break` with the lock's remaining strength as damage and `DamageReceiver.DamageType.Any`.
- **Read `Strength` to see whether a lock is broken, not `Locked`.** Breaking a lock does not turn `Locked` off, so a broken lock can still say `Locked` is `true`. The game disables a broken lock's collision and interaction, so the player cannot use it.
  - Technical: `BreakableLock.Break` never sets `_Lock.Locked`. `UpdateModel` disables the lock's collider and interaction.
- **`LockBroken` fires once, when a hit takes a lock's strength from above 0 to 0 or less.** It does not fire while a save is loading. It does not fire when a key or a lockpick opens the lock, and it does not fire for a hit the lock does not accept. Its `BreakableArgs.Id` is the lock's id.
  - Technical: a key or lockpick unlock goes through the game's `Unlocked`, which sets the strength to 0 without a break. A hit that the lock's allowed damage types refuse does not change its strength.
