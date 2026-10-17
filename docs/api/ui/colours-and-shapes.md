---
title: Colours and shapes
parent: UI
grand_parent: Screen and UI
nav_order: 6
---

# Colours and shapes

This page uses `Sprite` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

`UiTheme` and `UiSprites` give you the kit's colours and shapes, so your own screens look the same. You can read them but not change them. The colours are properties that only give a value. Their names and types are fixed, so a mod that uses `UiTheme.Card` or `UiTheme.Font` directly keeps building when the framework updates.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>UiTheme</code> <span>the kit's colours and font</span></p>
`UiTheme` gives you the colours the framework's own windows and controls use, so yours can match them. Each one is read-only.

<div class="api-pair" markdown="1">

```csharp
public static class UiTheme
{
    public static Color Window { get; }
    public static Color Card { get; }
    public static Color Control { get; }
    public static Color Hover { get; }
    public static Color Border { get; }
    public static Color Text { get; }
    public static Color Muted { get; }
    public static Color Accent { get; }
    public static Color AccentHover { get; }
    public static Color AccentPressed { get; }
    public static Color Clear { get; }
    public static TMP_FontAsset Font { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Window` | `Color` | The window and backdrop colour, `#121418` at 0.97 alpha. |
| `Card` | `Color` | The panel colour, `#1B1E24`. |
| `Control` | `Color` | The colour of controls, `#262A32`. |
| `Hover` | `Color` | The colour of a control when you point at it, `#30353F`. |
| `Border` | `Color` | The colour of borders, `#2E333D`. |
| `Text` | `Color` | The colour of normal text, `#E9EBEF`. |
| `Muted` | `Color` | The colour of dim text, `#9AA1AC`. |
| `Accent` | `Color` | The main colour, `#4C8DFF`. |
| `AccentHover` | `Color` | The main colour when you point at it, `#6BA2FF`. |
| `AccentPressed` | `Color` | The main colour when pressed, `#3A76DA`. |
| `Clear` | `Color` | Fully see-through. |
| `Font` | `TMP_FontAsset` | The game's font. It is found the first time it is used. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>UiSprites</code> <span>the kit's shapes</span></p>
`Rounded(radius)` gives you a sprite with rounded corners of that radius, which the framework's own windows and controls use.

<div class="api-pair" markdown="1">

```csharp
public static class UiSprites
{
    public static Sprite Rounded(int radius);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Rounded(radius)` | `Sprite` | Makes a rounded rectangle. Each radius is made once. The radius is kept between 0 and 64. |

</div>
</div>

## Things to know

- The same colours drive the dark look of the ConfigurationManager window.
