---
title: Blur
parent: Screen effects
grand_parent: Screen and UI
nav_order: 8
---

# Blur

```csharp
public static bool Blur(string owner, float amount);                      // blur the picture
public static bool Blur(string owner, float amount, float fadeSeconds);   // the same, changing over fadeSeconds
public static bool ClearBlur(string owner);                               // remove your blur
```

- **`Blur` blurs the 3D picture, the same effect the game uses behind its menus.** `amount` goes from 0 to 1: 0 is no blur and 1 is the strongest. A number outside that range is changed to the nearest edge, and one that is not a real number throws an `ArgumentException`.
  - Technical: it maps onto the game's `Beautify.blurIntensity`, which runs from 0 to 4.
- **`fadeSeconds` makes the blur change gradually, in the same way as for the vignette.** It keeps going while the game is paused, and `0` is the same as the version without it. A new change during a fade starts from the amount on the screen. A negative number or one that is not a real number throws an `ArgumentException`.
- **Only the 3D picture is blurred.** The game's menus and HUD are drawn on top and are not blurred. A canvas that the game draws with a camera is blurred.
  - Technical: a Screen Space Overlay canvas is drawn after the cameras, so it is not blurred. A canvas in Screen Space Camera mode is drawn by its camera, so it is.
- **The game also turns blur on while a menu is open, and off when it closes.** While your mod holds a blur, the framework keeps it on through those changes. When the last mod clears its blur, the game's own state comes back: blur on while a menu is open, off otherwise, with the game's own strength.
- **When the game loads a new set of picture settings, the framework holds your blur again.** It takes the new settings as the game's own.
  - Technical: this is `ChangeBeautifyProfiles` or `ChangeBeautifyProfilesToDefault`.
- **Each mod has one blur, and the one set last is shown.** When that mod clears its blur, the one set before it shows again. At most 4 mods can hold a blur at the same time. A fifth is refused: `Blur` returns `false` and logs one line. A mod that already has a blur can keep changing it.
- **`ClearBlur` removes your blur.** It returns `true` when it removed one and `false` when your mod had none. It always runs.
- **`Blur` returns `false` when the framework could not set up the blur.** Setting a blur works during a save. It does not depend on the field of view override, so it works when `IsAvailable` is `false`.
  - Technical: the blur hooks are `EffectsController.EnableBlur`, `DisableBlur`, `ChangeBeautifyProfiles` and `ChangeBeautifyProfilesToDefault`, in their own feature group (see Details).
- **A normal level load removes every blur and gives the game's own values back, and loading extra scenes in the background keeps them.** The kept blurs also apply to any game camera that the extra scene adds. Nothing is saved.
  - Technical: a Single scene load removes every entry. An additive load keeps them.
