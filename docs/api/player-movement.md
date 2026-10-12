# Player movement API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.playermovement`.
Files: `src/Api/PlayerMovement.cs`, `src/Api/PlayerMovementPatches.cs`.

Everything not listed here is internal.

## Wiring

```csharp
internal static void PlayerMovement.Init();
```

Core calls this from `Core.Awake`.

`Init` installs one postfix on `OpenSewerCharacterController.Awake` and subscribes to `Events.SceneLoaded` and `Events.Loaded`. Without `Init`, multipliers never reach a new controller and `Lock` is not released on scene change or load, so `Init` is required. If the `Awake` hook or the `walking` field is missing, the owner is removed, `PlayerMovement.IsAvailable` becomes `false`, and one warning is logged. Reads, `Lock` and `Unlock` keep working, and the multipliers stop applying.

## Threading and safety

- Main thread only.
- Every member is safe at any time. On the main menu, or with no player, reads return `false`, `Vector3.zero` or the stored multiplier, and `Lock` returns `false`.
- `Lock` and `Unlock` return `bool`. Nothing throws except `ArgumentException` for an invalid mod id (`ModIds.Require`) or a `NaN` multiplier.

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

    public static bool Lock(string modId);
    public static bool Unlock(string modId);
}
```

## Reads

- `IsGrounded`: `OpenSewerCharacterController.movement.isGrounded` (through `BaseCharacterController.isGrounded`).
- `IsCrouching`: `OpenSewerCharacterController.isCrouching`.
- `IsSprinting`: true when run speed is the selected speed: not crouching, and either the Walk toggle is in run mode or the input is a controller. The game reads the same condition in `GetTargetSpeed`. Swimming is not checked, and no movement check is made, so a stopped player who has run mode selected reads `true`.
- `Velocity`: `movement.velocity`, the character's current velocity in world space.
- `Position`: the controller's transform position (the player body).

## Multipliers

- `WalkSpeedMultiplier` scales walk speed only. Sprint speed, swim speed, crouch speed and backwards and strafe speeds are not changed.
- `JumpMultiplier` scales the base jump height. Apex height scales linearly with the multiplier, so 2 gives about twice the jump height.
- Both are clamped to 0.25 to 4 on set. The default is 1. The getter returns the clamped value, even when no player is loaded.
- The value is applied to the live controller at once. A controller created later (a new scene or a loaded save) gets it from the `Awake` postfix.
- Never compounds. The first time the multiplier is applied to a controller, the base value is read and kept. Every later write is `base * multiplier`, so setting the same value twice does not stack.
- Setting the multiplier back to 1 restores the base value.
- Not saved. The controller is not a `SavableScript`, so nothing is written to the save file.
- Scene change: the base value is written back to the old controller, if it still exists, and the current multiplier is then applied to the new controller. The multiplier value itself is kept, so a mod that sets 2 keeps 2 across scene changes and loads. It is lost when the game restarts.

## Lock

- `Lock(modId)`: takes one lock for `modId`. Locks are reference-counted per mod id, so two `Lock` calls from one mod need two `Unlock` calls. The player is held when the first lock across all mods is taken.
- `Unlock(modId)`: releases one lock for `modId`. Returns `false` if that mod holds no lock.
- The hold is the game's own movement disable. It calls `GameController.ControlsDisabled` with one holder GameObject, which is what `DisablePlayerMovement.DisableMovement(false)` does. It also disables the player's keys and the camera, the same as the game does for a cutscene, and it does not show the cursor. Other game disables are separate entries and are not affected.
- `Lock` returns `false` and takes nothing when the game controller, the player controller or the camera is not loaded.
- `Lock` returns `false` while a save is loading (`SaveController.Loading`).
- Locks are released on `Events.Loaded` (save load). They are also released on `Events.SceneLoaded`, but only when the game controller that took the hold is gone. Normal play loads neighbouring scenes additively, and those keep the locks. After a save load, or a scene change that replaces the controller, a mod must lock again. Its later `Unlock` returns `false`.
- `Unlock` keeps the camera look-locks (cutscenes and build mode) that were set before `Lock`.

## Interaction with Tweaks and Effects

- Core's Tweaks do not change locomotion. `src/Tweaks*.cs` covers needs, time, crafting, stacks, camera and renovations. Nothing else in `src/` writes `walkSpeed`, `runSpeed` or `baseJumpHeight`.
- `Effects.Mushroom` reports the mushroom state only and does not change speed.
- The game's own orange mushroom effect adds a run-speed modifier inside the `runSpeed` getter (`OrangeMushroomEffect.GetRunSpeedModifier`). `WalkSpeedMultiplier` does not touch `runSpeed`, so the two do not combine. Sprint speed is only the game's value plus the mushroom modifier.
- `JumpMultiplier` also changes the water ledge exit jump, because that path uses the same jump impulse.

## Smoke test (in game, later)

1. Load a save and stand still. `IsGrounded` is `true`. `Position` changes as you walk.
2. Set `PlayerMovement.WalkSpeedMultiplier = 2f`. Walking 5 metres takes about half the time. Set it to `1f`. Walking speed returns to normal.
3. Set `JumpMultiplier = 2f`. The jump is about twice as high. Set `1f`.
4. Call `Lock("smoke.test")`. The player cannot walk or jump, and the camera and keys are disabled. Call `Unlock("smoke.test")`. Control returns.
5. Call `Lock("smoke.test")`, then load a save from the pause menu. Control must work at once after the load. If it does not, release the keys and report it.
6. Check the BepInEx log for `controller hooks incomplete` and for `PlayerMovement` errors.

Not verified in game. Lock's key and camera disable, and whether the scene-change release leaves `keysDisabled` clear, are the first things to check.
