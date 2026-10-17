---
title: Common tasks
parent: Screen effects
grand_parent: Screen and UI
nav_order: 1
---

# Common tasks

Shake the camera for half a second. The second number is how far the camera moves each frame, in world units.

```csharp
ScreenFx.Shake(0.5f, 0.05f);
```

Fade the screen to black, and fade it back in later.

```csharp
ScreenFx.FadeToBlack(Mod.Id, 0.1f);
ScreenFx.FadeIn(Mod.Id, 0.1f);
```

Tint the edges of the screen red, and clear it later.

```csharp
ScreenFx.Vignette(Mod.Id, new Color(0.8f, 0f, 0f, 1f), 0.6f);
ScreenFx.ClearVignette(Mod.Id);
```

Blur the picture, and clear it later.

```csharp
ScreenFx.Blur(Mod.Id, 0.5f);
ScreenFx.ClearBlur(Mod.Id);
```

Change the text on the death screen.

```csharp
ScreenFx.SetDeathMessage("You lost track of time.");
```
