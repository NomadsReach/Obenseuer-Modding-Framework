---
title: Locks
parent: Breakables
grand_parent: Objects and devices
nav_order: 3
---

# Locks

A lock can break when it is hit hard enough. A lock is not a breakable: it is a different kind of object, so `Broken` and `Repaired` never fire for it. Use `Locks()`, `BreakLock` and `LockBroken` for locks. There is no call to repair a lock.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Breakables</code> <span>break locks</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Breakables
{
    public static List<BreakableLockInfo> Locks();
    public static bool BreakLock(string id);

    public static event Action<BreakableArgs> LockBroken;
}
```

| Method | Returns | What it does |
|---|---|---|
| `Locks()` | `List<BreakableLockInfo>` | Every lock in the scene the player is in. |
| `BreakLock(string id)` | `bool` | Breaks a lock. `true` when it is broken afterwards. |

| Event | Gives you | When it fires |
|---|---|---|
| `LockBroken` | `BreakableArgs` | A lock has just broken. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BreakableLockInfo</code> <span>a snapshot of one lock</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class BreakableLockInfo
{
    public string Id { get; }
    public bool Locked { get; }
    public int Strength { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The id to pass to `BreakLock`. |
| `Locked` | `bool` | The game's lock state. Breaking a lock does not change it. |
| `Strength` | `int` | How strong the lock is. `0` or less once it is broken. |

</div>
</div>

## Things to know

- `BreakLock(id)` breaks the lock the way a hit would. <span class="tech">(It calls `BreakableLock.Break` with the lock's remaining strength as damage and `DamageReceiver.DamageType.Any`.)</span>
- Read `Strength` to see whether a lock is broken, not `Locked`. Breaking a lock does not turn `Locked` off, so a broken lock can still say `Locked` is `true`. The game disables a broken lock's collision and interaction, so the player cannot use it. <span class="tech">(`BreakableLock.Break` never sets `_Lock.Locked`. `UpdateModel` disables the lock's collider and interaction.)</span>
- `LockBroken` fires once, when a hit takes a lock's strength from above 0 to 0 or less. Its `BreakableArgs.Id` is the lock's id.

## Limits

- `Locks()` only lists locks that are switched on in the scene the player is in. A lock that is switched off is skipped. <span class="tech">(It searches the active scene with `FindObjectsByType` and skips inactive objects. A lock's id is made the same way as a breakable's id.)</span>
- `BreakLock(id)` returns `false` when no lock has that id, when the lock is already broken, or when a save is loading or being written. It returns `true` when the lock is broken afterwards.
- `LockBroken` does not fire while a save is loading, when a key or a lockpick opens the lock, or for a hit the lock does not accept. <span class="tech">(A key or lockpick unlock goes through the game's `Unlocked`, which sets the strength to 0 without a break. A hit that the lock's allowed damage types refuse does not change its strength.)</span>
