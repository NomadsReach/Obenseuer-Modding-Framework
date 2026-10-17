---
title: Crouching
parent: Player movement
grand_parent: Player life and skills
nav_order: 6
---

# Crouching

These two calls make the player crouch and stand up.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerMovement</code> <span>crouch and stand up</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class PlayerMovement
{
    public static bool Crouch();
    public static bool StandUp();
}
```

| Method | Returns | What it does |
|---|---|---|
| `Crouch()` | `bool` | Makes the player crouch. `true` when the player is crouched afterwards. |
| `StandUp()` | `bool` | Makes the player stand up. `true` when the player stands or already stood. |

</div>
</div>

## Things to know

- `Crouch()` does nothing when the player is already crouched, and returns `true`. It does not cancel a stand that the player's own key press left waiting. <span class="tech">(it calls the game's `OpenSewerCharacterController.Crouch()`.)</span>
- `StandUp()` makes the player stand up, if there is room. No stand stays waiting. <span class="tech">(it calls the game's `StandUp()`. The game keeps retrying the stand each frame while a ceiling is over the player. When the player is still crouched straight after the attempt, the framework calls the game's `Crouch()` to cancel that retry.)</span>
- The room check is the game's own, and the framework does not repeat it. A jump, or the short wait after a jump, also keeps the player crouched. <span class="tech">(the test is a sphere cast in the crouched branch of `ScaleCapsuleForCrouching`.)</span>
- No-clip is not here. It is a developer console command in the game, so it is left out. <span class="tech">(`DeveloperConsoleRoutines.NoClip`.)</span>

## Limits

- If something is over the player's head, the player stays crouched and `StandUp()` returns `false`.
- Both return `false` when no player is loaded. They also return `false` when the controller is switched off, when `IsAvailable` is `false`, and while a save is loading or being written.
