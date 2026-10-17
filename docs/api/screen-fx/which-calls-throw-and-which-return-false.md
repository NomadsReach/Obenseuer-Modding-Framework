---
title: Which calls throw and which return false
parent: Screen effects
grand_parent: Screen and UI
nav_order: 2
---

# Which calls throw and which return false

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public static class ScreenFx
{
    public static bool IsAvailable { get; }    // false when the field of view override could not be set up
}
```

- **Every call returns `true` when it did its job and `false` when it could not.** It returns `false` when the game part it needs is not there yet, for example on the main menu or while a level is loading.
  - Technical: the missing parts are `CameraShake.instance`, `BlackCanvas.instance`, `BlackCanvas.fadeCanvasGroup` and `DeathMessage.instance`.
- **`Shake`, `FadeToBlack` and `SetDeathMessage` also return `false` while a save is loading or being written.** `FadeIn` always runs, so the screen can clear during a save.
- **A bad value throws an `ArgumentException`.** `duration` must be above 0, `amount` must not be negative, `speed` must be above 0, and `text` must not be `null`. Numbers must be real numbers. An `owner` must be a valid mod id, 1 to 64 letters, digits, dots, underscores or dashes (`Mod.Id` always is). Nothing else is thrown to you.
- **If the game itself fails, you get `false` and one line in the log.** The call does not throw.
