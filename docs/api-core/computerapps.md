---
title: ComputerApps
parent: Framework core classes
grand_parent: Framework basics
nav_order: 15
---

# ComputerApps

The game has computer desks the player can sit at. `ComputerApps` lets your mod put an app on them (an icon on each desk's screen that opens your own interface), open an app in a full-screen window, and give any object in the world a use prompt like the game's own ("Use", "Read" and so on). Everything you draw yourself, with the UI kit on the [UI kit](../api/ui.md) page.

```csharp
public static class ComputerApps
{
    public static bool IsAvailable { get; }                       // true when apps can be used (see below)
    public static event Action<ComputerAppClosedArgs> AppClosed;  // an app view closed
    public static event Action<RectTransform> ScreenChanged;      // the computer screen the player uses changed
    public static RectTransform ActiveScreen { get; }             // the screen of the computer the player is using now; null when none
    // Add an app icon to every desk.
    public static bool Register(string modId, string appId, string title, Sprite icon, Action<RectTransform> open);
    // Hide the app on every desk, and close its full-screen window.
    public static void Close(string appId);
    // Remove all of your mod's apps, icons, windows and use prompts.
    public static void RemoveAll(string modId);
    // Open an app in a full-screen window over the game.
    public static bool OpenStandalone(string modId, string appId);
    // Give an object a use prompt.
    public static bool AddInteraction(GameObject target, string modId, string title, string message, Action onUse);
}

public sealed class ComputerAppClosedArgs    // what AppClosed tells you
{
    public string ModId { get; }    // the mod that registered the app
    public string AppId { get; }    // the id of the app
}
```

- **`IsAvailable` is `true` when the framework may change game methods and the feature group has not been turned off.** The desk changes are made the first time `Register` is called, not at start-up. The feature group is `obenseuer.moddingframework.computerapps`, the name you will see in the log.
  - Technical: `Core.PatchingAllowed` is true and the feature group has not been disabled.

**Apps on the desks.**

- **`Register` adds an icon to every desk, the ones that exist now and the ones created later.** `title` must not be empty. `icon` may be `null`, and then the title is drawn on the tile.
- **`Register` returns `false` when the desk changes cannot be made, or when `appId` is already used.** Changes are refused when `Core.PatchingAllowed` is false. The first registration of an `appId` wins, and a warning is logged for the second. An empty `appId`, an `appId` that contains `/`, or an empty `title` also returns `false` and logs once.
- **`Register` throws for a bad `modId` or `open`.** A null or empty `modId` throws `ArgumentException`, and a null `open` throws `ArgumentNullException`.
- **The icons form a grid at the top right of each desk.** Each is 84 x 72, there are 5 rows, columns are 96 pixels apart, rows are 84 pixels apart, with a 12 pixel margin. An icon is created once for every desk and app, and reused after that.
- **When the player clicks the icon, `open(host)` is called, and `host` is an empty area you build your interface in.** `host` is a `RectTransform` that fills the desk's wallpaper, one for each desk and app. The framework clears it before every `open` call: each child is renamed, deactivated and destroyed. So you always build into an empty area, the same as `OpenStandalone`. Nothing you leave in the area survives the next click, so keep your own state in your mod.
- **If `open` throws, the error is logged to your mod's log and the area is hidden.**
- **The area needs a click catcher above it, or its buttons receive no clicks.** The desk logs one warning when none is found above the area. Each time an app is shown, the framework makes sure the scene has an event system, which the interface needs.
  - Technical: the host must sit under a `GraphicRaycaster`. The event system is an EventSystem.
- **Apps hide when the player leaves the computer.** `Close(appId)` hides the app on every desk, and closes the full-screen window if it shows that app. `Close` always runs, also while a save loads or saves. Each close raises `AppClosed`.
- **Typing in an app does not make the player leave the computer.** While an input field in an app has focus, the desk's own key handling is skipped.

**`AppClosed` and `ScreenChanged`.**

- **`AppClosed` fires once for each view that closes.** Its `ModId` is the mod that registered the app, and `AppId` is the app's id. A desk view raises it when it was showing and then one of these happens: `Close(appId)`, the player leaving the computer (`HideAll`), a single scene load (not an additive one), `RemoveAll` for its mod, or an `open` that throws. A view that was already hidden raises nothing.
- **The full-screen window raises it when it closes.** That is its Close button, Esc, a single scene load, `RemoveAll` for its mod, or `OpenStandalone` of another app.
- **A handler cannot tell a desk view from the full-screen window.** The arguments carry only `ModId` and `AppId`. If you need to tell them apart, track your own view. Handlers are kept apart from each other, and you unsubscribe with `-=`.
- **`ActiveScreen` is the screen of the computer the player is using now, or `null`.** It is set when the player starts using a computer and cleared when the use stops or a single scene load clears it. `ScreenChanged` fires with the new value on each change, not on every frame. A `WebView` can follow it with `PinTo` (see [Web UI](../api/webui.md)).
  - Technical: `ActiveScreen` is the desk's wallpaper `RectTransform`. It is set in `InteractableComputer.Use` and cleared in `StopUse`.

**The full-screen window.**

- **`OpenStandalone` opens the app over the whole game.** It draws on top of everything, has a Close button at the top right, locks the game's controls and shows the cursor. It changes no saved state, so it does not check whether the game is saving.
  - Technical: a ScreenSpaceOverlay canvas at sort order 500, a CanvasScaler at 1920x1080 (match 0.5) and a GraphicRaycaster. The controls lock is `ControlsDisabled`.
- **The window creates an event system if the scene has none, and removes it again.** The last window to close destroys the event system, if the framework created it.
  - Technical: it is an EventSystem with a StandaloneInputModule.
- **`OpenStandalone` returns `false` and logs a warning when the app is unknown, or when the game is not ready or is busy.** Busy means a menu, shop, pause, dialogue or the console is open. It returns `true` when that app is already open.
- **The window closes on Esc, on its Close button, on a single scene load, and when the window throws.** A scene added on top of the current one does not close it.
- **Esc belongs to a text field or key row first.** Esc does not close the window while an input field in it has focus or a key row is waiting for a key. That Esc belongs to the field or row, and the desk computer and the Pause Menu ignore it too. Otherwise Esc closes the window and does not open the Pause Menu on the same press.
  - Technical: the close runs after the game's `Update`, in `LateUpdate`, while the controls lock still holds, so the game's `Update` returns before its Pause Menu check.

**Use prompts on objects.**

- **`AddInteraction` gives an object the game's own use prompt.** The prompt shows `title` and `(message)` the way the game shows its own prompts, and `onUse` runs when the player uses it. An error in `onUse` is logged to your mod's log.
- **The object needs a collider.** `target` needs a `Collider`, and the game's `InteractObjects` must exist.
- **Calling it again from the same mod updates the title, message and `onUse`.**
- **`AddInteraction` returns `false` in these cases.** It returns `false` while a save loads or saves, when changes are not allowed (`Core.PatchingAllowed`), when there is no collider or no interact system, when no usable layer is found, or when another mod already owns the object. It also returns `false` when the object already has a game component that handles use. The game would show its own prompt in that case, so your prompt would never appear. A warning is logged once.
  - Technical: "a game component that handles use" is any MonoBehaviour that implements `IInteractable`, except the framework's own prompt.
- **The object keeps its layer when that layer can be used.** Otherwise it gets the first usable layer of 17, 23, 29, 20 and 15. `RemoveAll` puts the original layer back.
  - Technical: a usable layer is one that is in `InteractObjects.MaskList`.
- **`RemoveAll(modId)` removes your mod's apps and icons, closes its window, and removes its use prompts.**

**Still to be checked in the game.**

- Icon placement and draw order on a real desk, and that clicks reach the icons. The desk click-catcher warning should not appear there.
- Clicks in the full-screen window, both with an event system in the scene and without one (the framework creates it).
- Whether the game's `MaskList` includes the chosen layers at runtime. The check is the same bit test that the raycast uses.
- That Esc, which is also the Pause Menu key, is used by the window first.
- Movement and cursor state after the window closes while the game UI is open.
- `AddInteraction` on an object that already has another `IInteractable` returns `false`.

This plugin adds a Notes app, and a prompt on an object called Whiteboard that opens it in a full-screen window.

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
