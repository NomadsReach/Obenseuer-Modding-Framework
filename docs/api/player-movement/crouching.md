---
title: Crouching
parent: Player movement
grand_parent: Player life and skills
nav_order: 6
---

# Crouching

```csharp
public static class PlayerMovement
{
    public static bool Crouch();     // makes the player crouch; true when they are crouched afterwards
    public static bool StandUp();    // makes the player stand up; true when they stand or already stood
}
```

- **`Crouch()` makes the player crouch.** It returns `true` when the player is crouched afterwards. When the player is already crouched it does nothing and returns `true`. It does not cancel a stand that the player's own key press left waiting.
  - Technical: it calls the game's `OpenSewerCharacterController.Crouch()`.
- **`StandUp()` makes the player stand up, if there is room.** It returns `true` when the player stands or already stood. If something is over the player's head, the player stays crouched and `StandUp()` returns `false`. No stand stays waiting.
  - Technical: it calls the game's `StandUp()`. The game keeps retrying the stand each frame while a ceiling is over the player. When the player is still crouched straight after the attempt, the framework calls the game's `Crouch()` to cancel that retry.
- **The room check is the game's own.** The framework does not repeat it. A jump, or the short wait after a jump, also keeps the player crouched.
  - Technical: the test is a sphere cast in the crouched branch of `ScaleCapsuleForCrouching`.
- **Both return `false` when no player is loaded.** They also return `false` when the controller is switched off, when `IsAvailable` is `false`, and while a save is loading or being written.
- **No-clip is not here.** It is a developer console command in the game, so it is left out.
  - Technical: `DeveloperConsoleRoutines.NoClip`.
