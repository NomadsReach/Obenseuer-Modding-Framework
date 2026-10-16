---
title: Breakables API
parent: API reference
nav_order: 6
---

# Breakables API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.breakables`.
Files: `src/Api/Breakables.cs`, `src/Api/BreakablesPatches.cs`.

Everything not listed here is internal. The locks (`BreakableLock`) are in [Locks](#locks).

## If a hook cannot be installed

The framework installs the `Break` hook on `Breakable`, the `Repair` and `OnLoadingGame` hooks on `Breakable`, the `Break` hook on `BreakableLock`, and the `Damage` hook on `SpawnItemOnBreak`, one group owner each: `obenseuer.moddingframework.breakables.break`, `.repair`, `.lock` and `.smash`. If a hook is missing, only its group owner is removed, `Breakables.IsAvailable` becomes false, and one warning is logged. `Broken` needs the break group, `Repaired` the repair group, `LockBroken` the lock group and `Smashed` the smash group, so a missing group stops only its own event. `All`, `Get`, `IsBroken`, `Break`, `Repair`, `Locks` and `BreakLock` keep working.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no scene loaded, or on the main menu, reads return an empty list, `null` or `false`.
- Writes return `bool`. Nothing throws to the caller. An unknown or empty id returns `false`.
- A broken object is not broken again: `Break` returns `false` when the object is already broken. It also returns `false` when the object has no parent.
- `Repair` returns `false` when the object is not broken, or when its `repairAfterTime` is below 0, which means the object has no repair timer.
- `Broken`, `Repaired` and `Smashed` are raised synchronously, so a throwing handler is logged and the other handlers still run.
- No event is raised while a save loads (`SaveController.Loading`), including a repair timer that expired while the game was closed.
- `Break` returns `false` while a save is loading or being written.
- `Repair` returns `false` while a save is loading or being written.

## Breakables

```csharp
public static class Breakables
{
    public static List<BreakableInfo> All();
    public static BreakableInfo Get(string id);
    public static bool IsBroken(string id);
    public static bool Break(string id);
    public static bool Repair(string id);

    public static List<BreakableLockInfo> Locks();
    public static bool BreakLock(string id);

    public static event Action<BreakableArgs> Broken;
    public static event Action<BreakableArgs> Repaired;
    public static event Action<BreakableArgs> LockBroken;
    public static event Action<SmashedArgs> Smashed;

    public static bool IsAvailable { get; }
}

public sealed class BreakableInfo
{
    public string Id { get; }              // the save GUID, or a scene path when it has none
    public string Name { get; }            // the GameObject name
    public bool IsBroken { get; }          // Breakable.strength <= 0 when the list was built
}

public sealed class BreakableArgs
{
    public string Id { get; }              // the same id as BreakableInfo.Id, or the lock's id for LockBroken
}

public sealed class BreakableLockInfo
{
    public string Id { get; }              // the save GUID, or a scene path when it has none
    public bool Locked { get; }            // BreakableLock.Locked, the game's lock state. A break does not change it.
    public int Strength { get; }           // BreakableLock.strength, 0 or less once broken
}

public sealed class SmashedArgs
{
    public string Id { get; }              // the save GUID of the object or of a SavableScript on it, or a scene path when it has none
    public Vector3 Position { get; }       // transform.position before the break
    public bool IsAnimal { get; }          // objectType == SpawnItemOnBreak.Type.Animal
    public int SpawnItemId { get; }        // itemToSpawn.Item.ID, or -1 when there is no item or its ID is 0
}
```

## Notes

- `Break` applies the object's remaining strength as damage, with no push direction. The game's own break effects, sound, child pieces and `onBreak` relay run as they do for a hit.
- `Repair` restores strength only. It does not rebuild the model the break removed. This is the game's own behaviour, and the game's `repairAfterTime` timer does the same.

## Locks

Each lock is a `BreakableLock`. It derives from `InteractableDamageReceiver`, not from `Breakable`, so `Broken` and `Repaired` never fire for a lock.

```csharp
public static List<BreakableLockInfo> Locks();
public static bool BreakLock(string id);
public static event Action<BreakableArgs> LockBroken;
```

- `Locks` lists every `BreakableLock` in the active scene. It scans the scene with `FindObjectsByType`, so an inactive lock is skipped.
- `BreakLock` applies the lock's remaining strength as damage through `BreakableLock.Break`, as `Break` does for a breakable. It returns `false` when no lock has the id, when the lock is already broken (strength 0 or less), or when a save is busy. It returns `true` when the strength is 0 or less afterwards.
- A break does not change `Locked`. The game's `Break` never sets `_Lock.Locked`, so a broken lock can still read `Locked` as `true`. A broken lock cannot be used: `UpdateModel` disables its collider and interaction. Read `Strength` for broken.
- `LockBroken` fires when a break takes the lock's strength from above 0 to 0 or less. The args carry the lock's id. It does not fire on a save load, on a key or lockpick unlock (the game's `Unlocked` sets strength to 0 without a break, `BreakableLock`), or on a hit that the lock's damage types refuse.

## Smashed

`SpawnItemOnBreak` is a `DamageReceiver` that spawns its item when a hit takes its `health` to 0 or less. It is not a `Breakable`, so `Smashed` never fires for a breakable and `Broken` never fires for a `SpawnItemOnBreak`.

```csharp
public static event Action<SmashedArgs> Smashed;
```

- `Smashed` fires when a hit takes `health` from above 0 to 0 or less. The check is `CheckDamageType(type) && health - damage <= 0`, made before the game's `Damage` runs. A hit that `allowedDamageTypes` refuses does not fire it.
- An object that is not destroyed keeps breaking. Once `health` is 0 or less, the game runs its break branch on every hit and re-spawns the loot each time. So `Smashed` fires on each of those hits too. It is not one-shot.
- `Destroy()`, which `AnimalStats` calls when an animal dies, goes through the same `Damage` path, so it fires `Smashed` too.
- The event runs after the game's break code, so the object may already be destroyed. The args hold the values captured before the break, and a handler should not read the object.
- The event observes only. It cannot change the break, the loot, the crime record or a save.
- No event is raised while a save loads.
