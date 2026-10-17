---
title: Screen effects
parent: Screen and UI
grand_parent: API reference
has_children: true
nav_order: 3
---

# Screen effects

## What it is

ScreenFx changes how the screen looks while the game runs. It can shake the camera, fade the screen to black and back, change the message shown when the player dies, change the camera's field of view (how wide the view is, which is how you make a zoom), darken the edges of the screen with a coloured vignette, and blur the picture. Effects that take an `owner` belong to your mod, so you clear them with the matching `Clear` call when you are done. The `owner` is your mod's id (see the [glossary](../glossary.md#owner)).

## When to use it

- Add a zoom key by narrowing the field of view while a key is held.
- Shake the screen for an explosion or a hit, or fade to black for a scene change.
- Tint the screen edges red when the player is hurt, or blur the picture while your own menu is open.
- Change the text on the death screen to fit your mod.

## Quick example

This mod adds a zoom key. While the player holds Z, the view narrows to 30 degrees. When the key is let go, the player's normal view comes back.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.zoom", "Zoom", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ZoomMod : FrameworkMod
{
    private ModKeybind zoom;

    protected override void OnLoad()
    {
        zoom = Mod.Key("Zoom", "Zoom in", KeyCode.Z);
    }

    private void Update()
    {
        if (zoom == null) return;
        if (zoom.Down()) ScreenFx.SetFovOverride(Mod.Id, 30f);
        if (zoom.Up()) ScreenFx.ClearFovOverride(Mod.Id);
    }
}
```

`Mod.Key` makes a key the player can see and change in the Mods menu. `Down()` is `true` on the frame the key is pressed and `Up()` on the frame it is let go. `Update` runs every frame. See [Keybinds](../guides/getting-started/keybinds.md) for more on keys. The first argument of `SetFovOverride` is `Mod.Id`, your mod's id.
