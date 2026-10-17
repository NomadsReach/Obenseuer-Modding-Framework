---
title: Which calls throw and which return false
parent: Screen effects
grand_parent: Screen and UI
nav_order: 2
---

# Which calls throw and which return false

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ScreenFx</code> <span>whether the field of view override works</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class ScreenFx
{
    public static bool IsAvailable { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the field of view override could not be set up. |

</div>
</div>

## Things to know

- Every call returns `true` when it did its job and `false` when it could not. It returns `false` when the game part it needs is not there yet, for example on the main menu or while a level is loading. <span class="tech">(The missing parts are `CameraShake.instance`, `BlackCanvas.instance`, `BlackCanvas.fadeCanvasGroup` and `DeathMessage.instance`.)</span>
- `Shake`, `FadeToBlack` and `SetDeathMessage` also return `false` while a save is loading or being written. `FadeIn` always runs, so the screen can clear during a save.

## Limits

- A bad value throws an `ArgumentException`. `duration` must be above 0, `amount` must not be negative, `speed` must be above 0, and `text` must not be `null`. Numbers must be real numbers. An `owner` must be a valid mod id, 1 to 64 letters, digits, dots, underscores or dashes (`Mod.Id` always is). Nothing else is thrown to you.
- If the game itself fails, you get `false` and one line in the log. The call does not throw.
