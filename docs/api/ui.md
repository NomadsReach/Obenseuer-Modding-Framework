---
title: Obenseuer Modding Framework Ui API (ModUi, UiTheme, UiSprites)
parent: API reference
nav_order: 87
---

# Obenseuer Modding Framework Ui API (ModUi, UiTheme, UiSprites)

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
## Setup

Reference `Unity.TextMeshPro.dll` and `UnityEngine.UI.dll` from `Obenseuer_Data\Managed`. `ModUi.Title` and `ModUi.Text` return `TextMeshProUGUI`, and `ModUi.Button` returns `UnityEngine.UI.Button`. Without both references the compiler reports CS0012 (the type is defined in an assembly that is not referenced).

Add `using ObenseuerCore.Ui;` to each file that uses these types. `KeyBinding` takes a `KeyboardShortcut` from BepInEx, so a file that calls it also needs `using BepInEx.Unity.Mono.Configuration;` on BepInEx 6, or `using BepInEx.Configuration;` on BepInEx 5.

```csharp
using BepInEx.Unity.Mono.Configuration; // BepInEx 6
// using BepInEx.Configuration;         // BepInEx 5
using ObenseuerCore.Ui;
```

## Ui

Widgets can be built in any host. `ModUi` finds the owner of its parent: the `ModWindow` above it, else the desk or full-screen app host above it (its mod id is used for logs), else none (logs go to `ObenseuerCore`). Under a window, a callback that throws closes the window with reason `Fault`. Anywhere else it is logged to the owner's log (`ui callback failed: ...`), and the rest of the UI keeps working. Build the controls once and keep the handles. The widget classes change in place through `Set`, and `Set` never calls back.

```csharp
public enum ButtonStyle { Primary, Secondary, Ghost }
public enum WindowDock { Left, Right, Center, Full }
public enum WindowCloseReason { Requested, Escape, SaveLoad, SceneUnload, Destroyed, Fault }
public enum PreviewFraming { FullBody, Face }

public sealed class ModWindow : MonoBehaviour
{
    public static ModWindow Open(string modId, string title, WindowDock dock, Action<ModWindow> build);
    public static ModWindow Find(string modId, string title);
    public void Close();
    public void Run(Action action);
    public string ModId { get; }
    public string Title { get; }
    public WindowDock Dock { get; }
    public bool Faulted { get; }
    public WindowCloseReason CloseReason { get; }
    public RectTransform Body { get; }
    public RectTransform Side { get; }
    public RectTransform Footer { get; }
    public event Action Closed;
}
```

`Open` returns null when a window with the same mod id and title is already open (that window is raised instead), or when its setup fails (the window is destroyed and the error is logged). `build` runs under `Run`. An exception closes the window with reason `Fault`, sets `Faulted` and logs once. `Closed` fires before the window's previews are disposed, so a mod can flush its own work there. Escape closes only the top window, and not while a text field in it has focus. `Find` returns the open window with that mod id and title, or null, so a hotkey can close it. A window closes with `SceneUnload` only when the scene it was opened in unloads; other additive scene unloads are ignored.

Dock sizes: Left and Right are 440 px panels with 24 px margins. Right starts 110 px from the top. Center is 640 px wide. Left, Right and Center block clicks across their whole panel. Full covers the screen with a 0.97 alpha backdrop that hides the game and blocks its clicks. Full also has a 440 px `Side` column on the left, with `Body` to its right. The footer is a 56 px bar along the bottom.

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

`Slider`, `Toggle`, `Stepper`, `TextField` and `KeyBinding` put a label on the left and the control on the right of a 52 px row. A slider takes clicks across its full 40 px bar, and its `decimals` is clamped to 0 to 6. A NaN or infinite value is shown as `-`. A click on a toggle's label flips the switch. `TextField` calls `onChange` on every edit, and `KeyBinding` calls it when a key is bound. `Tabs` builds a tab bar with one scroll page per name. `Segmented` is a row of equal-width choices with one selected. A click calls `onSelect` with the index. `Card` is a titled panel. It adds its own size fitter only when its parent has no layout group. `Card(parent, null)` (or an empty title) builds an untitled panel. `Stack` and `Row` return a layout group rect with the theme's padding and gap, for the caller to fill. The clickable kit controls (`Button`, the segments of `Segmented`, `Tabs`, the steppers, the caption of `Toggle`, and the swatch chips) clear the EventSystem selection when clicked and have navigation set to none, so Enter and Space do not repeat a click. `Scroll` returns the content of a scroll page.

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
