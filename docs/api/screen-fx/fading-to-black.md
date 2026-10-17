---
title: Fading to black
parent: Screen effects
grand_parent: Screen and UI
nav_order: 4
---

# Fading to black

These calls fade the screen to black and back. Each call comes in a version with no owner and a version with an owner for your mod.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ScreenFx</code> <span>fade the screen to black and back</span></p>

`ScreenFx.FadeToBlack` fades the screen to black and `FadeIn` fades it back. Use the versions with an `owner` so your mod's hold is counted and cannot lift another mod's black screen.

<div class="api-pair" markdown="1">

```csharp
public static bool FadeToBlack(float speed = 0.1f);
public static bool FadeIn(float speed = 0.1f);
public static bool FadeToBlack(string owner, float speed);
public static bool FadeIn(string owner, float speed);
```

| Method | Returns | What it does |
|---|---|---|
| `FadeToBlack(speed)` | `bool` | Fades the screen to black, with no owner. |
| `FadeIn(speed)` | `bool` | Fades back in, with no owner. |
| `FadeToBlack(owner, speed)` | `bool` | Fades the screen to black, as your mod. |
| `FadeIn(owner, speed)` | `bool` | Lets go of your fade. It fades in when no mod still holds the screen black. |

</div>
</div>

## Things to know

- `FadeToBlack` fades the screen to full black and keeps it black until a fade in. It does not change the player's controls. `FadeIn` fades the screen back. <span class="tech">(`FadeToBlack` calls the game's `BlackCanvas.FadeIn` and `FadeIn` calls `fadeCanvasGroup.StartFadeOut(speed)` on the same canvas. The fade's alpha is on the `CanvasGroup` in `BlackCanvas.fadeCanvasGroup` (its private `canvasGroup` field). `BlackCanvas.canvasGroup` is the instant overlay, so do not read it to find out whether the screen is black.)</span>
- `speed` is how big each fade step is. The game's normal fade uses `0.1`, which is the default here. The game uses `0.5` for the tenement fades. A smaller number is slower. <span class="tech">(Steps are paced by `WaitForSecondsRealtime`, and the canvas's `realTime` flag changes the step size.)</span>
- The framework does not use the game's own `BlackCanvas.FadeOut`. That call gives the player's controls back, and it would free the camera that a cutscene or a conversation had locked. The framework fades the same way without that call. Do not call `BlackCanvas.ShowBlackCanvas` either, because it switches off movement and keys. <span class="tech">(`ControlsEnabled` removes only its own holder and resets the two camera look flags unconditionally (`ControlsEnabledMove`, which calls `ThirdPersonCameraController.EnableMovement` and `EnableFirstPersonMovement`).)</span>
- With an `owner`, each `FadeToBlack(owner, ...)` adds one hold for your mod, and each `FadeIn(owner, ...)` takes one away. So mod B's `FadeIn` does not lift mod A's black screen.
- `FadeIn(owner, ...)` returns `false` if your mod has no hold, and `true` when it took one away, even if another mod keeps the screen black.
- Without an `owner`, the fade is shared and not counted. `FadeIn(speed)` lets go of it and fades in only when no mod holds the screen, otherwise it returns `false`. If no mod holds the screen at all, it fades in as before, so it can still lift a black screen the game itself is holding.
- A normal level load drops every hold. It does not fade in, so a game fade is not cut off. Nothing is saved, so fade back in yourself. <span class="tech">(Every Single scene load drops every count.)</span>

## Limits

- A new fade stops the fade that is running. The game uses the same canvas for its own blackouts, treatments, conversations and tenement fades. So your `FadeIn` can lift a black screen the game is holding, and your `FadeToBlack` can cut off a game fade in progress. <span class="tech">(This is `FadeCanvasGroup`. The game's users of the canvas include `BlackoutTrigger`.)</span>
