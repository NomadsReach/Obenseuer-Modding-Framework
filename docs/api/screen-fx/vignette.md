---
title: Vignette
parent: Screen effects
grand_parent: Screen and UI
nav_order: 7
---

# Vignette

```csharp
public static bool Vignette(string owner, Color color, float strength);                      // darken the screen edges
public static bool Vignette(string owner, Color color, float strength, float fadeSeconds);   // the same, changing over fadeSeconds
public static bool ClearVignette(string owner);                                              // remove your vignette
```

- **A vignette darkens the edges of the screen with a colour.** It is drawn over the game's picture. It does not change the game's own vignette, its picture settings or its camera.
  - Technical: it is a screen-space canvas with its own object, kept across scenes, that does not block input. It draws over the 3D picture and under the game's own overlay canvases, such as the HUD, menus and the black fade. The framework's computer app overlay and mod windows draw above it. A game canvas in Screen Space Camera or World Space mode draws under the vignette whatever its sort order.
- **`color` is the tint, and its alpha makes it more or less see-through.** A colour with a value that is not a real number throws an `ArgumentException`.
- **`strength` goes from 0 to 1 and changes two things at once.** It sets how far the darkening reaches in from the edges, and how dark it is at the very edge. So a strength of 0.5 looks much weaker than half of a strength of 1. A strength of 0 shows nothing but keeps your entry. A number outside 0 to 1 is changed to the nearest edge, and one that is not a real number throws an `ArgumentException`.
- **`fadeSeconds` makes the vignette change gradually.** It moves from what is on the screen now to the new colour and strength over that many seconds, and keeps going while the game is paused. `0` is the same as the version without it. A new change during a fade starts from what is on the screen, so it does not jump. A negative number or one that is not a real number throws an `ArgumentException`.
- **Each mod has one vignette, and the one set last is shown.** When that mod clears its vignette, the one set before it shows again, with the values it had.
- **At most 4 mods can hold a vignette at the same time.** A fifth is refused: `Vignette` returns `false` and logs one line. A mod that already has a vignette can keep changing it.
  - Technical: this is the same cap that `Hud.Bar` puts on bars.
- **`ClearVignette` removes your vignette.** It returns `true` when it removed one and `false` when your mod had none. It always runs.
- **Setting a vignette works during a save, and does not depend on the field of view override.** It keeps working when `IsAvailable` is `false`.
- **A normal level load removes every vignette, and loading extra scenes in the background keeps them.** Nothing is saved.
  - Technical: a Single scene load removes every entry. An additive load keeps them.
- **The vignette does not touch the game's own vignette or its picture settings.**
  - Technical: it does not read or write the game's post-process settings.
