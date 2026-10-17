---
title: Vignette
parent: Screen effects
grand_parent: Screen and UI
nav_order: 7
---

# Vignette

A vignette darkens the edges of the screen with a colour. Each mod has its own vignette.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ScreenFx</code> <span>darken the screen edges</span></p>
<div class="api-pair" markdown="1">

```csharp
public static bool Vignette(string owner, Color color, float strength);
public static bool Vignette(string owner, Color color, float strength, float fadeSeconds);
public static bool ClearVignette(string owner);
```

| Method | Returns | What it does |
|---|---|---|
| `Vignette(string owner, Color color, float strength)` | `bool` | Darkens the screen edges with a colour. |
| `Vignette(string owner, Color color, float strength, float fadeSeconds)` | `bool` | The same, changing over `fadeSeconds` seconds. |
| `ClearVignette(string owner)` | `bool` | Removes your vignette. It returns `true` when it removed one and `false` when your mod had none. It always runs. |

</div>
</div>

## Things to know

- It is drawn over the game's picture. It does not change the game's own vignette, its picture settings or its camera. <span class="tech">(It is a screen-space canvas with its own object, kept across scenes, that does not block input. It draws over the 3D picture and under the game's own overlay canvases, such as the HUD, menus and the black fade. The framework's computer app overlay and mod windows draw above it. A game canvas in Screen Space Camera or World Space mode draws under the vignette whatever its sort order. It does not read or write the game's post-process settings.)</span>
- `color` is the tint, and its alpha makes it more or less see-through.
- `strength` goes from 0 to 1 and changes two things at once. It sets how far the darkening reaches in from the edges, and how dark it is at the very edge. So a strength of 0.5 looks much weaker than half of a strength of 1. A strength of 0 shows nothing but keeps your entry.
- `fadeSeconds` makes the vignette change gradually. It moves from what is on the screen now to the new colour and strength over that many seconds, and keeps going while the game is paused. `0` is the same as the version without it. A new change during a fade starts from what is on the screen, so it does not jump.
- Each mod has one vignette, and the one set last is shown. When that mod clears its vignette, the one set before it shows again, with the values it had.
- Setting a vignette works during a save, and does not depend on the field of view override. It keeps working when `IsAvailable` is `false`.
- A normal level load removes every vignette, and loading extra scenes in the background keeps them. Nothing is saved. <span class="tech">(A Single scene load removes every entry. An additive load keeps them.)</span>

## Limits

- A colour with a value that is not a real number throws an `ArgumentException`.
- A number outside 0 to 1 for `strength` is changed to the nearest edge. A number that is not a real number throws an `ArgumentException`.
- A negative `fadeSeconds`, or one that is not a real number, throws an `ArgumentException`.
- At most 4 mods can hold a vignette at the same time. A fifth is refused: `Vignette` returns `false` and logs one line. A mod that already has a vignette can keep changing it. <span class="tech">(This is the same cap that `Hud.Bar` puts on bars.)</span>
