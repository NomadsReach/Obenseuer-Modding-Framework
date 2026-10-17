---
title: Blur
parent: Screen effects
grand_parent: Screen and UI
nav_order: 8
---

# Blur

These calls blur the 3D picture, the same effect the game uses behind its menus.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ScreenFx</code> <span>blur the 3D picture</span></p>
<div class="api-pair" markdown="1">

```csharp
public static bool Blur(string owner, float amount);
public static bool Blur(string owner, float amount, float fadeSeconds);
public static bool ClearBlur(string owner);
```

| Method | Returns | What it does |
|---|---|---|
| `Blur(string owner, float amount)` | `bool` | Blurs the picture. `amount` runs from 0 (no blur) to 1 (strongest). |
| `Blur(string owner, float amount, float fadeSeconds)` | `bool` | The same blur, changing over `fadeSeconds` seconds. |
| `ClearBlur(string owner)` | `bool` | Removes your blur. It returns `true` when it removed one and `false` when your mod had none. It always runs. |

</div>
</div>

## Things to know

- Only the 3D picture is blurred. The game's menus and HUD are drawn on top and are not blurred. A canvas that the game draws with a camera is blurred. <span class="tech">(A Screen Space Overlay canvas is drawn after the cameras, so it is not blurred. A canvas in Screen Space Camera mode is drawn by its camera, so it is.)</span>
- `fadeSeconds` makes the blur change gradually, in the same way as for the vignette. It keeps going while the game is paused, and `0` is the same as the version without it. A new change during a fade starts from the amount on the screen.
- The game also turns blur on while a menu is open, and off when it closes. While your mod holds a blur, the framework keeps it on through those changes. When the last mod clears its blur, the game's own state comes back: blur on while a menu is open, off otherwise, with the game's own strength.
- When the game loads a new set of picture settings, the framework holds your blur again and takes the new settings as the game's own. <span class="tech">(This is `ChangeBeautifyProfiles` or `ChangeBeautifyProfilesToDefault`.)</span>
- Each mod has one blur, and the one set last is shown. When that mod clears its blur, the one set before it shows again. A mod that already has a blur can keep changing it.
- Setting a blur works during a save. It does not depend on the field of view override, so it works when `IsAvailable` is `false`. <span class="tech">(The blur hooks are `EffectsController.EnableBlur`, `DisableBlur`, `ChangeBeautifyProfiles` and `ChangeBeautifyProfilesToDefault`, in their own feature group (see Details).)</span>
- A normal level load removes every blur and gives the game's own values back. Loading extra scenes in the background keeps them, and the kept blurs also apply to any game camera that the extra scene adds. Nothing is saved. <span class="tech">(A Single scene load removes every entry. An additive load keeps them.)</span>

## Limits

- A number outside 0 to 1 for `amount` is changed to the nearest edge. A number that is not a real number throws an `ArgumentException`. <span class="tech">(`amount` maps onto the game's `Beautify.blurIntensity`, which runs from 0 to 4.)</span>
- A negative `fadeSeconds`, or one that is not a real number, throws an `ArgumentException`.
- At most 4 mods can hold a blur at the same time. A fifth is refused: `Blur` returns `false` and logs one line.
- `Blur` returns `false` when the framework could not set up the blur.
