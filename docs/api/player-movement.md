---
title: Player movement API
parent: API reference
nav_order: 54
---

# Player movement API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.playermovement`.
Files: `src/Api/PlayerMovement.cs`, `src/Api/PlayerMovementPatches.cs`.

Everything not listed here is internal.

## Threading and safety

- Main thread only.
- Every member is safe at any time. On the main menu, or with no player, reads return `false`, `Vector3.zero` or the stored multiplier, and `Lock` returns `false`.
- If the controller hooks did not install, `IsAvailable` is `false` and the multipliers stop applying. Reads, `Lock` and `Unlock` keep working.
- `Lock` and `Unlock` return `bool`. Nothing throws except `ArgumentException` for an invalid mod id, or for a `NaN` written to the `WalkSpeedMultiplier` or `JumpMultiplier` property. The `Set*Multiplier` methods return `false` for `NaN` or an infinite value.

## Surface

```csharp
public static class PlayerMovement
{
    public static bool IsAvailable { get; }

    public static bool IsGrounded { get; }
    public static bool IsSprinting { get; }
    public static bool IsCrouching { get; }
    public static Vector3 Velocity { get; }
    public static Vector3 Position { get; }

    public static float WalkSpeedMultiplier { get; set; }
    public static float JumpMultiplier { get; set; }

    public static PlayerMovementInfo State();
    public static bool SetSpeedMultiplier(float multiplier);
    public static bool SetSpeedMultiplier(string owner, float multiplier);
    public static bool SetWalkMultiplier(string owner, float multiplier);
    public static bool SetRunMultiplier(string owner, float multiplier);
    public static bool SetJumpMultiplier(string owner, float multiplier);
    public static bool ClearMultipliers(string owner);

    public static bool Crouch();
    public static bool StandUp();

    public static bool Lock(string modId);
    public static bool LockLookAt(string modId, Transform target, bool instantly);
    public static bool Unlock(string modId);
    public static bool LockLook(string modId);
    public static bool UnlockLook(string modId);
}

public sealed class PlayerMovementInfo
{
    public float WalkSpeed { get; }
    public float RunSpeed { get; }
    public float SwimmingSpeed { get; }
    public float CrouchSpeed { get; }
    public float BackwardSpeed { get; }
    public float StrafeSpeed { get; }
    public bool IsCrouching { get; }
    public bool IsWalking { get; }
    public bool IsGrounded { get; }
    public bool ControlsDisabled { get; }
}
```

## Reads

- `IsGrounded`: `OpenSewerCharacterController.movement.isGrounded` (through `BaseCharacterController.isGrounded`).
- `IsCrouching`: `OpenSewerCharacterController.isCrouching`.
- `IsSprinting`: true when run speed is the selected speed: not crouching, and either the Walk toggle is in run mode or the input is a controller. The game reads the same condition in `GetTargetSpeed`. Swimming is not checked, and no movement check is made, so a stopped player who has run mode selected reads `true`.
- `Velocity`: `movement.velocity`, the character's current velocity in world space.
- `Position`: the controller's transform position (the player body).
- `State()`: a snapshot of the speeds and flags above. It returns `null` when no player is loaded.
  - `WalkSpeed`, `CrouchSpeed`, `BackwardSpeed`, `StrafeSpeed`: the controller's current values.
  - `RunSpeed` and `SwimmingSpeed`: the controller's getter values, which include the game's orange mushroom run modifier. When the orange mushroom effect is not present, they are the unmodified base speeds.
  - `IsWalking`: the Walk toggle (true means walk mode). The controller's public `walk` property is never assigned in the game code, so the private `walking` field is read through the same reflection field as `IsSprinting`.
  - `IsGrounded`: the same value as the `IsGrounded` property.
  - `ControlsDisabled`: `OpenSewerCharacterController.ControlsDisabled()`, true while any disable is held, including a `Lock` from this API.
  - Sprinting is not in `State()`, because the controller does not expose it. Use the `IsSprinting` property.

## Multipliers

- Owners: `SetSpeedMultiplier(owner, m)`, `SetWalkMultiplier(owner, m)`, `SetRunMultiplier(owner, m)` and `SetJumpMultiplier(owner, m)` keep one value per owner and slot. The applied value of a slot is the product of every owner's value, clamped to 0.25 to 4, so two mods that each set 2 give 4, and one mod's change never replaces another's. `ClearMultipliers(owner)` removes that owner from all four slots and returns `false` when it had none. The setters return `false` in the same cases as `SetSpeedMultiplier`. `owner` must be a valid mod id.
- `WalkSpeedMultiplier`, `JumpMultiplier` and `SetSpeedMultiplier(m)` with no owner use one shared entry per slot, which multiplies with the owners. The getters return that shared entry, not the product.
- `WalkSpeedMultiplier` scales walk speed only.
- `SetRunMultiplier(owner, m)` scales run speed only. The game uses the run speed for run and for sprint: `GetTargetSpeed` picks it when the Walk toggle is off or the input is a controller, and not while crouched. There is no separate sprint value, so this covers both. It multiplies with `SetSpeedMultiplier`, so run is `base * run product * speed product`. Walk, swimming, crouch, backwards, strafe and jump are not changed. Its owner rules, clamp and `false` returns are those of `SetWalkMultiplier`. The game's `Animate` divides by `runSpeed`, so raising run alone damps the walk animation and the hand bob (`FirstPersonHands.UpdateHands`).
- `SetSpeedMultiplier(multiplier)` scales all six speeds from their base values: walk, run, swimming, crouch, backwards and strafe. Run is in that list, so run and sprint are already covered. Backwards and strafe are written, but `OpenSewerCharacterController` never reads them, so they have no movement effect. `SetRunMultiplier` adds a run-only factor on top. Walk speed is `base * WalkSpeedMultiplier * speed multiplier`, so the two multipliers combine. It returns `false`, and changes nothing, for a `NaN` or infinite value, when no player is loaded, or when the hooks are disabled. It works inside `Events.SceneLoaded` and while a save loads or saves. Otherwise it returns `true`. Unlike `WalkSpeedMultiplier`, which clamps an infinite value, `SetSpeedMultiplier` returns `false` for Infinity.
- `SetSpeedMultiplier` is clamped to 0.25 to 4 like the other multipliers, and its default is 1.
- `SetSpeedMultiplier` reads the run and swim base from the private `_runSpeed` and `_swimmingSpeed` fields, because their getters add the mushroom modifier. The other bases come from the public properties.
- The game reads these speeds each physics step (`OpenSewerCharacterController.GetTargetSpeed`, `CalcDesiredVelocity`), and the game never writes them outside the setters and `OnValidate`, so a write takes effect at once and nothing resets it.
- `JumpMultiplier` scales the base jump height. Apex height scales linearly with the multiplier, so 2 gives about twice the jump height.
- Both are clamped to 0.25 to 4 on set. The default is 1. The getter returns the clamped value, even when no player is loaded.
- The value is applied to the live controller at once. A controller created later (a new scene or a loaded save) gets it from the `Awake` postfix.
- Never compounds. The first time the multiplier is applied to a controller, the base value is read and kept. Every later write is `base * multiplier`, so setting the same value twice does not stack.
- Setting the multiplier back to 1 restores the base value.
- Not saved. The controller is not a `SavableScript`, so nothing is written to the save file.
- Scene change: the base value is written back to the old controller, if it still exists. A Single scene load then drops every owner entry, so a forgotten owner value does not stick for the session, and the current product is applied to the new controller. The no-owner entries (`WalkSpeedMultiplier`, `JumpMultiplier`, `SetSpeedMultiplier(m)`) are kept, so a value a mod sets once at startup still holds across scene changes and loads. It is lost when the game restarts. An owner sets its value again from its own `Events.SceneLoaded` or `Events.Loaded` handler.

## Lock

- `Lock(modId)`: takes one lock for `modId`. Locks are counted per mod id, so two `Lock` calls from one mod need two `Unlock` calls. The player is held when the first lock across all mods is taken.
- `Unlock(modId)`: releases the newest lock that `modId` took, a `Lock` or a `LockLookAt`. Returns `false` if that mod holds no lock.
- The hold is the game's own movement disable. It calls `GameController.ControlsDisabled` with one holder GameObject, which is what `DisablePlayerMovement.DisableMovement(false)` does. It also disables the player's keys and the camera, the same as the game does for a cutscene, and it does not show the cursor. Other game disables are separate entries and are not affected.
- `Lock` returns `false` and takes nothing when the game controller, the player controller or the camera is not loaded.
- `Lock` returns `false` while a save is loading or saving. `Unlock` always runs, also during a save, so a release is never refused.
- Locks are released on `Events.Loaded` (save load). They are also released on `Events.SceneLoaded`, but only when the game controller that took the hold is gone. Normal play loads neighbouring scenes additively, and those keep the locks. After a save load, or a scene change that replaces the controller, a mod must lock again. Its later `Unlock` returns `false`.
- `Unlock` keeps the camera look-locks (cutscenes and build mode) that were set before `Lock`.
- `LockLookAt(modId, target, instantly)`: the same hold as `Lock`, with the camera turned toward `target`. It is the game's look-at movement lock, and with `instantly` true it snaps the camera at once. It has the same counting, release and save rules as `Lock`. It returns `false`, and takes nothing, when `target` is `null` or when the camera's private `firstpersonSoftLock` or `lookAtTarget` field is missing.
- The turn applies only in first person. The game turns the camera only while the player is in first person, so in third person the hold applies but the camera does not turn, and an `instantly` snap is undone on the next frame. The turn starts when the player switches to first person while the lock is held.
- One target is active at a time: the newest live look target across all mods. A later `LockLookAt` from any mod replaces it.
- When `Unlock` releases a look lock, the camera turns toward the newest remaining look target of any mod. Destroyed targets are skipped. When no look lock remains, the camera's soft-lock flag and target go back to what they were before the first look lock, even while plain `Lock` holds remain.
- The game wins. The framework changes the camera look only while it still owns it: the soft-lock flag is set and the target is the one the framework last applied. If the game sets its own soft lock during the hold (a dialogue, the card game, lockpicking) or clears the soft lock, the framework leaves the camera alone. It does not turn back to a mod target and does not restore the earlier state. A later `LockLookAt` saves the game's lock as the state to restore.
- The release never clears a soft lock that the game set. The game's own `EnableMovementLookAtTarget` is not called. A destroyed target stops the turn without an error.

## Look lock

- `LockLook(modId)`: stops the player's mouse look and leaves movement free. It sets the game's own rotation flag through `ThirdPersonCameraController.DisableRotation`. Counted per mod id: two `LockLook` calls need two `UnlockLook` calls. Returns `false` and takes nothing when the camera is not loaded, while a save is loading or saving, or when the rotation hooks did not install. A bad mod id throws `ArgumentException`.
- `UnlockLook(modId)`: releases the newest look lock that `modId` took. Returns `false` if that mod holds none.
- Postfixes on `DisableRotation` and `EnableRotation` track whether the game wants rotation off. The framework's own calls are ignored. Rotation is off while the game wants it off or any mod holds a look lock. So a game `EnableRotation` (chair, computer, ladder, cards) never ends a mod's lock, and `UnlockLook` never re-enables rotation the game disabled.
- A Single scene load clears every look lock and resets the game's tracked state, because the camera may be replaced. Player death (`Survival.PlayerDied`) clears every look lock. There is no main-menu event; the main menu is loaded as a Single scene. A mod takes its lock again from its own handler.

## Crouch

- `Crouch()` calls the game's `OpenSewerCharacterController.Crouch()`. It returns `true` when the player is crouched afterwards. When the player is already crouched it does nothing and returns `true`. It does not cancel a stand that the player's own input left pending.
- `StandUp()` calls the game's `StandUp()`. The game keeps retrying the stand each frame while a ceiling is over the player. When the player is still crouched straight after the attempt, the facade calls the game's `Crouch()` to cancel that retry, so no stand stays pending, and returns `false`. The player stays crouched. It returns `true` when the player stands or is already standing.
- The ceiling test is the game's own: a sphere cast in the crouched branch of `ScaleCapsuleForCrouching`. The facade does not repeat it. A jump or jump cooldown also keeps the player crouched.
- Both return `false` when no player is loaded, when the controller is inactive, when `IsAvailable` is `false`, or while a save loads or saves.
- NoClip is not wrapped. It is a developer console command (`DeveloperConsoleRoutines.NoClip`, `DeveloperConsoleRoutines`), so it is left out.

## Interaction with Tweaks and Effects

- The framework's Tweaks do not change locomotion. The Tweaks cover needs, time, crafting, stacks, camera and renovations. Nothing else in the framework writes `walkSpeed`, `runSpeed` or `baseJumpHeight`.
- `Effect.Mushroom`, read with `Effects.IsActive`, reports the mushroom state only and does not change speed.
- The game's own orange mushroom effect adds a run-speed modifier inside the `runSpeed` getter (`OrangeMushroomEffect.GetRunSpeedModifier`). `WalkSpeedMultiplier` does not touch `runSpeed`, so the two do not combine. Sprint speed is only the game's value plus the mushroom modifier.
- `JumpMultiplier` also changes the water ledge exit jump, because that path uses the same jump impulse.
