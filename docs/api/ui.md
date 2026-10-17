---
title: UI
parent: Screen and UI
grand_parent: API reference
has_children: true
nav_order: 4
---

# UI

This page uses `KeyCode` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

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
`Open` returns null when a window with the same mod id and title is already open (that window is raised instead), or when its setup fails (the window is destroyed and the error is logged). `build` runs under `Run`. An exception closes the window with reason `Fault`, sets `Faulted` and logs once. `Closed` fires before the window's previews are disposed, so a mod can flush its own work there. Escape closes only the top window, and not while a text field in it has focus. `Find` returns the open window with that mod id and title, or null, so a hotkey can close it. A window closes with `SceneUnload` only when the scene it was opened in unloads; other additive scene unloads are ignored.

Dock sizes: Left and Right are 440 px panels with 24 px margins. Right starts 110 px from the top. Center is 640 px wide. Left, Right and Center block clicks across their whole panel. Full covers the screen with a 0.97 alpha backdrop that hides the game and blocks its clicks. Full also has a 440 px `Side` column on the left with `Body` to its right; the framework's own Mods menu asks for a proportional side column instead (23.5% of the window, about 440 px at 16:9), so a mod's own Full window keeps the fixed width. The footer is a 56 px bar along the bottom.

```csharp
public static class ModUi
{
    public static TextMeshProUGUI Title(Transform parent, string text);
    public static TextMeshProUGUI Text(Transform parent, string text, bool muted);
    public static RectTransform Card(Transform parent, string title);
    public static RectTransform Scroll(Transform parent);
    public static UnityEngine.UI.Button Button(Transform parent, string text, ButtonStyle style, Action onClick);
    public static ModSlider Slider(Transform parent, string text, float min, float max, float value, Action<float> onChange, int decimals);
    public static ModToggle Toggle(Transform parent, string text, bool value, Action<bool> onChange);
    public static ModStepper Stepper(Transform parent, string text, string[] options, int index, Action<int> onChange);
    public static ModTextField TextField(Transform parent, string label, string text, Action<string> onChange);
    public static ModKeyBinding KeyBinding(Transform parent, string text, KeyboardShortcut value, Action<KeyboardShortcut> onChange);
    public static ModSwatches Swatches(Transform parent, Color[] colors, int index, Action<int> onChange);
    public static ModTabs Tabs(Transform parent, string[] names, Action<int> onSelect);
    public static ModSegmented Segmented(Transform parent, string[] names, int index, Action<int> onSelect);
    public static ModelPreview ModelPreview(RectTransform parent, Transform target, PreviewFraming framing);
    public static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax);
    public static RectTransform Stack(Transform parent);
    public static RectTransform Row(Transform parent);
}
```

The clickable kit controls (`Button`, the segments of `Segmented`, `Tabs`, the steppers, the caption of `Toggle`, and the swatch chips) clear the EventSystem selection when clicked, so Enter and Space do not repeat a click. The kit leaves `Selectable.navigation` alone, except the scroll page's scrollbar, which is set to None so a click on it cannot move the focus; the classic Mods menu wires it explicitly, see [framework core classes](../api-core.md#modmenu). `Scroll` returns the content of a scroll page, and a scroll page has a viewport with a clipping mask and a visible scrollbar on the right.

```csharp
public sealed class ModSlider
{
    public RectTransform Rect { get; }
    public void Set(float value);
}

public sealed class ModToggle
{
    public RectTransform Rect { get; }
    public void Set(bool on);
}

public sealed class ModStepper
{
    public RectTransform Rect { get; }
    public void Set(int index);
    public void SetOptions(string[] names, int index);
}

public sealed class ModTextField
{
    public RectTransform Rect { get; }
    public void Set(string text);
}

public sealed class ModKeyBinding
{
    public RectTransform Rect { get; }
    public void Set(KeyboardShortcut shortcut);
}

public sealed class ModSwatches
{
    public void Set(int index);
}

public sealed class ModTabs
{
    public RectTransform Page(int index);
    public int Selected { get; }
    public void Set(int index);
}

public sealed class ModSegmented
{
    public RectTransform Rect { get; }
    public void Set(int index);
}

public sealed class ModelPreview : MonoBehaviour
{
    public PreviewFraming Framing { get; set; }
    public bool ShowFramingSwitch { get; set; }  // default true
    public RenderTexture Texture { get; }
    public int Renderers { get; }
    public void Refresh();
    public int Dispose();
}
```

A preview draws the target's renderers on layer 31 while its window is open. These include the renderers of `PlayerLocator.playerRig` and `playerRigUnrigified`, which are switched on for the window and switched back off when it closes. Only renderers on active GameObjects are claimed. Only one preview can be open at a time; a second `ModUi.ModelPreview` faults the window it is in. Drag turns the figure, and the wheel zooms. `Framing` reframes at once and moves the switch. `ShowFramingSwitch` shows or hides the Body|Face switch that the preview builds in its top-right corner. `Refresh()` adopts renderers that became active since the last call, for example after a mesh swap, and reframes. The camera and lights are disabled while the preview's tab page is hidden. `Dispose()` restores every change, then shows or hides the player model to match the game's current first or third person view, returns the recorded renderer count, and is safe to call twice. A skinned mesh's bounds are restored only when its shared mesh is the one recorded. The window disposes its previews when it closes. On the first settled frame the window logs one `model preview:` line with the recorded, active, visible and rig counts, and the first ten active renderers that are not visible.

## UiTheme and UiSprites

A read-only facade over the kit's palette and sprites. The names and types are fixed. The palette members are get-only properties. A mod that uses `UiTheme.Card` and `UiTheme.Font` directly breaks at build time if any member is renamed.

```csharp
public static class UiTheme
{
    public static Color Window { get; }        // window and backdrop colour, #121418 at 0.97 alpha
    public static Color Card { get; }          // #1B1E24
    public static Color Control { get; }       // #262A32
    public static Color Hover { get; }         // #30353F
    public static Color Border { get; }        // #2E333D
    public static Color Text { get; }          // #E9EBEF
    public static Color Muted { get; }         // #9AA1AC
    public static Color Accent { get; }        // #4C8DFF
    public static Color AccentHover { get; }   // #6BA2FF
    public static Color AccentPressed { get; } // #3A76DA
    public static Color Clear { get; }         // fully transparent
    public static TMP_FontAsset Font { get; }   // the game's font, found on first use
}

public static class UiSprites
{
    public static Sprite Rounded(int radius);   // rounded-rectangle sprite, cached per radius, radius clamped to 0..64
}
```

The same palette drives the ConfigurationManager dark theme.

Open questions:

- Text field and key binding rows: focus, typing, Esc and Enter inside a real window, and the field lock released on blur.
- A throwing callback under a window (Fault) and outside one (log only), in game.
- The head, hands and body in the preview. The cause and the fix come from the game code, not from a run.
- Clicks, wheel and drag on tab pages in a Full window.
- Lighting, framing and the backdrop look, including a 330 px tall preview in a Right dock.
- The release fallback when `ControlsEnabled` throws (keys, cursor and camera).
- The first or third person re-assert on close while the game flips the view.
- The preview pausing on a hidden tab, slider row clicks, toggle label clicks, and a `Find`-based hotkey toggle.
