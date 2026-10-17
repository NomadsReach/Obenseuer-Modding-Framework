---
title: ComputerApps
parent: Framework core classes
grand_parent: Framework basics
nav_order: 15
---

# ComputerApps

This page uses `GameObject`, `Sprite` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

The game has computer desks the player can sit at. `ComputerApps` lets your mod put an app on them, which is an icon on each desk's screen that opens your own interface. It can also open an app in a full-screen window, and give any object in the world a use prompt like the game's own ("Use", "Read" and so on). You draw the interface yourself, with the help of the [UI kit](../api/ui.md) page.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ComputerApps</code> <span>apps on the desk computers</span></p>

Call `Register` to put an app icon on every desk, and `AddInteraction` to give a world object a use prompt. `OpenStandalone` opens an app in a full-screen window.

<div class="api-pair" markdown="1">

```csharp
public static class ComputerApps
{
    public static bool IsAvailable { get; }
    public static event Action<ComputerAppClosedArgs> AppClosed;
    public static event Action<RectTransform> ScreenChanged;
    public static RectTransform ActiveScreen { get; }
    public static bool Register(string modId, string appId, string title, Sprite icon, Action<RectTransform> open);
    public static void Close(string appId);
    public static void RemoveAll(string modId);
    public static bool OpenStandalone(string modId, string appId);
    public static bool AddInteraction(GameObject target, string modId, string title, string message, Action onUse);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Register(modId, appId, title, icon, open)` | `bool` | Adds an app icon to every desk, the ones that exist now and the ones made later. |
| `Close(appId)` | `void` | Hides the app on every desk, and closes its full-screen window if it shows that app. |
| `RemoveAll(modId)` | `void` | Removes all of your mod's apps, icons, windows and use prompts. |
| `OpenStandalone(modId, appId)` | `bool` | Opens an app in a full-screen window over the game. |
| `AddInteraction(target, modId, title, message, onUse)` | `bool` | Gives an object a use prompt. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when apps can be used. See Things to know. |
| `ActiveScreen` | `RectTransform` | The screen of the computer the player is using now. `null` when none. |

| Event | Gives you | When it fires |
|---|---|---|
| `AppClosed` | `ComputerAppClosedArgs` | An app view closes. |
| `ScreenChanged` | `RectTransform` | The computer screen the player uses changes. It gives the new value. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ComputerAppClosedArgs</code> <span>what AppClosed tells you</span></p>

`AppClosed` hands you this when an app view closes. `ModId` and `AppId` tell you which mod and app it was.

<div class="api-pair" markdown="1">

```csharp
public sealed class ComputerAppClosedArgs
{
    public string ModId { get; }
    public string AppId { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ModId` | `string` | The mod that registered the app. |
| `AppId` | `string` | The id of the app. |

</div>
</div>

### Putting it together

Adds a Notes app, and a prompt on an object called Whiteboard that opens it in a full-screen window.

<div class="api-example" markdown="1">

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using UnityEngine;
using ObenseuerCore;

public class NotesApp : BaseUnityPlugin
{
    private void Awake()
    {
        ComputerApps.Register("example.notesapp", "notes", "Notes", null, host => BuildNotes(host));
    }

    private void Start()
    {
        GameObject board = GameObject.Find("Whiteboard");
        if (board != null) ComputerApps.AddInteraction(board, "example.notesapp", "Whiteboard", "Read", () => ComputerApps.OpenStandalone("example.notesapp", "notes"));
    }

    private void BuildNotes(RectTransform host)
    {
    }
}
```

| Step | What it does |
|---|---|
| `ComputerApps.Register(...)` | Adds the Notes icon to every desk. |
| `ComputerApps.AddInteraction(board, ...)` | Gives the Whiteboard a "Read" prompt. |
| `ComputerApps.OpenStandalone(...)` | Opens Notes in a full-screen window when the prompt is used. |

</div>

### Apps on the desks

- `IsAvailable` is `true` when the framework may change game methods and the feature group has not been turned off. The desk changes are made the first time `Register` is called, not at start-up. The feature group is `obenseuer.moddingframework.computerapps`, the name you will see in the log. <span class="tech">(`Core.PatchingAllowed` is true and the feature group has not been disabled.)</span>
- `Register` returns `false` when the desk changes cannot be made, or when `appId` is already used. Changes are refused when `Core.PatchingAllowed` is false. The first registration of an `appId` wins, and a warning is logged for the second. An empty `appId`, an `appId` that contains `/`, or an empty `title` also returns `false` and logs once.
- `Register` throws for a bad `modId` or `open`. A null or empty `modId` throws `ArgumentException`, and a null `open` throws `ArgumentNullException`.
- `title` must not be empty. `icon` may be `null`, and then the title is drawn on the tile.
- The icons form a grid at the top right of each desk. Each is 84 x 72, there are 5 rows, columns are 96 pixels apart, rows are 84 pixels apart, with a 12 pixel margin.
- An icon is created once for every desk and app, and reused after that.
- When the player clicks the icon, `open(host)` is called. `host` is a `RectTransform` that fills the desk's wallpaper, one for each desk and app, and it is an empty area you build your interface in.
- The framework clears `host` before every `open` call: each child is renamed, deactivated and destroyed. You always build into an empty area, the same as with `OpenStandalone`. Nothing you leave in the area survives the next click, so keep your own state in your mod.
- If `open` throws, the error is logged to your mod's log and the area is hidden.
- The area needs a click catcher above it, or its buttons receive no clicks. The desk logs one warning when none is found above the area. Each time an app is shown, the framework makes sure the scene has an event system, which the interface needs. <span class="tech">(The host must sit under a `GraphicRaycaster`. The event system is an EventSystem.)</span>
- Typing in an app does not make the player leave the computer. While an input field in an app has focus, the desk's own key handling is skipped.

### Closing and events

- Apps hide when the player leaves the computer. `Close(appId)` hides the app on every desk, and closes the full-screen window if it shows that app. `Close` always runs, also while a save loads or saves. Each close raises `AppClosed`.
- A desk view raises `AppClosed` when it was showing and then one of these happens: `Close(appId)`, the player leaving the computer (`HideAll`), a single scene load (not an additive one), `RemoveAll` for its mod, or an `open` that throws. A view that was already hidden raises nothing.
- The full-screen window raises `AppClosed` when it closes: its Close button, Esc, a single scene load, `RemoveAll` for its mod, or `OpenStandalone` of another app.
- `AppClosed` fires once for each view that closes. Its `ModId` is the mod that registered the app, and `AppId` is the app's id.
- A handler cannot tell a desk view from the full-screen window. The arguments carry only `ModId` and `AppId`. If you need to tell them apart, track your own view. Handlers are kept apart from each other, and you unsubscribe with `-=`.
- `ActiveScreen` is the screen of the computer the player is using now, or `null`. It is set when the player starts using a computer and cleared when the use stops or a single scene load clears it. `ScreenChanged` fires with the new value on each change, not on every frame. A `WebView` can follow it with `PinTo` (see [Web UI](../api/webui.md)). <span class="tech">(`ActiveScreen` is the desk's wallpaper `RectTransform`. It is set in `InteractableComputer.Use` and cleared in `StopUse`.)</span>

### The full-screen window

- `OpenStandalone` opens the app over the whole game. It draws on top of everything, has a Close button at the top right, locks the game's controls and shows the cursor. It changes no saved state, so it does not check whether the game is saving. <span class="tech">(A ScreenSpaceOverlay canvas at sort order 500, a CanvasScaler at 1920x1080 (match 0.5) and a GraphicRaycaster. The controls lock is `ControlsDisabled`.)</span>
- `OpenStandalone` returns `false` and logs a warning when the app is unknown, or when the game is not ready or is busy. Busy means a menu, shop, pause, dialogue or the console is open. It returns `true` when that app is already open.
- The window creates an event system if the scene has none, and removes it again. The last window to close destroys the event system, if the framework created it. <span class="tech">(It is an EventSystem with a StandaloneInputModule.)</span>
- The window closes on Esc, on its Close button, on a single scene load, and when the window throws. A scene added on top of the current one does not close it.
- Esc belongs to a text field or key row first. Esc does not close the window while an input field in it has focus or a key row is waiting for a key. That Esc belongs to the field or row, and the desk computer and the Pause Menu ignore it too. Otherwise Esc closes the window and does not open the Pause Menu on the same press. <span class="tech">(The close runs after the game's `Update`, in `LateUpdate`, while the controls lock still holds, so the game's `Update` returns before its Pause Menu check.)</span>

### Use prompts

- `AddInteraction` gives an object the game's own use prompt. The prompt shows `title` and `(message)` the way the game shows its own prompts, and `onUse` runs when the player uses it. An error in `onUse` is logged to your mod's log.
- The object needs a collider. `target` needs a `Collider`, and the game's `InteractObjects` must exist.
- Calling it again from the same mod updates the title, message and `onUse`.
- `AddInteraction` returns `false` while a save loads or saves, when changes are not allowed (`Core.PatchingAllowed` is false), when there is no collider or no interact system, when no usable layer is found, or when another mod already owns the object. It also returns `false` when the object already has a game component that handles use. The game would show its own prompt in that case, so your prompt would never appear. A warning is logged once. <span class="tech">(A "game component that handles use" is any MonoBehaviour that implements `IInteractable`, except the framework's own prompt.)</span>
- The object keeps its layer when that layer can be used. Otherwise it gets the first usable layer of 17, 23, 29, 20 and 15. `RemoveAll` puts the original layer back. <span class="tech">(A usable layer is one that is in `InteractObjects.MaskList`.)</span>
- `RemoveAll(modId)` removes your mod's apps and icons, closes its window, and removes its use prompts.

### Still to be checked in the game

- Icon placement and draw order on a real desk, and that clicks reach the icons. The desk click-catcher warning should not appear there.
- Clicks in the full-screen window, both with an event system in the scene and without one (the framework creates it).
- Whether the game's `MaskList` includes the chosen layers at runtime. The check is the same bit test that the raycast uses.
- That Esc, which is also the Pause Menu key, is used by the window first.
- Movement and cursor state after the window closes while the game UI is open.
- `AddInteraction` on an object that already has another `IInteractable` returns `false`.
