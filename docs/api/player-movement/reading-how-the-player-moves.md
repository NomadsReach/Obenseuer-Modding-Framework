---
title: Reading how the player moves
parent: Player movement
grand_parent: Player life and skills
nav_order: 2
---

# Reading how the player moves

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

These properties give you what the player is doing right now. `State()` gives several of them together in one `PlayerMovementInfo`, a snapshot of the speeds and flags at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public sealed class PlayerMovementInfo    // a snapshot of the player's movement
{
    public float WalkSpeed { get; }       // how fast the player walks
    public float RunSpeed { get; }        // how fast the player runs
    public float SwimmingSpeed { get; }   // how fast the player swims
    public float CrouchSpeed { get; }     // how fast the player moves while crouching
    public float BackwardSpeed { get; }   // the game's backward speed (has no effect on movement)
    public float StrafeSpeed { get; }     // the game's sideways speed (has no effect on movement)
    public bool IsCrouching { get; }      // true while the player is crouched
    public bool IsWalking { get; }        // true when the walk toggle is on (walk mode instead of run mode)
    public bool IsGrounded { get; }       // true while the player stands on the ground
    public bool ControlsDisabled { get; } // true while the player's controls are held, including a Lock from this API
}

public static class PlayerMovement
{
    public static bool IsAvailable { get; }    // false when the speed hooks did not set up

    public static bool IsGrounded { get; }     // true while the player stands on the ground
    public static bool IsSprinting { get; }    // true when the player is set to run (see below)
    public static bool IsCrouching { get; }    // true while the player is crouched
    public static Vector3 Velocity { get; }    // how fast and in which direction the player is moving, in world space
    public static Vector3 Position { get; }    // where the player's body is, in world coordinates

    public static PlayerMovementInfo State();  // several of the above in one snapshot; null when no player is loaded
}
```

- **On the main menu, or with no player, the reads give plain defaults.** `IsGrounded`, `IsCrouching` and `IsSprinting` are `false`, and `Velocity` and `Position` are `Vector3.zero`.
- **`IsSprinting` is `true` when the player is set to run, not when they are actually moving.** A player who is standing still with run mode selected reads `true`. It is `false` while crouching. Swimming is not checked.
  - Technical: it is `true` when run speed is the selected speed: not crouching, and either the Walk toggle is in run mode or the input is a controller. The game reads the same condition in `GetTargetSpeed`. No movement check is made.
- **`IsGrounded` and `IsCrouching` come straight from the game's player controller.**
  - Technical: `OpenSewerCharacterController.movement.isGrounded` (through `BaseCharacterController.isGrounded`) and `OpenSewerCharacterController.isCrouching`.
- **`Velocity` is the player's current velocity, and `Position` is the position of the player's body.**
  - Technical: `movement.velocity` and the controller's transform position.
- **`State()` returns `null` when no player is loaded.**
- **`WalkSpeed`, `CrouchSpeed`, `BackwardSpeed` and `StrafeSpeed` are the controller's current values.**
- **`RunSpeed` and `SwimmingSpeed` include the orange mushroom's speed boost.** When the orange mushroom effect is not active, they are the unmodified base speeds.
  - Technical: they are the controller's getter values, which include the game's orange mushroom run modifier.
- **`IsWalking` is `true` in walk mode and `false` in run mode.**
  - Technical: the controller's public `walk` property is never assigned in the game code, so the private `walking` field is read through the same reflection field as `IsSprinting`.
- **`ControlsDisabled` is `true` while anything holds the player's controls.** That includes a `Lock` from this API.
  - Technical: it is `OpenSewerCharacterController.ControlsDisabled()`.
- **Sprinting is not in `State()`.** The controller does not expose it, so use the `IsSprinting` property.
- **`IsAvailable` tells you whether the speed changes work.** It is `false` when the framework could not set up its speed hooks. The multipliers then stop applying. Reads, `Lock` and `Unlock` keep working.
