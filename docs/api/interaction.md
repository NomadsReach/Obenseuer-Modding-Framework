---
title: Interaction and locks API
parent: API reference
nav_order: 30
---

# Interaction and locks API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.interaction`.
Files: `src/Api/Interactions.cs`, `src/Api/InteractionsPatches.cs`, `src/Api/Locks.cs`, `src/Api/LocksPatches.cs`.

Everything not listed here is internal.

## If a hook cannot be installed

The framework installs the `UseInteractable` hook and the lock hooks under one owner. If any hook is missing, all of them are removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.interaction")` becomes true, and one warning is logged. Reads and `Add`/`Remove`/`Lock`/`Unlock` keep working; only the events stop.

## Threading and safety

- Main thread only.
- `Interactions.IsAvailable` covers the interaction and lock hooks. `Locks.IsAvailable` returns the same value.
- Every member is safe at any time. Off a save, or on the main menu, reads return `null`, `false` or `0`.
- Writes return `bool`. `Interactions.Add` returns `false` while a save loads or saves (`SaveData.Busy`), after its argument checks. The only throws are `ArgumentNullException` (a subclass of `ArgumentException`) for a null target, label or use (or a null `done` in `StartLockpick`), and `ArgumentException` for a bad mod id. `Interactions.Remove` and `SetReason` require a mod id of 1 to 64 characters of letters, digits, `.`, `_` or `-`. `Interactions.Add` requires only a non-empty mod id, so an id that `Remove` rejects can still be added.
- Events are raised synchronously after the game method has finished. A handler that throws is logged and the other handlers still run.

## Interactions

```csharp
public static class Interactions
{
    public static event Action<InteractionArgs> Used;

    public static bool IsAvailable { get; }

    public static bool Add(GameObject target, string modId, string label, Action use);
    public static bool Add(GameObject target, string modId, string label, string verb, Action use);
    public static bool SetReason(GameObject target, string modId, Func<string> reason);
    public static bool Remove(GameObject target, string modId);
    public static InteractionArgs Looking();
}

public sealed class InteractionArgs
{
    public string Name { get; }
    public string Kind { get; }
    public Vector3 Position { get; }
    public float Distance { get; }
    public string Guid { get; }
    public GameObject Target { get; }
}
```

- `Add` makes `target` usable with the game's own prompt, `UseText` and crosshair. The prompt reads `label` and `(Use)`. It returns `false` when the framework cannot patch, the target has no `Collider`, the target already has a game `Interactable` (its prompt would win), or another mod owns the target's interaction (one warning names both mods). It throws `ArgumentNullException` for a null `target`, `label` or `use`, and `ArgumentException` for an empty `modId`. These checks run before the save check.
- The layer rule: if the target's layer is one of 17, 23, 29, 20 or 15, and the interact mask (`InteractObjects.MaskList`) includes it, the layer is kept. Otherwise `Add` moves the target to the first of 17, 23, 29, 20 and 15 that the mask includes, tried in that order. If none of them is usable, `Add` returns `false`. `Remove` restores the original layer.
- `Add` with `verb` sets the word in brackets that the prompt shows after the label. `Add(treadmill, "example.gym", "Treadmill", "Run", use)` shows `Treadmill` and `(Run)`. Pass the bare word: the game adds the brackets, and a verb that already starts with `(` is shown as given. A null, empty or whitespace-only verb uses `Use`. The four-argument `Add` is the same as the five-argument one with `Use`.
- `SetReason` sets a red reason that the prompt shows in place of the verb, the way the game shows a bed that cannot be used yet. The callback returns the text. A null or empty result means no reason, and the normal prompt shows. The reason is shown in brackets like the verb: a reason of `Too hungry` shows as `(Too hungry)`. Text that starts with `(` is shown as given. The callback runs each time the game updates the prompt for the target: on the first-person look within use range, and when the target is selected in the third-person list. In third person the list shows only the title, but selecting an entry still makes it the game's current interactable, so the use is still blocked with the warning effect. While a reason is set, use plays the game's warning effect (`UseText.WarningUseEffect`) and does not call the `use` action. `Used` still fires, because the game still calls `Interact()`. `SetReason(target, modId, null)` clears the reason. It returns `false` when `modId` has no interaction on `target`, or when that interaction is being removed. A reason stays when the same mod calls `Add` again, and `Remove` drops it. If the callback throws, the first throw is logged once per target and the target is treated as having no reason. Keep the callback quick and main-thread only, since it can run every frame.
- `Remove` removes the interaction only when `modId` added it. It returns `false` otherwise. Calling `Remove` for the interaction being used (from inside its `use` action) defers destruction to the end of the frame; an `Add` for the same target in that same call returns `false`, so call `Add` alone to change the label.
- `Used` fires after the game's `InteractObjects.UseInteractable` (player input) has called `Interact()` on the target. It covers mod interactions too. `Kind` is the component's class name (`ModInteraction` for those added with `Add`). `Guid` is the object's save GUID, or `null`. `Distance` is from the camera origin to the object's pivot. The postfix runs after a skipped original too: if another mod's prefix on `UseInteractable` returns false and skips the game's `Interact()` call, `Used` can still fire, provided the facade's prefix ran first. The prefix order was not checked.
- `Looking` returns what the crosshair ray hits in the interaction layer mask, within the game's 3 m raycast range. `Kind` is `null` when the object has no game or mod `Interactable`. `Distance` is from the camera origin to the object's pivot, not to the ray hit point.

Not included: a cancelable `Using` event (the handlers' results are not collected, so there is no clean skip rule), and hold-to-use. No mod has asked for either.

## Locks and doors

```csharp
public static class Locks
{
    public static event Action<LockArgs> Locked;
    public static event Action<LockArgs> Unlocked;
    public static event Action<LockArgs> LockpickSucceeded;
    public static event Action<LockArgs> LockpickFailed;

    public static bool IsAvailable { get; }

    public static bool IsLocked(GameObject target);
    public static bool IsLockpickable(GameObject target);
    public static float Difficulty(GameObject target);            // -1f when the target has no Lock
    public static bool PlayerHasKey(GameObject target);
    public static bool Lock(GameObject target);
    public static bool Unlock(GameObject target);
    public static bool SetLockpickable(GameObject target, bool lockpickable);
    public static bool SetDifficulty(GameObject target, float difficulty); // ArgumentException outside 0 to 2
    public static bool SetKeypadOpen(GameObject target, bool open);
    public static bool LockKeypad(GameObject target);
    public static bool TriggerKeypad(GameObject target, bool success);
    public static int[] KeypadCode(string id);
    public static bool SetKeypadCode(string id, int[] code);
    public static bool StartLockpick(GameObject target, float difficulty, Action<bool> done);
}

public sealed class LockArgs
{
    public GameObject Target { get; }
    public string Name { get; }
    public string Guid { get; }
}
```

- `target` is any object with a public game `Lock` field (doors, storages, buttons, and others), a `Door`, or a `Keypad`.
- `IsLocked`: for a keypad it is `true` while its `open` flag is `false`. For a lock it is `Lock.Locked`.
- `Lock` and `Unlock` return `true` when the target is in the requested state afterwards. A `Door` uses its own `Door.Lock()` and `Door.Unlock()`. Any other lock uses `Lock._Lock()` and `Lock._Unlock()`. A keypad can be locked (`Keypad.Lock()`) but not unlocked, because the game has no unlock method for it other than its success path. `Unlock` on a keypad returns `false`. `Lock`, `Unlock`, `SetKeypadOpen`, `LockKeypad`, `TriggerKeypad` and `StartLockpick` return `false` while the game loads or saves.
- `PlayerHasKey` is the game's own key-item check (`Lock.PlayerHasKey()`). It does not check the lockpick.
- `SetLockpickable` and `SetDifficulty` set `Lock.Lockpickable` and `Lock.Difficulty` on the target's `Lock`, the fields the game reads for lockpicking. A keypad has no `Lock`, so it returns `false`. Both return `false` while `SaveData.Busy`, and `true` when the field now holds the value. Neither raises `Locked`, `Unlocked` or a lockpick event.
- The two fields are saved with the game's own save, next to `Locked`. A door saves its `_Lock` with its own save. Other lock holders depend on their own save.
- `Difficulty` is a value from `0` to `2`. The game's default is `2`. It returns `-1f` when the target has no `Lock` or the read fails. `0` is a real difficulty, a lock set to `0`, so a negative value means no lock. Check `IsLockpickable` first when that matters.
- `SetDifficulty` throws `ArgumentException` for NaN or a value outside `0` to `2`.
- `Locked` and `Unlocked` fire when the state changes through `Lock._Lock`, `Lock._Unlock`, `Door.Lock`, `Door.Unlock` or `Door.ToggleLock`. The state change is checked, so a chained call (a door unlocked inside a lock's unlock callback) raises one event. `LockArgs.Target` is the object: the door itself for doors, or the object that last called `Lock.Use` with a lock for other locks. It is `null` only if the game changed a lock before any use of it.
- `LockpickSucceeded` and `LockpickFailed` fire when a lockpick attempt started by `StartLockpick` finishes, with the target as `LockArgs.Target`. They also fire for the game's own lockpick path (`Lock.LockpickSuccessCallback`, through `LocksPatches`), with the object that was used as the lock owner. Cancelling is different: when the player cancels or looks away, the game's `PlayerLockpicking.StopLockpicking` calls `Lock.AbortLockpicking`, and the framework then calls `done(false)` for the attempt it started. No `LockpickSucceeded` or `LockpickFailed` is raised for a cancel. A level change during the minigame leaves the framework's record for that attempt armed, so `StartLockpick` refuses until the next game-side stop on that lock arrives, and that stop can report one late `done(false)` for the old attempt.
- `SetKeypadOpen` sets the keypad's `open` flag through `Keypad.SetOpen`. It returns `true` when the flag equals `open` afterwards. No `Locked` or `Unlocked` event fires, because the lock hooks do not cover `Keypad`.
- `LockKeypad` calls `Keypad.Lock()`, which acts only while the keypad is open. It returns `IsLocked(target)` afterwards. It raises no event.
- `TriggerKeypad` calls `Keypad.TriggerSuccess()` when `success` is true, or `TriggerFailure()` when it is false. Success plays its sound and runs the success relay, and sets `open` when the keypad has `enableLockOnSuccess`. It returns `true` when the call was made. It raises no event.
- `StartLockpick` calls `PlayerLockpicking.ToggleLockpicking` on `PlayerLockpicking.instance` with its own callback, not `Lock.LockpickSuccessCallback`, which needs the game's private `_parent` and `_CallbackUnlock`. It returns `false` when the target has no `Lock`, when the lock is not `Locked` and `Lockpickable`, when the instance is null, or when a minigame is already open (`ActiveLockpicking` is set). On a successful pick the callback unlocks the target with `Unlock`, then computes `unlocked` as the pick succeeded and the target is unlocked. `unlocked` drives both events: it raises `LockpickSucceeded` or `LockpickFailed`, and calls `done(unlocked)`. Each step is guarded, so an exception in one does not stop the others. It does not check that the player has a lockpick.
- A pick started by the framework does not play the game's unlock sound, does not honour `UnlockDelay`, and does not re-arm `LockAfterTime`. The unlock is a direct `Lock._Unlock()`, while the game's own pick path plays the sound and waits `UnlockDelay` (`PlayerLockpicking`) and re-arms the timer in `Lock.DelayUnlock`.

```csharp
Locks.StartLockpick(door, 2f, ok => Log(ok ? "picked" : "failed"));
Locks.TriggerKeypad(keypad, true);
```

Not covered: the game writes `Locked` directly in a few places. Those changes raise no event. Keypad success and failure go through `TriggerKeypad`.

- `KeypadCode(id)` returns a copy of the keypad's `code` array, or `null` when no active keypad has that id. The values of a digit keypad's buttons are set in the scene and are not checked here. A rotary lock reports positions `1` to `32` (`RotaryLockUI`, which passes them to `KeypadUI.NumberPressed`), so a rotary code uses those values.
- `SetKeypadCode(id, code)` copies `code` into the keypad's `code` array and returns `true`. It returns `false` for a null or empty `code`, for a code longer than 4 values, for a value outside `0` to `32`, during a save or load, or when no active keypad has that id. Both keypad kinds take at most 4 inputs (`KeypadUI`, and the rotary lock feeds the same method at `RotaryLockUI`). The UI checks the entered digits against `code` in order (`KeypadUI`), so a code shorter than 4 matches on its prefix: only its first `code.Length` digits are compared. A code longer than 4 would read past the entered digits, so it is refused.
- The code is not saved. `Keypad.code` is `[fsIgnore]`, so the save data does not hold it. Every save load reloads the level scene, which rebuilds the keypad, so a set code is lost on load and the scene's code comes back.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
