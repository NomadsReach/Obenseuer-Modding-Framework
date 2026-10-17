---
title: Controls
parent: UI
grand_parent: Screen and UI
nav_order: 4
---

# Controls

You build controls with `ModUi`, by giving it the area to put the control in. The control can be built in any area, not only in a window. The controls that you can change later give you an object, and you call `Set` on it. `Set` never calls your change code back.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ButtonStyle</code> <span>how a button looks</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum ButtonStyle
{
    Primary,
    Secondary,
    Ghost
}
```

| Value | Meaning |
|---|---|
| `Primary` | The main button, in the accent colour. |
| `Secondary` | A normal button. |
| `Ghost` | A button with no background until you point at it. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PreviewFraming</code> <span>how a model preview is framed</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum PreviewFraming
{
    FullBody,
    Face
}
```

| Value | Meaning |
|---|---|
| `FullBody` | The whole figure. |
| `Face` | The face. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModUi</code> <span>builds the controls</span></p>
<div class="api-pair" markdown="1">

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

| Method | Returns | What it does |
|---|---|---|
| `Title(Transform parent, string text)` | `TextMeshProUGUI` | Adds a heading. |
| `Text(Transform parent, string text, bool muted)` | `TextMeshProUGUI` | Adds a line of text. `muted` makes it dim. |
| `Card(Transform parent, string title)` | `RectTransform` | Adds a titled panel. |
| `Scroll(Transform parent)` | `RectTransform` | Adds a scrolling area. It gives you its content area. |
| `Button(Transform parent, string text, ButtonStyle style, Action onClick)` | `UnityEngine.UI.Button` | Adds a button. |
| `Slider(Transform parent, string text, float min, float max, float value, Action<float> onChange, int decimals)` | `ModSlider` | Adds a slider. |
| `Toggle(Transform parent, string text, bool value, Action<bool> onChange)` | `ModToggle` | Adds an on and off switch. |
| `Stepper(Transform parent, string text, string[] options, int index, Action<int> onChange)` | `ModStepper` | Adds a list with arrows. |
| `TextField(Transform parent, string label, string text, Action<string> onChange)` | `ModTextField` | Adds a line of text to type in. |
| `KeyBinding(Transform parent, string text, KeyboardShortcut value, Action<KeyboardShortcut> onChange)` | `ModKeyBinding` | Adds a key the player picks. |
| `Swatches(Transform parent, Color[] colors, int index, Action<int> onChange)` | `ModSwatches` | Adds a row of colour chips. |
| `Tabs(Transform parent, string[] names, Action<int> onSelect)` | `ModTabs` | Adds a tab bar with a page for each tab. |
| `Segmented(Transform parent, string[] names, int index, Action<int> onSelect)` | `ModSegmented` | Adds a row of equal choices. |
| `ModelPreview(RectTransform parent, Transform target, PreviewFraming framing)` | `ModelPreview` | Adds a 3D view of a model. |
| `Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)` | `void` | Sets where a control sits. |
| `Stack(Transform parent)` | `RectTransform` | Adds an area that stacks its children top to bottom. |
| `Row(Transform parent)` | `RectTransform` | Adds an area that lines its children up side by side. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModSlider</code> <span>a slider you can move</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ModSlider
{
    public RectTransform Rect { get; }
    public void Set(float value);
}
```

| Property | Type | What it means |
|---|---|---|
| `Rect` | `RectTransform` | The control's area, to place it yourself. |

| Method | Returns | What it does |
|---|---|---|
| `Set(float value)` | `void` | Moves the slider. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModToggle</code> <span>an on and off switch</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ModToggle
{
    public RectTransform Rect { get; }
    public void Set(bool on);
}
```

| Property | Type | What it means |
|---|---|---|
| `Rect` | `RectTransform` | The control's area. |

| Method | Returns | What it does |
|---|---|---|
| `Set(bool on)` | `void` | Switches it on or off. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModStepper</code> <span>a list with arrows</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ModStepper
{
    public RectTransform Rect { get; }
    public void Set(int index);
    public void SetOptions(string[] names, int index);
}
```

| Property | Type | What it means |
|---|---|---|
| `Rect` | `RectTransform` | The control's area. |

| Method | Returns | What it does |
|---|---|---|
| `Set(int index)` | `void` | Picks a choice by its number. |
| `SetOptions(string[] names, int index)` | `void` | Replaces the list of choices and picks one. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModTextField</code> <span>a line of text to type in</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ModTextField
{
    public RectTransform Rect { get; }
    public void Set(string text);
}
```

| Property | Type | What it means |
|---|---|---|
| `Rect` | `RectTransform` | The control's area. |

| Method | Returns | What it does |
|---|---|---|
| `Set(string text)` | `void` | Replaces the text. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModKeyBinding</code> <span>a key the player picks</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ModKeyBinding
{
    public RectTransform Rect { get; }
    public void Set(KeyboardShortcut shortcut);
}
```

| Property | Type | What it means |
|---|---|---|
| `Rect` | `RectTransform` | The control's area. |

| Method | Returns | What it does |
|---|---|---|
| `Set(KeyboardShortcut shortcut)` | `void` | Changes the shown key. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModSwatches</code> <span>a row of colour chips</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ModSwatches
{
    public void Set(int index);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Set(int index)` | `void` | Picks a colour chip by its number. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModTabs</code> <span>a tab bar with a page for each tab</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ModTabs
{
    public RectTransform Page(int index);
    public int Selected { get; }
    public void Set(int index);
}
```

| Property | Type | What it means |
|---|---|---|
| `Selected` | `int` | The number of the tab that is shown. |

| Method | Returns | What it does |
|---|---|---|
| `Page(int index)` | `RectTransform` | The page of a tab. Fill it with controls. |
| `Set(int index)` | `void` | Shows a tab. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModSegmented</code> <span>a row of equal choices</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ModSegmented
{
    public RectTransform Rect { get; }
    public void Set(int index);
}
```

| Property | Type | What it means |
|---|---|---|
| `Rect` | `RectTransform` | The control's area. |

| Method | Returns | What it does |
|---|---|---|
| `Set(int index)` | `void` | Picks a choice by its number. |

</div>
</div>

## Things to know

- `ModUi` finds out which mod a control belongs to by looking up from its parent: first a `ModWindow`, then the desk or full-screen app that hosts it. If there is none, the framework's main log is used.
- If your code for a control throws an error under a window, the window closes with the reason `Fault`. Anywhere else the error is logged, and the rest of the screen keeps working.
- Build the controls once and keep what they give you.
- `Slider`, `Toggle`, `Stepper`, `TextField` and `KeyBinding` put a label on the left and the control on the right of a row. The row is 52 pixels tall.
- A slider takes clicks across its whole bar. Its `decimals` (how many digits after the point it shows) is kept between 0 and 6. A value that is not a number, or is infinite, is shown as `-`. <span class="tech">(The bar is 40 pixels tall.)</span>
- Clicking a toggle's label flips the switch. `TextField` calls your code on every edit, and `KeyBinding` calls it when a key is picked.
- `Tabs` builds a tab bar with one scrolling page for each name. `Segmented` is a row of equal-width choices with one selected, and a click calls your code with its number. Numbers start at 0.
- `Card` is a titled panel. `Card(parent, null)`, or an empty title, builds a panel with no title. It adds its own size fitter only when its parent has no layout group.
- `Stack` and `Row` give an area with the kit's usual padding and gap, for you to fill. `Scroll` gives the content area of a scroll page.
- Clicking a button, a segment, a tab, a stepper, a toggle's label or a colour chip does not leave it selected. This stops Enter and Space from repeating the click. <span class="tech">(These controls clear the EventSystem selection when they are clicked and have their navigation set to none.)</span>
