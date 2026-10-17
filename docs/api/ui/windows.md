---
title: Windows
parent: UI
grand_parent: Screen and UI
nav_order: 3
---

# Windows

A window is a screen the player sees over the game, with its own areas to fill. You open one with `ModWindow.Open` and give it your code to fill it. The controls can also be built in other places, not only in a window (see the next section).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WindowDock</code> <span>where a window sits</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum WindowDock
{
    Left,
    Right,
    Center,
    Full
}
```

| Value | Meaning |
|---|---|
| `Left` | A panel at the left of the screen. |
| `Right` | A panel at the right of the screen. |
| `Center` | A panel in the middle of the screen. |
| `Full` | The whole screen. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WindowCloseReason</code> <span>why a window closed</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum WindowCloseReason
{
    Requested,
    Escape,
    SaveLoad,
    SceneUnload,
    Destroyed,
    Fault
}
```

| Value | Meaning |
|---|---|
| `Requested` | Your mod, or the player, closed it. |
| `Escape` | The player pressed Escape. |
| `SaveLoad` | A save was loaded. |
| `SceneUnload` | The scene the window was opened in was unloaded. |
| `Destroyed` | The window's object was destroyed. |
| `Fault` | Your code threw an error. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModWindow</code> <span>a screen over the game</span></p>
<div class="api-pair" markdown="1">

```csharp
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

| Method | Returns | What it does |
|---|---|---|
| `Open(string modId, string title, WindowDock dock, Action<ModWindow> build)` | `ModWindow` | Opens a window. Your build code fills it. |
| `Find(string modId, string title)` | `ModWindow` | Gives the open window with this mod id and title, or `null` if none. |
| `Close()` | `void` | Closes the window. |
| `Run(Action action)` | `void` | Runs your code safely inside the window. |

| Property | Type | What it means |
|---|---|---|
| `ModId` | `string` | The mod id the window was opened with. |
| `Title` | `string` | The window's title. |
| `Dock` | `WindowDock` | Where the window sits. |
| `Faulted` | `bool` | `true` when your code threw an error and the window closed. |
| `CloseReason` | `WindowCloseReason` | Why the window closed. |
| `Body` | `RectTransform` | The main area. Put your controls here. |
| `Side` | `RectTransform` | The left column of a Full window. |
| `Footer` | `RectTransform` | A bar along the bottom of the window. |

| Event | Gives you | When it fires |
|---|---|---|
| `Closed` | Nothing, it carries no value. | The window has closed. |

</div>
</div>

## Things to know

- `Open` gives back the new window, or `null` when it could not open one. It gives `null` when a window with the same mod id and title is already open (that window is brought to the front instead), or when its set-up fails (the window is removed and the error is logged).
- An open window turns off the game's controls and shows the mouse cursor, and gives them back when it closes. If giving the controls back fails, the window releases the keys and the cursor itself, but only when nothing else is holding them off. <span class="tech">(It calls `GameController.ControlsDisabled(gameObject, ShowCursor: true)` and releases through `CoreInput.EnableControls`. It also hides the game menu if it is showing.)</span>
- Your build code runs once, inside `Run`. If it throws an error, the window closes with the reason `Fault`, `Faulted` becomes `true`, and the error is logged once.
- `Closed` fires before the window's model previews are cleaned up. Your mod can save its own work there.
- Escape closes only the top window, and not while a text field in it has focus.
- `Find` lets you close a window from a hotkey.
- A window closes with `SceneUnload` only when the scene it was opened in is unloaded. Other scenes that are unloaded at the same time do not close it. <span class="tech">(Unloads of other additive scenes are ignored.)</span>
- The window sizes are fixed. `Left` and `Right` are 440 pixels wide with 24 pixels of space around them, and `Right` starts 110 pixels from the top. `Center` is 640 pixels wide.
- These three panels block clicks across their whole area. `Full` covers the whole screen with a nearly solid dark backdrop that hides the game and blocks its clicks. <span class="tech">(The backdrop has an alpha of 0.97.)</span>
- `Full` also has a 440 pixel column on the left, `Side`, and `Body` to its right. The `Footer` is a 56 pixel bar along the bottom.
