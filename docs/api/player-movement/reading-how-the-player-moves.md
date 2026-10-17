---
title: Reading how the player moves
parent: Player movement
grand_parent: Player life and skills
nav_order: 2
---

# Reading how the player moves

This page uses `Vector3`, `Transform` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

This is the full list of what the API gives you, one section for each part. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

These properties give you what the player is doing right now. `State()` gives several of them together in one `PlayerMovementInfo`, a snapshot of the speeds and flags at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerMovement</code> <span>what the player is doing right now</span></p>

Read these properties to see what the player is doing now, or call `State()` to get several of them at once.

<div class="api-pair" markdown="1">

```csharp
public static class PlayerMovement
{
    public static bool IsAvailable { get; }
    public static bool IsGrounded { get; }
    public static bool IsSprinting { get; }
    public static bool IsCrouching { get; }
    public static Vector3 Velocity { get; }
    public static Vector3 Position { get; }
    public static PlayerMovementInfo State();
}
```

| Method | Returns | What it does |
|---|---|---|
| `State()` | `PlayerMovementInfo` | Several of the properties below in one snapshot. `null` when no player is loaded. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the speed hooks did not set up. |
| `IsGrounded` | `bool` | `true` while the player stands on the ground. |
| `IsSprinting` | `bool` | `true` when the player is set to run (see below). |
| `IsCrouching` | `bool` | `true` while the player is crouched. |
| `Velocity` | `Vector3` | How fast and in which direction the player is moving, in world space. |
| `Position` | `Vector3` | Where the player's body is, in world coordinates. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerMovementInfo</code> <span>a snapshot of the player's movement</span></p>

`PlayerMovement.State()` gives you one of these, with the speeds and states at that moment. It is `null` when no player is loaded. <span class="tech">(The speeds and flags come from the game's `OpenSewerCharacterController`.)</span>

<div class="api-pair" markdown="1">

```csharp
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

| Property | Type | What it means |
|---|---|---|
| `WalkSpeed` | `float` | How fast the player walks. |
| `RunSpeed` | `float` | How fast the player runs. |
| `SwimmingSpeed` | `float` | How fast the player swims. |
| `CrouchSpeed` | `float` | How fast the player moves while crouching. |
| `BackwardSpeed` | `float` | The game's backward speed. It has no effect on movement. |
| `StrafeSpeed` | `float` | The game's sideways speed. It has no effect on movement. |
| `IsCrouching` | `bool` | `true` while the player is crouched. |
| `IsWalking` | `bool` | `true` when the walk toggle is on (walk mode instead of run mode). |
| `IsGrounded` | `bool` | `true` while the player stands on the ground. |
| `ControlsDisabled` | `bool` | `true` while the player's controls are held, including a `Lock` from this API. |

</div>
</div>

## Things to know

- On the main menu, or with no player, the reads give plain defaults. `IsGrounded`, `IsCrouching` and `IsSprinting` are `false`, and `Velocity` and `Position` are `Vector3.zero`.
- `IsSprinting` is `true` when the player is set to run, not when they are actually moving. A player who is standing still with run mode selected reads `true`. It is `false` while crouching. Swimming is not checked. <span class="tech">(it is `true` when run speed is the selected speed: not crouching, and either the Walk toggle is in run mode or the input is a controller. The game reads the same condition in `GetTargetSpeed`. No movement check is made.)</span>
- `IsGrounded` and `IsCrouching` come straight from the game's player controller. <span class="tech">(`OpenSewerCharacterController.movement.isGrounded` (through `BaseCharacterController.isGrounded`) and `OpenSewerCharacterController.isCrouching`.)</span>
- `Velocity` is the player's current velocity, and `Position` is the position of the player's body. <span class="tech">(`movement.velocity` and the controller's transform position.)</span>
- `WalkSpeed`, `CrouchSpeed`, `BackwardSpeed` and `StrafeSpeed` are the controller's current values.
- `RunSpeed` and `SwimmingSpeed` include the orange mushroom's speed boost. When the orange mushroom effect is not active, they are the unmodified base speeds. <span class="tech">(they are the controller's getter values, which include the game's orange mushroom run modifier.)</span>
- `IsWalking` is `true` in walk mode and `false` in run mode. <span class="tech">(the controller's public `walk` property is never assigned in the game code, so the private `walking` field is read through the same reflection field as `IsSprinting`.)</span>
- `ControlsDisabled` is `true` while anything holds the player's controls. That includes a `Lock` from this API. <span class="tech">(it is `OpenSewerCharacterController.ControlsDisabled()`.)</span>
- Sprinting is not in `State()`. The controller does not expose it, so use the `IsSprinting` property.

## Limits

- `IsAvailable` tells you whether the speed changes work. It is `false` when the framework could not set up its speed hooks. The multipliers then stop applying. Reads, `Lock` and `Unlock` keep working.
