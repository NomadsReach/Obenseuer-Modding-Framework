---
title: Locks and doors
parent: Interaction and locks
grand_parent: Places and travel
nav_order: 3
---

# Locks and doors

A lock keeps a door, chest or other object shut until it is opened with a key, a pick or a code. The `target` for these calls is any object with a game lock on it, such as a door, a storage, a button, or a keypad.

```csharp
public static class Locks
{
    public static event Action<LockArgs> Locked;             // a lock has been locked
    public static event Action<LockArgs> Unlocked;           // a lock has been unlocked
    public static event Action<LockArgs> LockpickSucceeded;  // a lockpick attempt opened the lock
    public static event Action<LockArgs> LockpickFailed;     // a lockpick attempt failed

    public static bool IsAvailable { get; }    // the same value as Interactions.IsAvailable

    public static bool IsLocked(GameObject target);          // true when the object is locked
    public static bool IsLockpickable(GameObject target);    // true when the lock can be picked
    public static float Difficulty(GameObject target);       // how hard it is to pick, 0 to 2; -1 when the object has no lock
    public static bool PlayerHasKey(GameObject target);      // true when the player carries the key
    public static bool Lock(GameObject target);              // locks it; true when it is locked afterwards
    public static bool Unlock(GameObject target);            // unlocks it; true when it is unlocked afterwards
    public static bool SetLockpickable(GameObject target, bool lockpickable);  // lets the lock be picked or not
    public static bool SetDifficulty(GameObject target, float difficulty);     // 0 to 2; anything else throws ArgumentException
    public static bool SetKeypadOpen(GameObject target, bool open);   // opens or closes a keypad
    public static bool LockKeypad(GameObject target);                 // closes an open keypad again
    public static bool TriggerKeypad(GameObject target, bool success);// makes the keypad count as solved or failed
    public static int[] KeypadCode(string id);                        // a copy of the keypad's code; null when there is no such keypad
    public static bool SetKeypadCode(string id, int[] code);          // changes the keypad's code
    public static bool StartLockpick(GameObject target, float difficulty, Action<bool> done); // starts the minigame
}

public sealed class LockArgs    // the object a lock event is about
{
    public GameObject Target { get; }   // the object that was locked, unlocked or picked
    public string Name { get; }         // its name in the scene
    public string Guid { get; }         // its save id, or null
}
```

- **`IsLocked` works for locks and for keypads.** A keypad counts as locked while it has not been opened.
  - Technical: for a keypad it is `true` while its `open` flag is `false`. For anything else it is `Lock.Locked`.
- **`Lock` and `Unlock` return `true` when the object is in the state you asked for afterwards.** They return `false` while the game is loading or saving a save. A keypad can be locked but not unlocked this way, because the game opens a keypad only when the right code is entered, so `Unlock` on a keypad returns `false`. Use `SetKeypadOpen` for that.
  - Technical: a `Door` uses its own `Door.Lock()` and `Door.Unlock()`. Any other lock uses `Lock._Lock()` and `Lock._Unlock()`. A keypad is locked with `Keypad.Lock()`.
- **`PlayerHasKey` checks only for the key item.** It does not check whether the player has a lockpick.
  - Technical: it is the game's `Lock.PlayerHasKey()`.
- **`SetLockpickable` and `SetDifficulty` change what the game reads for lockpicking.** They return `true` when the lock now holds the value and `false` while the game is loading or saving, or when the object has no lock. A keypad has no lock of this kind, so they return `false` for it. Neither one raises a lock or lockpick event.
  - Technical: they set `Lock.Lockpickable` and `Lock.Difficulty`. The two fields are saved with the game's own save, next to `Locked`. A door saves its lock with its own save, and other lock holders depend on their own save.
- **`Difficulty` is a number from 0 to 2, and the game's normal value is 2.** It returns `-1` when the object has no lock, or when reading it fails. `0` is a real difficulty (the easiest), so a negative number is the only sign of "no lock". Check `IsLockpickable` first when that matters.
- **`SetDifficulty` throws `ArgumentException` for a number below 0, above 2 or not a number.** Pass a value from 0 to 2.
- **`Locked` and `Unlocked` fire when a lock really changes state.** A lock that was already in that state does not fire. If one change causes another, such as a door that is unlocked from inside a lock's unlock step, you get one event, not two. `LockArgs.Target` is the door itself for doors. For other locks it is the object that last used the lock, and it is `null` if the game changed the lock before anything used it.
  - Technical: they are raised after `Lock._Lock`, `Lock._Unlock`, `Door.Lock`, `Door.Unlock` and `Door.ToggleLock`. For other locks the target is the object that last called `Lock.Use`. The game also writes `Locked` directly in a few places, and those changes raise no event.
- **`LockpickSucceeded` and `LockpickFailed` fire when a pick attempt ends.** That is true for attempts your mod starts with `StartLockpick` and for the game's own lockpicking. If the player cancels or looks away, neither event fires. For an attempt your mod started, `done` is called with `false` in that case.
  - Technical: the game's `Lock.LockpickSuccessCallback` is watched for the game's own picks, and `PlayerLockpicking.StopLockpicking` calls `Lock.AbortLockpicking` on a cancel. A level change during the minigame leaves the framework's record of that attempt armed, so `StartLockpick` refuses until the game's next stop on that lock arrives, and that stop can report one late `done(false)` for the old attempt.
- **`StartLockpick` opens the lockpicking minigame on a locked, pickable object.** It returns `true` when the minigame started. It returns `false` when the object has no lock, is not locked, can't be picked, when a minigame is already open, or while the game is loading or saving. A null `done` throws `ArgumentNullException`. It does not check that the player has a lockpick. When the player wins, the lock is unlocked and `done(true)` runs. Otherwise `done(false)` runs.
  - Technical: it calls `PlayerLockpicking.ToggleLockpicking` on `PlayerLockpicking.instance` with its own callback, not `Lock.LockpickSuccessCallback`, which needs the game's private `_parent` and `_CallbackUnlock`. On a success the callback calls `Unlock` and then computes `unlocked` as "the pick succeeded and the target is unlocked". `unlocked` drives both the lockpick events and `done`. Each step is guarded, so an error in one does not stop the others.
- **A pick that your mod starts is plainer than the game's own.** It does not play the unlock sound, does not wait for the unlock delay, and does not start the lock's timer for locking itself again.
  - Technical: the unlock is a direct `Lock._Unlock()`. The game's own path plays the sound and waits `UnlockDelay` in `PlayerLockpicking`, and re-arms the timer in `Lock.DelayUnlock` (`LockAfterTime`).
- **`SetKeypadOpen` opens or closes a keypad.** It returns `true` when the keypad is in the state you asked for, and `false` for an object that is not a keypad or while the game is loading or saving. No `Locked` or `Unlocked` event fires for a keypad.
  - Technical: it calls `Keypad.SetOpen`. The framework's lock change events do not cover `Keypad`.
- **`LockKeypad` closes a keypad that was open.** It returns whether the keypad is locked afterwards, and fires no event. A keypad that is already closed stays as it is.
  - Technical: it calls `Keypad.Lock()`, which acts only while the keypad is open.
- **`TriggerKeypad` makes the keypad react as if the code was right (`true`) or wrong (`false`).** On success it plays its sound, runs the keypad's success actions, and opens it when the keypad is set to stay open. It returns `true` when the call was made and fires no event.
  - Technical: it calls `Keypad.TriggerSuccess()` or `TriggerFailure()`. A keypad stays open on success when it has `enableLockOnSuccess`.
- **`KeypadCode(id)` gives a copy of the keypad's code, or `null` when the scene has no such keypad.** The id is the keypad's save id, which is the `Guid` from `InteractionArgs`. A keypad with a row of digit buttons uses the numbers set on those buttons in the scene. A rotary lock (a dial) uses positions 1 to 32.
  - Technical: it uses the same lookup as other scene ids, so the keypad has to be active in the loaded scene. The digit values are not checked.
- **`SetKeypadCode(id, code)` changes a keypad's code and returns `true`.** It returns `false` for a null or empty code, a code longer than 4 numbers, a number below 0 or above 32, while the game is loading or saving, or when the scene has no such keypad. Both kinds of keypad take at most 4 numbers. A shorter code matches on its start: only its first few digits are checked, so `{1, 2}` accepts any entry that starts with 1 then 2.
  - Technical: `KeypadUI` compares the entered digits to `code` in order, and the rotary lock feeds the same method (`RotaryLockUI`). A code longer than 4 would read past the entered digits, so it is refused.
- **A code you set is lost when a save loads.** The game does not save the code, and loading a save builds the keypad again from the scene, so the scene's own code comes back. Set your code again after each load.
  - Technical: `Keypad.code` is `[fsIgnore]`, so the save data does not hold it. Every save load reloads the level scene.
