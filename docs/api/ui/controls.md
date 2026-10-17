---
title: Controls
parent: UI
grand_parent: Screen and UI
nav_order: 4
---

# Controls

You build controls with `ModUi`, by giving it the area to put the control in. The control can be built in any area, not only in a window. `ModUi` finds out which mod a control belongs to by looking up from its parent: first a `ModWindow`, then the desk or full-screen app that hosts it, and if there is none, the framework's main log is used. If your code for a control throws an error under a window, the window closes with the reason `Fault`. Anywhere else the error is logged, and the rest of the screen keeps working. Build the controls once and keep what they give you. The controls change in place when you call `Set`, and `Set` never calls your code back.

```csharp
public enum ButtonStyle
{
    Primary,     // the main button, in the accent colour
    Secondary,   // a normal button
    Ghost        // a button with no background until you point at it
}

public enum PreviewFraming
{
    FullBody,   // the whole figure
    Face        // the face
}

public static class ModUi
{
    public static TextMeshProUGUI Title(Transform parent, string text);                  // a heading
    public static TextMeshProUGUI Text(Transform parent, string text, bool muted);       // a line of text; muted makes it dim
    public static RectTransform Card(Transform parent, string title);                    // a titled panel
    public static RectTransform Scroll(Transform parent);                                // a scrolling area; gives its content area
    public static UnityEngine.UI.Button Button(Transform parent, string text, ButtonStyle style, Action onClick);   // a button
    public static ModSlider Slider(Transform parent, string text, float min, float max, float value, Action<float> onChange, int decimals);   // a slider
    public static ModToggle Toggle(Transform parent, string text, bool value, Action<bool> onChange);       // an on and off switch
    public static ModStepper Stepper(Transform parent, string text, string[] options, int index, Action<int> onChange);   // a list with arrows
    public static ModTextField TextField(Transform parent, string label, string text, Action<string> onChange);   // a line of text to type in
    public static ModKeyBinding KeyBinding(Transform parent, string text, KeyboardShortcut value, Action<KeyboardShortcut> onChange);   // a key the player picks
    public static ModSwatches Swatches(Transform parent, Color[] colors, int index, Action<int> onChange);   // a row of colour chips
    public static ModTabs Tabs(Transform parent, string[] names, Action<int> onSelect);         // a tab bar with a page for each tab
    public static ModSegmented Segmented(Transform parent, string[] names, int index, Action<int> onSelect);   // a row of equal choices
    public static ModelPreview ModelPreview(RectTransform parent, Transform target, PreviewFraming framing);   // a 3D view of a model
    public static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax);   // sets where a control sits
    public static RectTransform Stack(Transform parent);   // an area that stacks its children top to bottom
    public static RectTransform Row(Transform parent);     // an area that lines its children up side by side
}
```

- **`Slider`, `Toggle`, `Stepper`, `TextField` and `KeyBinding` put a label on the left and the control on the right of a row.** The row is 52 pixels tall.
- **A slider takes clicks across its whole bar.** Its `decimals` (how many digits after the point it shows) is kept between 0 and 6. A value that is not a number, or is infinite, is shown as `-`.
  - Technical: the bar is 40 pixels tall.
- **Clicking a toggle's label flips the switch.**
- **`TextField` calls your code on every edit, and `KeyBinding` calls it when a key is picked.**
- **`Tabs` builds a tab bar with one scrolling page for each name.** `Segmented` is a row of equal-width choices with one selected, and a click calls your code with its number. Numbers start at 0.
- **`Card` is a titled panel.** `Card(parent, null)`, or an empty title, builds a panel with no title. It adds its own size fitter only when its parent has no layout group.
- **`Stack` and `Row` give an area with the kit's usual padding and gap, for you to fill.**
- **`Scroll` gives the content area of a scroll page.**
- **Clicking a button, a segment, a tab, a stepper, a toggle's label or a colour chip does not leave it selected.** This stops Enter and Space from repeating the click.
  - Technical: these controls clear the EventSystem selection when they are clicked and have their navigation set to none.

The controls that you can change later give you an object. You call `Set` on it, and `Set` never calls your change code back.

```csharp
public sealed class ModSlider
{
    public RectTransform Rect { get; }   // the control's area, to place it yourself
    public void Set(float value);        // moves the slider
}

public sealed class ModToggle
{
    public RectTransform Rect { get; }   // the control's area
    public void Set(bool on);            // switches it on or off
}

public sealed class ModStepper
{
    public RectTransform Rect { get; }                          // the control's area
    public void Set(int index);                                 // picks a choice by its number
    public void SetOptions(string[] names, int index);          // replaces the list of choices and picks one
}

public sealed class ModTextField
{
    public RectTransform Rect { get; }   // the control's area
    public void Set(string text);        // replaces the text
}

public sealed class ModKeyBinding
{
    public RectTransform Rect { get; }              // the control's area
    public void Set(KeyboardShortcut shortcut);     // changes the shown key
}

public sealed class ModSwatches
{
    public void Set(int index);   // picks a colour chip by its number
}

public sealed class ModTabs
{
    public RectTransform Page(int index);   // the page of a tab; fill it with controls
    public int Selected { get; }            // the number of the tab that is shown
    public void Set(int index);             // shows a tab
}

public sealed class ModSegmented
{
    public RectTransform Rect { get; }   // the control's area
    public void Set(int index);          // picks a choice by its number
}
```
