# Interaction and locks API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.interaction`.
Files: `src/Api/Interactions.cs`, `src/Api/InteractionsPatches.cs`, `src/Api/Locks.cs`, `src/Api/LocksPatches.cs`.

Everything not listed here is internal.

## Wiring

```csharp
internal static void Interactions.Init();
```

Core calls this from `Core.Awake`.

`Init` installs the `UseInteractable` hook and the lock hooks under one owner. If any hook is missing, all of them are removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.interaction")` becomes true, and one warning is logged. Reads and `Add`/`Remove`/`Lock`/`Unlock` keep working; only the events stop.

## Threading and safety

- Main thread only.
- `Interactions.IsAvailable` covers the interaction and lock hooks. `Locks.IsAvailable` returns the same value.
- Every member is safe at any time. Off a save, or on the main menu, reads return `null`, `false` or `0`.
- Writes return `bool`. The only throw is `ArgumentNullException` (a subclass of `ArgumentException`) for a null target (or a null `done` in `StartLockpick`), and `ArgumentException` for a mod id outside 1 to 64 characters of letters, digits, `.`, `_` or `-`.
- Events are raised synchronously after the game method has finished. A handler that throws is logged and the other handlers still run (`Events.Raise`).

## Interactions

```csharp
public static class Interactions
{
    public static event Action<InteractionArgs> Used;

    public static bool Add(GameObject target, string modId, string label, Action use);
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

- `Add` makes `target` usable with the game's own prompt, `UseText` and crosshair. The prompt reads `label` and `(Use)`. It returns `false` when Core cannot patch, the target has no `Collider`, the target already has a game `Interactable` (its prompt would win), or another mod owns the target's interaction. The target's layer may move to a usable one, and `Remove` puts it back.
- `Remove` removes the interaction only when `modId` added it. It returns `false` otherwise. Calling `Remove` for the interaction being used (from inside its `use` action) defers destruction to the end of the frame; an `Add` for the same target in that same call returns `false`, so call `Add` alone to change the label.
- `Used` fires after the game's `InteractObjects.UseInteractable` (player input) has called `Interact()` on the target. It covers mod interactions too. `Kind` is the component's class name (`ModInteraction` for those added with `Add`). `Guid` is the object's save GUID, or `null`. `Distance` is from the camera origin to the object's pivot.
- `Looking` returns what the crosshair ray hits in the interaction layer mask, within the game's 3 m raycast range. `Kind` is `null` when the object has no game or mod `Interactable`. `Distance` is from the camera origin to the object's pivot, not to the ray hit point.

Not included: a cancelable `Using` event (`Events.Raise` does not collect results, so there is no clean skip rule), and hold-to-use. No mod has asked for either.

## Locks and doors

```csharp
public static class Locks
{
    public static event Action<LockArgs> Locked;
    public static event Action<LockArgs> Unlocked;
    public static event Action<LockArgs> LockpickSucceeded;
    public static event Action<LockArgs> LockpickFailed;

    public static bool IsLocked(GameObject target);
    public static bool IsLockpickable(GameObject target);
    public static bool PlayerHasKey(GameObject target);
    public static bool Lock(GameObject target);
    public static bool Unlock(GameObject target);
    public static bool SetKeypadOpen(GameObject target, bool open);
    public static bool LockKeypad(GameObject target);
    public static bool TriggerKeypad(GameObject target, bool success);
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
- `Locked` and `Unlocked` fire when the state changes through `Lock._Lock`, `Lock._Unlock`, `Door.Lock`, `Door.Unlock` or `Door.ToggleLock`. The state change is checked, so a chained call (a door unlocked inside a lock's unlock callback) raises one event. `LockArgs.Target` is the object: the door itself for doors, or the object that last called `Lock.Use` with a lock for other locks. It is `null` only if the game changed a lock before any use of it.
- `LockpickSucceeded` and `LockpickFailed` fire when a lockpick attempt started by `StartLockpick` finishes, with the target as `LockArgs.Target`. They also fire for the game's own lockpick path (`Lock.LockpickSuccessCallback`, through `LocksPatches`), with the object that was used as the lock owner. Cancelling a lockpick (`AbortLockpicking`) is not reported.
- `SetKeypadOpen` sets the keypad's `open` flag through `Keypad.SetOpen` (`Interaction/Keypad.cs:146`). It returns `true` when the flag equals `open` afterwards. No `Locked` or `Unlocked` event fires, because the lock hooks do not cover `Keypad`.
- `LockKeypad` calls `Keypad.Lock()` (`Keypad.cs:133`), which acts only while the keypad is open. It returns `IsLocked(target)` afterwards. It raises no event.
- `TriggerKeypad` calls `Keypad.TriggerSuccess()` (`:151`) when `success` is true, or `TriggerFailure()` (`:164`) when it is false. Success plays its sound and runs the success relay, and sets `open` when the keypad has `enableLockOnSuccess` (`:156`). It returns `true` when the call was made. It raises no event.
- `StartLockpick` calls `PlayerLockpicking.ToggleLockpicking` on `PlayerLockpicking.instance` (`Crime/PlayerLockpicking.cs:34`) with its own callback, not `Lock.LockpickSuccessCallback`, which needs the game's private `_parent` and `_CallbackUnlock`. It returns `false` when the target has no `Lock`, when the lock is not `Locked` and `Lockpickable`, when the instance is null, or when a minigame is already open (`ActiveLockpicking` is set). On a successful pick the callback unlocks the target with `Unlock`, then computes `unlocked` as the pick succeeded and the target is unlocked. `unlocked` drives both events: it raises `LockpickSucceeded` or `LockpickFailed`, and calls `done(unlocked)`. Each step is guarded, so an exception in one does not stop the others. It does not check that the player has a lockpick.
- A Core-started pick does not play the game's unlock sound, does not honour `UnlockDelay`, and does not re-arm `LockAfterTime`. The unlock is a direct `Lock._Unlock()` (`World/Lock.cs:306`), while the game's own pick path plays the sound and waits `UnlockDelay` (`Crime/PlayerLockpicking.cs:130`, `:134`) and re-arms the timer in `Lock.DelayUnlock` (`World/Lock.cs:268`).

```csharp
Locks.StartLockpick(door, 2f, ok => Log(ok ? "picked" : "failed"));
Locks.TriggerKeypad(keypad, true);
```

Not covered: the game writes `Locked` directly in a few places (`Button.cs:214`, `BreakableLock.cs:58`, `CraftingBase.cs:1560`, `PutItemSelection.cs:361`, `DoorChangelevel.cs:290`). Those changes raise no event. Keypad codes are not exposed. Keypad success and failure go through `TriggerKeypad`.
