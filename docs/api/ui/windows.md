---
title: Windows
parent: UI
grand_parent: Screen and UI
nav_order: 3
---

# Windows

A window is a screen the player sees over the game, with its own areas to fill. You open one with `ModWindow.Open` and give it your code to fill it. The controls can also be built in other places, not only in a window (see the next section).

```csharp
public enum WindowDock
{
    Left,     // a panel at the left of the screen
    Right,    // a panel at the right of the screen
    Center,   // a panel in the middle of the screen
    Full      // the whole screen
}

public enum WindowCloseReason
{
    Requested,    // your mod, or the player, closed it
    Escape,       // the player pressed Escape
    SaveLoad,     // a save was loaded
    SceneUnload,  // the scene the window was opened in was unloaded
    Destroyed,    // the window's object was destroyed
    Fault         // your code threw an error
}

public sealed class ModWindow : MonoBehaviour
{
    public static ModWindow Open(string modId, string title, WindowDock dock, Action<ModWindow> build);   // opens a window; your build code fills it
    public static ModWindow Find(string modId, string title);   // the open window with this mod id and title; null if none
    public void Close();                                        // closes the window
    public void Run(Action action);                             // runs your code safely inside the window
    public string ModId { get; }                // the mod id the window was opened with
    public string Title { get; }                // the window's title
    public WindowDock Dock { get; }             // where the window sits
    public bool Faulted { get; }                // true when your code threw an error and the window closed
    public WindowCloseReason CloseReason { get; }   // why the window closed
    public RectTransform Body { get; }          // the main area; put your controls here
    public RectTransform Side { get; }          // the left column of a Full window
    public RectTransform Footer { get; }        // a bar along the bottom of the window
    public event Action Closed;                 // the window has closed
}
```

- **`Open` gives back the new window, or `null` when it could not open one.** It gives `null` when a window with the same mod id and title is already open (that window is brought to the front instead), or when its set-up fails (the window is removed and the error is logged).
- **An open window turns off the game's controls and shows the mouse cursor, and gives them back when it closes.** If giving the controls back fails, the window releases the keys and the cursor itself, but only when nothing else is holding them off.
  - Technical: it calls `GameController.ControlsDisabled(gameObject, ShowCursor: true)` and releases through `CoreInput.EnableControls`. It also hides the game menu if it is showing.
- **Your build code runs once, inside `Run`.** If it throws an error, the window closes with the reason `Fault`, `Faulted` becomes `true`, and the error is logged once.
- **`Closed` fires before the window's model previews are cleaned up.** Your mod can save its own work there.
- **Escape closes only the top window, and not while a text field in it has focus.**
- **`Find` lets you close a window from a hotkey.** It gives the open window with that mod id and title, or `null`.
- **A window closes with `SceneUnload` only when the scene it was opened in is unloaded.** Other scenes that are unloaded at the same time do not close it.
  - Technical: unloads of other additive scenes are ignored.
- **The window sizes are fixed.** `Left` and `Right` are 440 pixels wide with 24 pixels of space around them, and `Right` starts 110 pixels from the top. `Center` is 640 pixels wide. These three panels block clicks across their whole area. `Full` covers the whole screen with a nearly solid dark backdrop that hides the game and blocks its clicks. `Full` also has a 440 pixel column on the left, `Side`, and `Body` to its right. The `Footer` is a 56 pixel bar along the bottom.
  - Technical: the backdrop has an alpha of 0.97.
