---
title: Colours and shapes
parent: UI
grand_parent: Screen and UI
nav_order: 6
---

# Colours and shapes

`UiTheme` and `UiSprites` give you the kit's colours and shapes, so your own screens look the same. You can read them but not change them. The colours are properties that only give a value. Their names and types are fixed, so a mod that uses `UiTheme.Card` or `UiTheme.Font` directly keeps building when the framework updates.

```csharp
public static class UiTheme
{
    public static Color Window { get; }        // the window and backdrop colour, #121418 at 0.97 alpha
    public static Color Card { get; }          // the panel colour, #1B1E24
    public static Color Control { get; }       // the colour of controls, #262A32
    public static Color Hover { get; }         // a control when you point at it, #30353F
    public static Color Border { get; }        // borders, #2E333D
    public static Color Text { get; }          // normal text, #E9EBEF
    public static Color Muted { get; }         // dim text, #9AA1AC
    public static Color Accent { get; }        // the main colour, #4C8DFF
    public static Color AccentHover { get; }   // the main colour when you point at it, #6BA2FF
    public static Color AccentPressed { get; } // the main colour when pressed, #3A76DA
    public static Color Clear { get; }         // fully see-through
    public static TMP_FontAsset Font { get; }  // the game's font, found the first time it is used
}

public static class UiSprites
{
    public static Sprite Rounded(int radius);   // a rounded rectangle; made once for each radius; radius is kept between 0 and 64
}
```

The same colours drive the dark look of the ConfigurationManager window.
