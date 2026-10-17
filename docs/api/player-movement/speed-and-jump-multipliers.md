---
title: Speed and jump multipliers
parent: Player movement
grand_parent: Player life and skills
nav_order: 3
---

# Speed and jump multipliers

A multiplier makes a speed bigger or smaller. A value of `2` doubles it, and `0.5` halves it. The setters keep a separate value for each mod id, and the game uses the product of all of them, so two mods that each set `2` give `4`. One mod's change never replaces another's.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerMovement</code> <span>speed and jump multipliers</span></p>

Call `SetSpeedMultiplier`, `SetWalkMultiplier`, `SetRunMultiplier` or `SetJumpMultiplier` to change how fast the player moves or how high they jump, and `ClearMultipliers` to take your mod id's values back.

<div class="api-pair" markdown="1">

```csharp
public static class PlayerMovement
{
    public static float WalkSpeedMultiplier { get; set; }
    public static float JumpMultiplier { get; set; }
    public static bool SetSpeedMultiplier(float multiplier);
    public static bool SetSpeedMultiplier(string owner, float multiplier);
    public static bool SetWalkMultiplier(string owner, float multiplier);
    public static bool SetRunMultiplier(string owner, float multiplier);
    public static bool SetJumpMultiplier(string owner, float multiplier);
    public static bool ClearMultipliers(string owner);
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetSpeedMultiplier(multiplier)` | `bool` | Sets all six speeds, with no mod id. `false` for `NaN` or infinity. |
| `SetSpeedMultiplier(owner, multiplier)` | `bool` | Sets all six speeds for your mod id. |
| `SetWalkMultiplier(owner, multiplier)` | `bool` | Sets walk speed only. |
| `SetRunMultiplier(owner, multiplier)` | `bool` | Sets run and sprint speed only. |
| `SetJumpMultiplier(owner, multiplier)` | `bool` | Sets jump height. |
| `ClearMultipliers(owner)` | `bool` | Takes back everything your mod id set. `false` when it set nothing. |

| Property | Type | What it means |
|---|---|---|
| `WalkSpeedMultiplier` | `float` | Walk speed only. One value shared by code without a mod id. |
| `JumpMultiplier` | `float` | Jump height. One value shared by code without a mod id. |

</div>
</div>

## Things to know

- In the calls that take `owner`, pass your mod id (see [Owner](../../glossary.md#owner) in the glossary). The six speeds are walk, run, swimming, crouch, backwards and strafe.
- The final value is limited to between 0.25 and 4. The framework multiplies every mod's value together and then keeps the result in that range. The default is `1`.
- `SetSpeedMultiplier(owner, m)`, `SetWalkMultiplier`, `SetRunMultiplier` and `SetJumpMultiplier` keep one value for each mod id and each kind. `ClearMultipliers(owner)` removes your mod id from all four kinds.
- `WalkSpeedMultiplier`, `JumpMultiplier` and `SetSpeedMultiplier(m)` without a mod id are shared by all code. They count as one extra entry that multiplies with the mod ids' values. Reading `WalkSpeedMultiplier` or `JumpMultiplier` gives you that shared entry, not the final product.
- `WalkSpeedMultiplier` changes the walk speed only. An infinite value is limited to the maximum.
- `SetRunMultiplier(owner, m)` changes the run speed only, and run speed is also the sprint speed. The game uses the same speed for running and sprinting, so there is no separate sprint value. It multiplies with `SetSpeedMultiplier`: the run speed is the base run speed times the run product times the speed product. Walk, swimming, crouch, backwards, strafe and jump are not changed. Raising the run speed alone makes the walking animation and the hand bob look slower, because the game scales them by the run speed. <span class="tech">(the game picks the run speed in `GetTargetSpeed` when the Walk toggle is off or the input is a controller, and not while crouched. `Animate` divides by `runSpeed`, and so does `FirstPersonHands.UpdateHands`.)</span>
- `SetSpeedMultiplier(multiplier)` changes all six speeds, from their base values. To change only walk, use `WalkSpeedMultiplier` or `SetWalkMultiplier`. To change only run and sprint, use `SetRunMultiplier`. It combines with the others: the walk speed is the base times `WalkSpeedMultiplier` times the speed multiplier. Backwards and strafe are changed too, but the game never reads those two, so they have no effect on movement. <span class="tech">(the game's `OpenSewerCharacterController` never reads `backwardSpeed` and `strafeSpeed`.)</span>
- With no player loaded, `SetSpeedMultiplier(multiplier)` stores the value and returns `true`, and the value applies when the player's controller is created. It works inside `Events.SceneLoaded` and while a save loads or is being written.
- `JumpMultiplier` changes the jump height. The apex of the jump scales in line with the value, so `2` gives about twice the jump height. It also changes the jump out of the water at a ledge, because that jump uses the same push.
- A change takes effect at once and does not pile up. The first time the framework changes the controller, it remembers the base value. Every later write is the base times the multiplier, so setting the same value twice does not make it bigger. Setting the multiplier back to `1` restores the base value. <span class="tech">(the game reads these speeds each physics step (`OpenSewerCharacterController.GetTargetSpeed`, `CalcDesiredVelocity`), and it never writes them outside the setters and `OnValidate`, so nothing resets them.)</span>
- The base values cannot be raised by another mod's patch. The framework reads the base speeds from the controller's private fields, so a change that another mod makes to a speed getter does not change the base. Run and swimming need this, because their getters add the mushroom boost. If a private field is missing, the getter is used for that speed. The jump base is read from the private `_baseJumpHeight` field, with the `baseJumpHeight` property as the fallback. <span class="tech">(the fields are `_walkSpeed`, `_runSpeed`, `_swimmingSpeed`, `_crouchSpeed`, `_backwardSpeed` and `_strafeSpeed`.)</span>
- Multipliers are not saved. Nothing is written to the save file, so set yours again each time the game starts. <span class="tech">(the controller is not a `SavableScript`.)</span>
- A new scene or a loaded save gets your values on its own. A controller created later gets them when it starts. A value set while no player exists is stored and applied then. Every kind is applied: walk, run, speed (swimming, crouch, backwards, strafe) and jump. The getter returns the limited value, even when no player is loaded.
- The shared entries (no mod id) are never dropped on a scene change. `WalkSpeedMultiplier`, `JumpMultiplier` and `SetSpeedMultiplier(m)` hold across scene changes and loads. A value a mod sets once at startup stays until the game restarts.

## Limits

- The setters return `false` for a value that is not a real number. That means `NaN` or infinity, or when the speed hooks are not available. `owner` must be a valid mod id, or you get an `ArgumentException`.
- Otherwise the setters store the value and return `true`, even when no player is loaded.
- Setting `WalkSpeedMultiplier` to a value that is not a number throws an `ArgumentException`.
- `SetSpeedMultiplier(multiplier)` returns `false` and changes nothing for `NaN` or infinity. Unlike `WalkSpeedMultiplier`, which limits an infinite value, it returns `false` for infinity.
- `ClearMultipliers` returns `false` when your mod id had nothing to remove.
- When the scene changes, values from a mod id are dropped unless you set them again. The base value is written back to the old controller, if it still exists, and the current product is applied to the new one. Each single-scene load drops every mod id's value, so a value you forgot to take back does not stay for the whole session. Set yours again from your `Events.SceneLoaded` handler, which runs after the reset. <span class="tech">(the exception is a value stored while no player exists. That mod id's entry is kept through each single-scene load up to and including the first one after which a player controller exists. Every other mod id's entry is dropped by that same load, so one mod storing a value with no player never keeps another mod's value alive. After that, the next single-scene load drops entries again.)</span>
