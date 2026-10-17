---
title: Shake
parent: Screen effects
grand_parent: Screen and UI
nav_order: 3
---

# Shake

```csharp
public static bool Shake(float duration, float amount);   // shake the camera; true when it started
```

- **`Shake` shakes the camera for `duration` seconds.** `amount` is the most the camera moves each frame, in world units.
  - Technical: it calls the game's static `CameraShake.Shake`.
- **A new shake replaces the one that is running.** That includes the game's own shakes, such as when the player is hit, and the other way round.
  - Technical: the game's `Shake` stops its own coroutine first. Before the new shake, the camera goes back to the base position from before the running shake, so an interrupted shake leaves no offset.
- **The shake stops while the game is paused.**
  - Technical: it waits while `PauseMenu.paused` is set.
