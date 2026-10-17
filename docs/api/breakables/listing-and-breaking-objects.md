---
title: Listing and breaking objects
parent: Breakables
grand_parent: Objects and devices
nav_order: 2
---

# Listing and breaking objects

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

A breakable is an object with a strength. `Breakables` lets you list them, look one up by its id, break it, repair it, and hear when the game breaks or repairs one. Each breakable you get back is a `BreakableInfo`, a snapshot taken when you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public static class Breakables
{
    public static List<BreakableInfo> All();          // every breakable in the scene the player is in
    public static BreakableInfo Get(string id);       // the breakable with this id; null when none matches
    public static bool IsBroken(string id);           // true when the breakable exists and is broken
    public static bool Break(string id);              // breaks it; true when it is broken afterwards
    public static bool Repair(string id);             // repairs it; true when it is whole afterwards

    public static event Action<BreakableArgs> Broken;      // a breakable has just broken
    public static event Action<BreakableArgs> Repaired;    // a breakable has just been repaired

    public static bool IsAvailable { get; }           // false when any of the game changes below could not be set up
}

public sealed class BreakableInfo    // a snapshot of one breakable
{
    public string Id { get; }          // the id to pass to the other calls; the game's saved number is stable between sessions
    public string Name { get; }        // the object's name in the scene
    public bool IsBroken { get; }      // true when its strength was 0 or less when you asked
}

public sealed class BreakableArgs    // what an event tells you
{
    public string Id { get; }          // the id of the breakable (or of the lock, for LockBroken)
}
```

- **A breakable's id is the number the game saves it under, or a path through the scene when it has none.** Use the id with `Get`, `IsBroken`, `Break` and `Repair`. An id for an object with no saved number is a path such as `SceneName:Root[0]/Child[2]`, and it changes if objects in the scene are reordered.
  - Technical: `Id` is the object's `GUID` field when it has one, otherwise the GUID of the `SavableScript` on its object, otherwise a path counted from the scene root with sibling positions.
- **`All()` and the id lookups only see objects that are switched on in the scene the player is in.** A breakable that is switched off, or in another scene, is not listed, and an id for it gives `null` or `false`.
  - Technical: they search the active scene with `FindObjectsByType` and skip inactive objects.
- **`Break(id)` breaks the object the same way a hit would, so its normal break effects play.** The sound, the pieces that fall off and anything the game attaches to the break all happen. The hit has no push direction, so nothing is knocked sideways.
  - Technical: it calls `Breakable.Break` with the object's remaining strength as damage, zero vectors for the direction and `DamageReceiver.DamageType.Any`. The `onBreak` relay runs as it does for a hit.
- **`Break(id)` returns `false` when nothing was broken.** That happens when the id matches nothing, when the object is already broken, when a save is loading or being written, and when the object has no parent in the scene.
  - Technical: the checks are an unknown id, `strength <= 0`, `SaveGate.Busy` and `transform.parent == null`.
- **`Repair(id)` makes the object whole again but does not rebuild what the break removed.** The game does the same when its own repair timer runs out, so a repaired object can look different from a new one.
  - Technical: it calls `Breakable.Repair`, which restores `strength` only. It does not rebuild the model.
- **`Repair(id)` returns `false` when nothing was repaired.** That happens when the id matches nothing, when the object is not broken, when a save is loading or being written, and when the object has no repair timer. An object has no repair timer when its repair time is below 0.
  - Technical: the check is `repairAfterTime < 0`.
- **`Broken` fires once, when a breakable goes from whole to broken.** It does not fire for an object that was already broken, and it does not fire while a save is loading.
  - Technical: it runs after `Breakable.Break`, and only when `strength` is 0 or less and the object was not already broken. Calls made with `onLoadingGame` set are ignored.
- **`Repaired` fires once, when a broken breakable becomes whole again.** It does not fire while a save is loading. That includes an object whose repair timer ran out while the game was closed.
  - Technical: it runs after `Breakable.Repair`. A repair that `Breakable.OnLoadingGame` restored from an expired timer is remembered and skipped.
- **`IsAvailable` is `false` when any part of the break, repair, lock or smash events could not be set up.** The calls that read or change things keep working, and only the event whose setup failed stops. See [If something fails to set up](details.md#if-something-fails-to-set-up).
