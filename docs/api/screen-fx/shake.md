---
title: Shake
parent: Screen effects
grand_parent: Screen and UI
nav_order: 3
---

# Shake

`Shake` moves the camera for a set time.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ScreenFx</code> <span>shake the camera</span></p>

`ScreenFx.Shake` moves the camera for a set time. `amount` is the most the camera moves each frame.

<div class="api-pair" markdown="1">

```csharp
public static bool Shake(float duration, float amount);
```

| Method | Returns | What it does |
|---|---|---|
| `Shake(duration, amount)` | `bool` | Shakes the camera for `duration` seconds. `amount` is the most the camera moves each frame, in world units. Returns `true` when the shake started. |

</div>
</div>

## Things to know

- A new shake replaces the one that is running. That includes the game's own shakes, such as when the player is hit, and the other way round. <span class="tech">(The game's `Shake` stops its own coroutine first. Before the new shake, the camera goes back to the base position from before the running shake, so an interrupted shake leaves no offset. `Shake` calls the game's static `CameraShake.Shake`.)</span>

## Limits

- The shake stops while the game is paused. <span class="tech">(It waits while `PauseMenu.paused` is set.)</span>
