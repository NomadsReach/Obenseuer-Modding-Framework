---
title: UI and HTML views
parent: Guides
nav_order: 4
---

# Building UI

This guide covers the four UI routes a framework mod can use: an HTML panel on WebView2, a settings page in the Mods menu, an app on the in-game computer, and HUD and screen effects. The last section builds a small info panel. It is an HTML window that shows the player's balance and a press count, and it sends each button press back to the mod.

You need:

- Obenseuer 0.5.08, BepInEx 5.4.23 or later (Mono) or BepInEx 6 Unity.Mono, and the Obenseuer Modding Framework 0.11.0. Depend on it with `[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]`. This guide uses WebUi, so also depend on the optional WebUi module with `[BepInDependency("obenseuer.moddingframework.webui", ">=0.11.0")]`. On BepInEx 5, write `"0.11.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](getting-started/packaging.md#supporting-bepinex-5-too).
- C# 5 is the version the framework is built with. The Starter project uses the latest C# version.
- For HTML panels, the WebView2 Runtime. Windows 10 and 11 have it already. If it is missing, `WebUi.Open` returns `null` and `WebUi.IsAvailable` becomes `false`.

Besides `mscorlib.dll`, `netstandard.dll`, `System.dll`, `System.Core.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `BepInEx.Core.dll`, `BepInEx.Unity.Mono.dll`, `ObenseuerCore.dll` and `ObenseuerCore.WebUi.dll`, these references matter. The names are BepInEx 6 names. For BepInEx 5, see [Supporting BepInEx 5 too](getting-started/packaging.md#supporting-bepinex-5-too):

- `UnityEngine.InputLegacyModule.dll` is needed only for `Input.GetKeyDown`.
- The `ModUi` snippets need `UnityEngine.UI.dll` and `Unity.TextMeshPro.dll`.
- `UnityEngine.UIModule.dll` is not needed.

The snippets assume these usings, and each one needs the ones for the names it uses: `System.IO`, `System.Reflection`, `BepInEx`, `BepInEx.Configuration`, `BepInEx.Unity.Mono`, `BepInEx.Unity.Mono.Configuration`, `ObenseuerCore`, `ObenseuerCore.Ui` and `UnityEngine`. `KeyboardShortcut` needs `BepInEx.Unity.Mono.Configuration`. On BepInEx 5, leave out the two `BepInEx.Unity.Mono` usings, or wrap them in `#if BEPINEX6` and `#endif`, as in [Supporting BepInEx 5 too](getting-started/packaging.md#supporting-bepinex-5-too).

## Pick a route

| You want | Use | Section |
|---|---|---|
| A panel with HTML, CSS and JavaScript | `WebUi.Open` and `WebView` | 1 |
| Settings the player edits in the Mods menu | BepInEx config entries, or `ModMenu.AddPage` for a custom page | 2 |
| An app on the in-game computer | `ComputerApps.Register` | 3 |
| Notices, hints, subtitles, shake and fades | `Hud` and `ScreenFx` | 4 |

## 1. HTML panels with WebUi

A view is a WebView2 window that loads a page from your mod's `web` folder. The framework maps that folder to the host `<modid>.obx`, for example `example-infopanel.obx`. The page is served from there and from nowhere else, apart from the framework's shared assets.

### Step 1: ship the web folder

Put the page next to your DLL, and copy the folder into the build output with a `CopyWeb` target in your `.csproj`:

```xml
<ItemGroup>
  <WebFiles Include="web\**\*" />
</ItemGroup>

<Target Name="CopyWeb" AfterTargets="Build">
  <RemoveDir Directories="$(OutDir)web" />
  <Copy SourceFiles="@(WebFiles)" DestinationFolder="$(OutDir)web\%(RecursiveDir)" />
</Target>
```

The build then gives this layout:

```
bin\InfoPanel.dll
bin\web\index.html
bin\web\panel.css
bin\web\app.js
```

To install, copy `bin\InfoPanel.dll` and `bin\web\` into `BepInEx\plugins\InfoPanel\`, so the `web` folder sits next to the DLL.

Pass the page as a relative path. `WebUi.Open` returns `null` for a path that starts with `/` or `\`, or that contains `:`, `\`, `%` or `..`.

### Step 2: open the view

```csharp
string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "web");
view = WebUi.Open(ModId, folder, "index.html", true);
if (view == null)
{
    Logger.LogWarning("the view did not open: " + WebUi.LastError);
    return;
}
view.Message += OnMessage;
```

`ModId` must be 1 to 64 characters of letters, digits, `.`, `_` or `-`. A bad id throws `ArgumentException`. The last argument is `modal`. A modal view locks the player's controls and shows the cursor. Pass `false` for a window that should open anyway. Call `Open` on Unity's main thread.

`Open` returns `null` when:

- `modal` is true and the game is busy. That covers a menu, shop or dialogue open, the developer console open, the pause menu active, the main menu showing, or the player's controls already disabled by another modal view or by computer use.
- `folder` does not exist, or `page` is not a relative path.
- The game window is not active. The log says the window opens once the game has focus.
- The WebView2 shim is missing or failed to start. See the fallback below.

A non-modal view skips the busy check.

### Step 3: link the shared theme

The framework serves two shared files at `https://obx-core.obx/`. Link them from your page:

```html
<link rel="stylesheet" href="https://obx-core.obx/obx-theme.css">
<link rel="stylesheet" href="https://obx-core.obx/obx-base.css">
```

`obx-theme.css` defines the colour, spacing, type and motion tokens, and these component classes: `.obx-brand`, `.obx-btn` (with `primary`, `danger`, `ghost`, `sm` or `block`), `.obx-card`, `.obx-label`, `.obx-value`, `.obx-table`, `.obx-input`, `.obx-tabs`, `.obx-tab`, `.obx-badge` and `.obx-seg`. `obx-base.css` is optional. It sets `box-sizing`, a transparent full-height body with no scrolling and no text selection, and the `[hidden]` rule.

The tokens you will use most, with their light values:

| Token | Light value | Use |
|---|---|---|
| `--obx-surface-0` | `#e6e6e6` | panel background |
| `--obx-surface-1` | `#f2f2f2` | hover and inset areas |
| `--obx-text` | `#333333` | body text |
| `--obx-muted` | `#646464` | labels |
| `--obx-frame` | `#8c8c8c` | panel border |
| `--obx-amber` | `#f0a830` | `.obx-btn.primary` |
| `--obx-red` | `#c8281e` | `.obx-btn.danger` |
| `--obx-s1` to `--obx-s4` | 4, 8, 16, 24 px | spacing |
| `--obx-r-sm`, `--obx-r-md` | 6, 8 px | corner radius |
| `--obx-fs-sm` to `--obx-fs-xl` | 13, 14, 18, 30 px | font size |
| `--obx-font` | Segoe UI, Bahnschrift, system-ui | font family |

Dark values apply under `:root[data-theme="dark"]`. The framework sets `data-theme` on `<html>` before your page runs, from the `Appearance.WebTheme` setting (`light`, `dark` or `system`). When the setting changes, the framework posts `{"v":1,"type":"theme","mode":"dark"}` to every open view. Handle it in the page, as Step 5 shows.

### Step 4: JavaScript to C#

The page sends a JSON object with the WebView2 `chrome.webview` object. The framework injects no helper, so call it directly:

```js
const host = chrome.webview;
document.getElementById("press").addEventListener("click", () => {
  host.postMessage({ type: "press" });
});
```

C# receives the raw JSON text in `view.Message`:

```csharp
private void OnMessage(string json)
{
    if (json.Contains("\"type\":\"press\"")) presses++;
}
```

The rules for `Message`:

- The handler gets the text, not parsed fields. Parse it yourself, and treat it as untrusted input. `UnityEngine.JsonUtility.FromJson<T>` reads it into a `[Serializable]` class, and needs a reference to `UnityEngine.JSONSerializeModule.dll`. The `json.Contains(...)` check in the example is fine for a demo, not for real data.
- A message longer than 16384 characters is dropped.
- A message whose `type` starts with `obx.` is dropped. Those names belong to the shim.
- A message with no `type`, or one that is not a JSON object, reaches the handler as raw text.

### Step 5: C# to JavaScript

`view.Post` sends a JSON object to the page. Build the text yourself:

```csharp
view.Post("{\"type\":\"state\",\"balance\":" + Wallet.Balance + ",\"presses\":" + presses + "}");
```

The page reads it from the `message` event. `e.data` is already a parsed object:

```js
host.addEventListener("message", (e) => {
  const m = e.data;
  if (m.type === "theme") document.documentElement.setAttribute("data-theme", m.mode);
  if (m.type === "state") {
    document.getElementById("balance").textContent = String(m.balance);
    document.getElementById("presses").textContent = String(m.presses);
  }
});
```

`Post` returns `false` until the page's web view is attached, so do not post right after `Open`. Let the page ask first. The page sends `{"type":"hello"}` when it loads, and the mod answers in `OnMessage`. The complete example does this.

### Step 6: extra folders with MapFolder

`view.MapFolder(name, folder)` maps a second folder to `<name>.<modhost>`. For the info panel, `view.MapFolder("frames", ...)` gives `https://frames.example-infopanel.obx/`. Use it for images and other assets:

```csharp
view.MapFolder("frames", Path.Combine(folder, "frames"));
```

The extra host can serve files, but its pages cannot post messages to the view. A Content-Security-Policy on your page also has to name that host, because `'self'` covers only the view's own host.

### Step 7: hide, close and Escape

- `view.Show()` and `view.Hide()` toggle the view. `view.Close()` destroys it. A closed view stays closed, and `Open` makes a new one.
- Escape hides a visible view when `view.EscapeHides` is true, which is the default. With no `KeyPressed` handler, that is any visible view. With a handler, the handler runs first, and the framework then hides only a modal view. Set `view.EscapeHides = false` to handle Escape yourself, then call `Hide` or `Close`.
- `view.KeyPressed` is a public `Action<int>` event. The framework raises it with the virtual-key code of Escape (27) and F10 (121) pressed in the focused view. No other key reaches it, so F9 does not while the panel has focus.
- A scene load hides modal views. That is a plain `Hide`, so `Dismissed` does not run.
- `view.Dismissed` runs after the framework hides the view on Escape. It does not run when your mod calls `Hide`.
- A modal view that is not ready within 20 seconds of `Show` closes itself, and the log says so.

### Security model

- Only the view's own page can post messages. A page loaded from a `MapFolder` host cannot.
- The framework answers requests to unmapped hosts with `403`. Your page's CSP is the second layer, so always set one.
- Navigation to other hosts, new windows, downloads, permission prompts and external URI schemes are blocked. Each block is reported with a `*-blocked` name, and the view stays open. The report goes to the log and to `WebUi.LastError`. The mod's `Message` handler never sees it.
- Dev tools and the right-click menu are off. Host objects are not exposed to the page.
- Service workers are hidden from the page.
- If the page posts a bare string, C# receives it JSON-quoted, so `"hi"` arrives with its quotes.
- The framework adds no Content-Security-Policy to your page. The Mods menu page sets its own. Set one on your page too:

```html
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; script-src 'self'; style-src 'self' https://obx-core.obx; img-src 'self' data:; connect-src 'none'; object-src 'none'; base-uri 'none'; form-action 'none'">
```

With this policy, keep scripts and styles in files. `style-src` must name `https://obx-core.obx` when you link the shared CSS.

### Fallback when WebView2 is missing

`WebUi.IsAvailable` becomes `false` when the shim is missing or the WebView2 start fails. `WebUi.LastError` says why. Check both after `Open` returns `null`, and build the same panel as a classic window with `ModWindow` and `ModUi`:

```csharp
if (view == null && !WebUi.IsAvailable)
{
    ModWindow.Open(ModId, "Info panel", WindowDock.Center, w =>
    {
        ModUi.Title(w.Body, "Info panel");
        ModUi.Text(w.Body, "Presses: " + presses, true);
        ModUi.Button(w.Body, "Press", ButtonStyle.Primary, () => presses++);
    });
    return;
}
```

Before the first `Open`, `IsAvailable` is still `true` if the shim file is present. Only the `Open` call shows whether the runtime is installed.

### Common mistakes

- Calling any WebUi member off Unity's main thread. `Open` returns `null`, the `bool` members return `false`, and `Close` does nothing.
- Passing a page path such as `/index.html` or `../other.html`. `Open` returns `null`.
- A missing `web` folder next to the DLL. `Open` returns `null`.
- Posting right after `Open`. Wait for the page's first message.
- Trusting a message. Parse it, check the type, and clamp numbers.
- Using a message type that starts with `obx.`. It is dropped.
- A Content-Security-Policy without `https://obx-core.obx` in `style-src`. The shared CSS does not load.
- Opening a modal view while a menu is open. `Open` returns `null`.

## 2. Settings pages in the Mods menu

The Mods menu opens from the Mods tab in the pause menu, and from the MODS button on the main menu. Every loaded plugin that has visible config entries gets a settings page there. Custom pages from `ModMenu.AddPage` are listed next to them.

### Step 1: bind config entries

Use BepInEx config. The Mods menu builds a row for each entry:

```csharp
private ConfigEntry<bool> announce;
private ConfigEntry<float> volume;
private ConfigEntry<KeyboardShortcut> openKey;

private void Awake()
{
    announce = Config.Bind("General", "Announce", true, "Show a notice when the panel opens");
    volume = Config.Bind("General", "Volume", 0.5f, new ConfigDescription("Alert volume", new AcceptableValueRange<float>(0f, 1f)));
    openKey = Config.Bind("Keys", "Open", new KeyboardShortcut(KeyCode.F9), "Key that opens the panel");
}
```

The row control depends on the entry type:

| Entry type | Row |
|---|---|
| `bool` | toggle |
| enum, or a value list (`AcceptableValueList`) | stepper that cycles the values |
| `KeyboardShortcut` | key binding: click it, then press a key |
| `int` or `float` with an `AcceptableValueRange` | slider |
| `string`, or a type that converts from text | text field |
| any other type | read-only |

Most rows save at once. Sliders save 0.25 seconds after the last change, and again when the game quits.

### Step 2: add a custom page

Use `ModMenu.AddPage` when the page needs more than config rows. The build callback gets the page's content area, and you build the controls with `ModUi`:

```csharp
ModMenu.AddPage("example.settingsdemo", "Settings demo", content =>
{
    RectTransform stack = ModUi.Stack(content);
    ModUi.Title(stack, "Settings demo");
    ModUi.Toggle(stack, "Announce", announce.Value, v => announce.Value = v);
    ModUi.Slider(stack, "Volume", 0f, 1f, volume.Value, v => volume.Value = v, 2);
});
```

- `AddPage` adds a sidebar entry. It never adds a game tab. A page with the same title as another mod's page is still added, and one warning names both mods. Call it once: a second call from the same mod adds a second page.
- The sidebar is read when the Mods window opens. A page added while the window is open shows the next time it opens.
- The build runs when the page is selected. If it throws, the page shows a notice and the error goes to the log. The other pages still work.
- When the Mods menu uses its web page, a custom page opens as a classic pane inside it.
- `ModMenuWidgets` is obsolete. Build new pages with `ModUi`.

### Common mistakes

- Expecting the Mods menu to change the game's own options. It edits your config. The game's display and quality options are read only (see [settings](../api/settings.md)).
- A `ModUi` callback that throws closes the Mods window with reason `Fault`. Keep callbacks short, and do the work in your mod.
- Forgetting that a plugin with no visible config entries gets no Settings page.

## 3. Apps on the in-game computer

Registered apps appear as icons on the computer desks. A click calls your `open` callback with an empty host.

### Step 1: register the app

```csharp
ComputerApps.Register("example.notesapp", "notes", "Notes", null, host => Build(host));
```

- `appId` must not be empty and must not contain `/`. Registering a second app with the same id returns `false` and logs a warning that names both mods. The first registration wins.
- `Register` returns `false` when `ComputerApps.IsAvailable` is `false`, because the desk patches are then not installed.
- `icon` may be `null`. The title is then drawn on the tile.
- `open` receives the host. It may throw; the exception is logged to your mod's log and the host is hidden.

### Step 2: build into the host

```csharp
private void Build(RectTransform host)
{
    RectTransform stack = ModUi.Stack(host);
    ModUi.Title(stack, "Notes");
    ModUi.Text(stack, note, false);
    ModUi.Button(stack, "Reset", ButtonStyle.Secondary, () => note = "Buy bread.");
}
```

The framework clears the host before each `open` call. Keep your state in the mod, not in the host's children.

### Step 3: an HTML app on the monitor

A `WebView` can follow the monitor the player is using. Open it the first time a monitor is used, then pin it each time the player starts using a computer, and hide it when they stop:

```csharp
string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "web");
WebView view = null;
ComputerApps.ScreenChanged += s =>
{
    if (s == null)
    {
        if (view != null) view.Hide();
        return;
    }
    if (view == null) view = WebUi.Open(ModId, folder, "index.html", false);
    if (view == null) return;
    view.PinTo(s);
    view.Show();
};
```

`ComputerApps.ScreenChanged` is raised with the monitor's `RectTransform`, or `null` when the player stops. `ComputerApps.ActiveScreen` holds the current value. The view is not opened at start, because `Open` returns `null` until the game window has focus. A refused `Open` is tried again on the next use.

### Step 4: full-screen window and world prompts

```csharp
ComputerApps.AddInteraction(board, "example.notesapp", "Whiteboard", "Read", () => ComputerApps.OpenStandalone("example.notesapp", "notes"));
```

- `AddInteraction` gives a world object the game's use prompt. The object needs a `Collider`. It returns `false` when the object already has a game interactable, and a warning is logged once.
- `OpenStandalone` opens the app full screen over the game. It returns `false` when the game is busy.
- `ComputerApps.Close(appId)` hides the app. `ComputerApps.AppClosed` runs when a view closes.

### Common mistakes

- A host with no `GraphicRaycaster` above it gets no clicks. The desk host has one. Add one to any canvas you create yourself.
- Keeping UI objects in the host. The framework destroys them on the next open.
- An `appId` with `/` makes `Register` return `false` and logs a warning.
- A second `Register` call with the same `appId` returns `false`.

## 4. HUD and screen effects

```csharp
Hud.Notify("Example notice");
Hud.ShowSubtitle("Example subtitle", 2f);
Hud.ShowHint("example.hudnotes", "Tip", new[] { "Example hint line." });
Hud.HideHint("example.hudnotes");
ScreenFx.Shake(0.4f, 0.02f);
ScreenFx.FadeToBlack(0.1f);
ScreenFx.FadeIn(0.1f);
```

- `Hud.Notify(text)` shows the game's notice box with the type line `Notice`. `Hud.Notify(title, body)` shows the same box with your own title line.
- `Hud.ShowSubtitle(text, fadeSeconds)` types the text at 20 characters per second, then fades it out over `fadeSeconds`.
- `Hud.ShowHint(modId, title, lines)` shows one hint per mod id. Each line is a paragraph. `Hud.HideHint(modId)` removes it.
- `Hud.ShowNote(noteId)` opens a note the player already has. It cannot show text of your own.
- `ScreenFx.Shake(duration, amount)` shakes the camera. A new shake replaces the one in progress.
- `ScreenFx.FadeToBlack(speed)` leaves the screen black until `ScreenFx.FadeIn` runs. Call both.
- `ScreenFx.SetDeathMessage(text)` sets the reason text. Call it just before the death screen, because the game replaces the text when it sets its own.
- Field of view is not wrapped. On cameras that have the game's `SetCameraFOV` component enabled, it resets the field of view to the saved value every frame, so a direct write is undone on the next frame.

### Common mistakes

- The `Hud` and `ScreenFx` calls return `false` on the main menu or while loading, when the game object is missing. Check the result if the message matters.
- `Hud.Notify` and `Hud.ShowSubtitle` throw `ArgumentException` for empty text, and `Hud.ShowHint` throws for an empty title.
- A negative `fadeSeconds` throws `ArgumentException`.
- `ScreenFx.Shake` throws `ArgumentException` for a duration of 0 or less, or a negative amount.
- `ScreenFx.FadeToBlack` and `ScreenFx.FadeIn` throw `ArgumentException` for a speed of 0 or less.
- `Hud.ShowHint` and `Hud.HideHint` throw `ArgumentException` for a mod id that is not 1 to 64 characters of letters, digits, `.`, `_` or `-`.
- `FadeToBlack` without a later `FadeIn` leaves the player's screen black.

## Complete example: info panel

The info panel opens with F9. It shows the player's balance and a press count. The page asks for the state when it loads, and each button press goes back to the mod, which updates the count and sends the state again.

Files:

```
InfoPanel\InfoPanel.cs
InfoPanel\web\index.html
InfoPanel\web\panel.css
InfoPanel\web\app.js
```

`InfoPanel.cs`:

```csharp
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.infopanel", "InfoPanel", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
[BepInDependency("obenseuer.moddingframework.webui", ">=0.11.0")]
public class InfoPanel : BaseUnityPlugin
{
    private const string ModId = "example.infopanel";
    private WebView view;
    private int presses;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F9)) return;
        if (view != null && !view.IsClosed)
        {
            if (view.IsVisible) view.Hide();
            else view.Show();
            return;
        }
        string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "web");
        view = WebUi.Open(ModId, folder, "index.html", true);
        if (view == null)
        {
            Logger.LogWarning("InfoPanel: the view did not open: " + WebUi.LastError);
            return;
        }
        view.Message += OnMessage;
    }

    private void OnMessage(string json)
    {
        if (json.Contains("\"type\":\"hello\"")) SendState();
        else if (json.Contains("\"type\":\"press\""))
        {
            presses++;
            SendState();
        }
    }

    private void SendState()
    {
        view.Post("{\"type\":\"state\",\"balance\":" + Wallet.Balance + ",\"presses\":" + presses + "}");
    }
}
```

`web\index.html`:

```html
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; script-src 'self'; style-src 'self' https://obx-core.obx; img-src 'self' data:; connect-src 'none'; object-src 'none'; base-uri 'none'; form-action 'none'">
<title>Info panel</title>
<link rel="stylesheet" href="https://obx-core.obx/obx-theme.css">
<link rel="stylesheet" href="https://obx-core.obx/obx-base.css">
<link rel="stylesheet" href="panel.css">
</head>
<body>
<main class="panel">
  <div class="obx-brand"><span class="obx-brand-a">Info</span><span class="obx-brand-b">Panel</span></div>
  <div class="row"><span class="obx-label">Balance</span><span class="obx-value" id="balance">-</span></div>
  <div class="row"><span class="obx-label">Presses</span><span class="obx-value" id="presses">0</span></div>
  <button class="obx-btn primary block" id="press" type="button">Press me</button>
</main>
<script src="app.js"></script>
</body>
</html>
```

`web\panel.css`:

```css
body { display: flex; align-items: center; justify-content: center; }
.panel {
  display: flex;
  flex-direction: column;
  gap: var(--obx-s2);
  min-width: 300px;
  padding: var(--obx-s3);
  background: var(--obx-surface-0);
  border: 1px solid var(--obx-frame);
  border-radius: var(--obx-r-md);
}
.row { display: flex; justify-content: space-between; align-items: baseline; }
```

`web\app.js`:

```js
const host = window.chrome.webview;
const balance = document.getElementById("balance");
const presses = document.getElementById("presses");

document.getElementById("press").addEventListener("click", () => {
  host.postMessage({ type: "press" });
});

host.addEventListener("message", (e) => {
  const m = e.data;
  if (m.type === "theme") document.documentElement.setAttribute("data-theme", m.mode);
  if (m.type === "state") {
    balance.textContent = String(m.balance);
    presses.textContent = String(m.presses);
  }
});

host.postMessage({ type: "hello" });
```

Build it with the project from [Your first mod](getting-started/first-mod.md), with two more `Reference` items: `ObenseuerCore.WebUi.dll` from the game's `BepInEx\plugins\ObenseuerCore.WebUi\` folder (`ObenseuerCore.WebUi.BepInEx5.dll` on BepInEx 5), and `UnityEngine.InputLegacyModule.dll` for `Input.GetKeyDown`. The `CopyWeb` target from step 1 copies `web\` to `bin\web\`. Run the game, press F9 to open the panel, press the button, and watch the count change. Press Escape to hide it, then F9 to show it again.

## Related pages

- [WebUi](../api/webui.md): the full WebUi and WebView reference, including pinning, navigation and the shim's messages.
- [UI kit](../api/ui.md): the `ModUi` and `ModWindow` kit used by the fallback and the settings pages.
- [API reference](../api-core.md), sections `ModMenu` and `ComputerApps`: the Mods menu and computer app references.
- [HUD](../api/hud.md): `Hud`, `ConsoleCommands` and the other HUD facades.
- [ScreenFx](../api/screen-fx.md): `ScreenFx` in full, including the fade and FOV notes.
- [Settings](../api/settings.md): the game's own settings and `Keybinds`, which register mod keys in the Mods menu.
