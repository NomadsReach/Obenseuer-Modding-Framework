---
title: UI
parent: Screen and UI
grand_parent: API reference
has_children: true
nav_order: 4
---

# UI

## What it is

This is the framework's small kit for building screens in your mod. It has windows (`ModWindow`), ready-made controls such as buttons, sliders and toggles (`ModUi`), and the colours and shapes the kit uses (`UiTheme`, `UiSprites`). Screens built with it match the framework's Mods menu. If you would rather write your screen in HTML, see the [WebUi page](webui.md).

## When to use it

- Open a window over the game with a title, text, buttons and settings controls.
- Fill a page of the Mods menu with controls (see the [Mods menu page guide](../guides/cookbook/mods-menu-page.md)).
- Show the player's model in a window, and let the player turn it.
- Reuse the kit's colours so your own screen looks the same.

## Quick example

This mod opens a window when you press F8, and closes it when you press F8 again. The window has a heading, a line of text, a slider, a toggle and a button.

Your project needs references to `Unity.TextMeshPro.dll` and `UnityEngine.UI.dll`, both from the game's `Obenseuer_Data\Managed` folder. Without them the compiler reports error CS0012. The first lines of the file add the kit's namespace.

```csharp
using ObenseuerCore;
using ObenseuerCore.Ui;
using UnityEngine;

[ModInfo("example.uidemo", "UI Demo", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class UiDemo : FrameworkMod
{
    private const string WindowTitle = "UI Demo";
    private ModKeybind openKey;
    private float volume = 5f;
    private bool soundOn = true;

    protected override void OnLoad()
    {
        openKey = Mod.Key("Window", "Open or close the demo window", KeyCode.F8);
    }

    private void Update()
    {
        if (openKey == null || !openKey.Down()) return;
        ModWindow open = ModWindow.Find(Mod.Id, WindowTitle);
        if (open != null)
        {
            open.Close();
            return;
        }
        if (UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        ModWindow.Open(Mod.Id, WindowTitle, WindowDock.Center, window =>
        {
            ModUi.Title(window.Body, "Demo window");
            ModUi.Text(window.Body, "A window built with the framework's UI kit.", true);
            ModUi.Slider(window.Body, "Volume", 0f, 10f, volume, value => volume = value, 1);
            ModUi.Toggle(window.Body, "Sound", soundOn, on => soundOn = on);
            ModUi.Button(window.Body, "Close", ButtonStyle.Primary, window.Close);
        });
    }
}
```

`ModWindow.Open` builds the window and runs your code once to fill it. The `window.Body` is the empty area to put controls in. The controls call your code when the player changes them. While a window is open the game's own controls are off, so the example checks the game state only before it opens the window. See the [keybinds guide](../guides/getting-started/keybinds.md) for `Mod.Key`.
