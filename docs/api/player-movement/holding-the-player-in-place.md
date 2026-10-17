---
title: Holding the player in place
parent: Player movement
grand_parent: Player life and skills
nav_order: 4
---

# Holding the player in place

A lock holds the player's controls, the way the game does in a cutscene. The player cannot walk, look or use keys while it is held. The calls are counted for each mod id: if your mod calls `Lock` twice, it must call `Unlock` twice.

```csharp
public static class PlayerMovement
{
    public static bool Lock(string modId);                                          // holds the player; false if it could not
    public static bool LockLookAt(string modId, Transform target, bool instantly);  // holds the player and turns the camera to a target
    public static bool Unlock(string modId);                                        // lets go of your newest lock; false if you hold none
}
```

- **`Lock(modId)` takes one lock for your mod.** The player is held as soon as the first lock from any mod is taken, and let go when the last one is released.
- **`Unlock(modId)` releases the newest lock that your mod took.** That can be a `Lock` or a `LockLookAt`. It returns `false` if your mod holds no lock. It always runs, also during a save, so a release is never refused.
- **The hold is the game's own way of freezing the player.** It also disables the player's keys and the camera, as the game does for a cutscene, and it does not show the mouse cursor. Other freezes the game uses are separate and are not affected.
  - Technical: it calls `GameController.ControlsDisabled` with one holder `GameObject`, which is what `DisablePlayerMovement.DisableMovement(false)` does.
- **`Lock` returns `false` and takes nothing when the game is not ready.** That is when the game controller, the player controller or the camera is not loaded, or while a save is loading or being written.
- **Your locks end when a save loads, and sometimes when a scene loads.** They are released when a save is loaded. They are also released on a scene load, but only when the game controller that took the hold is gone. Normal play loads neighbouring scenes alongside, and those keep the locks. After a save load, or a scene change that replaces the controller, your mod must lock again, and its later `Unlock` returns `false`.
  - Technical: locks are released on `Events.Loaded` and on `Events.SceneLoaded` when the game controller that took the hold is gone.
- **`Unlock` keeps the camera locks the game had before your `Lock`.** For example the cutscene and build-mode look locks stay as they were.
- **`LockLookAt(modId, target, instantly)` is the same hold as `Lock`, with the camera turned toward `target`.** With `instantly` set to `true` the camera snaps at once. It counts, releases and refuses the same way as `Lock`. It returns `false`, and takes nothing, when `target` is `null` or when the game's camera has changed in a way the framework does not know.
  - Technical: it is the game's look-at movement lock. It returns `false` when the camera's private `firstpersonSoftLock` or `lookAtTarget` field is missing.
- **The turn only happens in first person.** In third person the hold applies but the camera does not turn, and an `instantly` snap is undone on the next frame. The turn starts when the player switches to first person while the lock is held.
- **Only one look target is active at a time.** It is the newest one that still exists, from any mod. A later `LockLookAt` from any mod replaces it.
- **When you `Unlock` a look lock, the camera turns toward the newest look target that is left.** Targets that no longer exist are skipped. When no look lock is left, the camera goes back to what it was doing before the first look lock, even while plain `Lock` holds remain.
  - Technical: the camera's soft-lock flag and target go back to what they were before the first look lock.
- **The game wins.** The framework only changes where the camera looks while it still controls the camera. If the game starts its own look lock during your hold (a conversation, the card game, lockpicking) or clears it, the framework leaves the camera alone. It does not turn back to your target, and it does not restore the earlier state. A later `LockLookAt` saves the game's lock as the state to restore.
  - Technical: it owns the camera while the soft-lock flag is set and the target is the one the framework last applied. The release never clears a soft lock that the game set. The game's own `EnableMovementLookAtTarget` is not called.
- **A look target that has been destroyed just stops the turn.** No error is raised.
