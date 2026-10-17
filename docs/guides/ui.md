---
title: UI and HTML views
parent: Guides
nav_order: 4
---

# Building UI

This guide covers the four ways a mod can put something on the player's screen: an HTML panel in its own window, a settings page in the Mods menu, an app on the in-game computer, and notices and screen effects. The last section builds a small info panel. It is an HTML window that shows the player's balance and a press count, and it sends each button press back to the mod.

You can read the four routes in any order. Each one starts from the project in the next section.

## What you need

- Obenseuer 0.5.08 with the framework installed (see the [install page](../install.md)), and BepInEx 5.4.23 or later (Mono) or BepInEx 6 Unity.Mono.
- The .NET 10 SDK, to build your mod with `dotnet build`.
- For HTML panels, the WebView2 Runtime. This is the component that shows web pages inside a program. Windows 10 and 11 have it already. If it is missing, `WebUi.Open` returns `null` and `WebUi.IsAvailable` becomes `false`.
- For HTML panels, the framework's optional WebUi module, which is installed with the framework (see [install](../install.md#optional-webui)).

## Set up the project

The mods in this guide are BepInEx plugins, because they use `Update` and BepInEx's settings. A framework mod, like the one in [Your first mod](getting-started/first-mod.md), does not have these.

1. Do the steps in [Your first mod](getting-started/first-mod.md) up to the point where it builds. That gives you a folder with `MyFirstMod.csproj`, `Plugin.cs` and `Directory.Build.props`.
2. Open the `.csproj`. In the `<ItemGroup>` that holds the other `<Reference>` lines, add these. They give your code the BepInEx plugin base class, the framework's WebUi module, the game's classic UI classes (used by the `ModUi` snippets) and Unity's keyboard input (used by `Input.GetKeyDown`):

```xml
<Reference Include="BepInEx.Core">
  <HintPath>$(BepInExCore)\BepInEx.Core.dll</HintPath>
  <Private>false</Private>
</Reference>
<Reference Include="BepInEx.Unity.Mono">
  <HintPath>$(BepInExCore)\BepInEx.Unity.Mono.dll</HintPath>
  <Private>false</Private>
</Reference>
<Reference Include="ObenseuerCore.WebUi">
  <HintPath>$(GameDir)\BepInEx\plugins\ObenseuerCore.WebUi\ObenseuerCore.WebUi.dll</HintPath>
  <Private>false</Private>
</Reference>
<Reference Include="UnityEngine.InputLegacyModule">
  <HintPath>$(ManagedDir)\UnityEngine.InputLegacyModule.dll</HintPath>
  <Private>false</Private>
</Reference>
<Reference Include="UnityEngine.UI">
  <HintPath>$(ManagedDir)\UnityEngine.UI.dll</HintPath>
  <Private>false</Private>
</Reference>
<Reference Include="Unity.TextMeshPro">
  <HintPath>$(ManagedDir)\Unity.TextMeshPro.dll</HintPath>
  <Private>false</Private>
</Reference>
```

3. Your plugin needs the framework and, for HTML panels, WebUi to start first. Put these two lines above your plugin class:

```csharp
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
[BepInDependency("obenseuer.moddingframework.webui", ">=0.11.0")]
```

- **On BepInEx 5, change two things.** Write `"0.11.0"` instead of `">=0.11.0"`, because BepInEx 5 cannot read a version range, and remove `using BepInEx.Unity.Mono;`, because `BaseUnityPlugin` then comes from `BepInEx`. The WebUi file is `ObenseuerCore.WebUi.BepInEx5.dll` on BepInEx 5. See [Supporting BepInEx 5 too](getting-started/packaging.md#supporting-bepinex-5-too).
- **The `using` lines each snippet needs:** `System.IO`, `System.Reflection`, `BepInEx`, `BepInEx.Configuration`, `BepInEx.Unity.Mono`, `BepInEx.Unity.Mono.Configuration`, `ObenseuerCore`, `ObenseuerCore.Ui` and `UnityEngine`. `KeyboardShortcut` needs `BepInEx.Unity.Mono.Configuration`. On BepInEx 5, leave out the two `BepInEx.Unity.Mono` lines, or wrap them in `#if BEPINEX6` and `#endif`, as shown in [Supporting BepInEx 5 too](getting-started/packaging.md#supporting-bepinex-5-too).
- **You can use newer C# than the framework.** The framework is built as C# 5, but your project uses the latest version.
- **`UnityEngine.UIModule.dll` is not needed.**

## Pick a route

| You want | Use | Section |
|---|---|---|
| A panel with HTML, CSS and JavaScript | `WebUi.Open` and `WebView` | 1 |
| Settings the player edits in the Mods menu | BepInEx config entries, or `ModMenu.AddPage` for a custom page | 2 |
| An app on the in-game computer | `ComputerApps.Register` | 3 |
| Notices, hints, subtitles, shake and fades | `Hud` and `ScreenFx` | 4 |

## 1. HTML panels with WebUi

A view is a window that shows a web page from your mod's `web` folder. The page is ordinary HTML, CSS and JavaScript. The framework gives that folder an address of the form `<modid>.obx`, for example `example-infopanel.obx`. The page is served from there and from nowhere else, apart from the framework's shared files.

### Step 1: ship the web folder

Put the page files in a `web` folder next to your project file. Then add this to your `.csproj`. It copies the `web` folder into the build output each time you build:

```xml
<ItemGroup>
  <WebFiles Include="web\**\*" />   <!-- every file under the web folder -->
</ItemGroup>

<Target Name="CopyWeb" AfterTargets="Build">   <!-- runs after each build -->
  <RemoveDir Directories="$(OutDir)web" />   <!-- clear the old copy -->
  <Copy SourceFiles="@(WebFiles)" DestinationFolder="$(OutDir)web\%(RecursiveDir)" />   <!-- copy the new files, keeping folders -->
</Target>
```

The build then gives this layout:

```
bin\InfoPanel.dll
bin\web\index.html
bin\web\panel.css
bin\web\app.js
```

To install, copy `bin\InfoPanel.dll` and the `bin\web\` folder into `BepInEx\plugins\InfoPanel\`, so the `web` folder sits next to the DLL.

**Pass the page as a relative path, such as `index.html`.** `WebUi.Open` returns `null` for a path that starts with `/` or `\`, or that contains `:`, `\`, `%` or `..`.

### Step 2: open the view

`WebUi.Open` makes the window and gives you a `WebView` to talk to it. This is the code for the plugin's `Update` or a key handler. The last argument says whether the view is modal, which means it takes over the game: the player's controls are locked and the mouse cursor is shown.

```csharp
string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "web");   // the web folder next to your DLL
view = WebUi.Open(ModId, folder, "index.html", true);   // mod id, folder, page, modal or not
if (view == null)
{
    Logger.LogWarning("the view did not open: " + WebUi.LastError);   // LastError says why
    return;
}
view.Message += OnMessage;   // be told when the page sends a message
```

**`ModId` must be 1 to 64 characters of letters, digits, `.`, `_` or `-`.** A bad id throws `ArgumentException`. Call `Open` from your mod's normal code (for example `Update`), not from a background thread you started.

**Pass `false` for the last argument if the window should open anyway.** A non-modal view skips the check for a busy game.

`Open` returns `null` when:

- **`modal` is `true` and the game is busy.** That covers a menu, shop or dialogue open, the developer console open, the pause menu active, the main menu showing, or the player's controls already switched off by another modal view or by computer use.
- **`folder` does not exist, or `page` is not a relative path.**
- **The game window does not have focus.** The log says the window opens once the game has focus.
- **The framework's web page helper is missing or failed to start.** See "If WebView2 is missing" below.
  - Technical: the helper is the WebView2 helper, `ObxWebView.dll` in the WebUi folder.

### Step 3: link the shared theme

The framework serves two shared style files at `https://obx-core.obx/`. They make your panel look like the rest of the framework's windows. Link them from your page:

```html
<link rel="stylesheet" href="https://obx-core.obx/obx-theme.css">
<link rel="stylesheet" href="https://obx-core.obx/obx-base.css">
```

- **`obx-theme.css` gives you colours, spacing, text sizes and ready-made parts.** The parts are the classes `.obx-brand`, `.obx-btn` (with `primary`, `danger`, `ghost`, `sm` or `block`), `.obx-card`, `.obx-label`, `.obx-value`, `.obx-table`, `.obx-input`, `.obx-tabs`, `.obx-tab`, `.obx-badge` and `.obx-seg`.
- **`obx-base.css` is optional.** It sets `box-sizing`, a see-through full-height body with no scrolling and no text selection, and the `[hidden]` rule.

These are the values you will use most, with their light-theme values:

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

**The player can choose a dark theme.** Dark values apply under `:root[data-theme="dark"]`. The framework sets `data-theme` on `<html>` before your page runs, from the `Appearance.WebTheme` setting (`light`, `dark` or `system`). When the setting changes while your view is open, the framework posts `{"v":1,"type":"theme","mode":"dark"}` to every open view. Handle it in the page, as Step 5 shows.

### Step 4: JavaScript to C#

The page sends a message to your mod with the browser's `chrome.webview` object. The framework adds no helper, so call it directly. This page code sends `{ type: "press" }` when the button is clicked:

```js
const host = chrome.webview;   // the link to your mod
document.getElementById("press").addEventListener("click", () => {
  host.postMessage({ type: "press" });   // send a message to your mod
});
```

Your mod gets the message as plain JSON text in `view.Message`:

```csharp
private void OnMessage(string json)   // json is the text the page sent
{
    if (json.Contains("\"type\":\"press\"")) presses++;   // fine for a demo, not for real data
}
```

- **The handler gets the text, not separate fields.** Read it yourself, and treat it as untrusted input. `UnityEngine.JsonUtility.FromJson<T>` reads it into a `[Serializable]` class, and needs a reference to `UnityEngine.JSONSerializeModule.dll`. The `json.Contains(...)` check in the example is fine for a demo, not for real data.
- **A message longer than 16384 characters is dropped.**
- **A message whose `type` starts with `obx.` is dropped.** Those names belong to the framework.
- **A message with no `type`, or one that is not a JSON object, reaches your handler as raw text.**

### Step 5: C# to JavaScript

`view.Post` sends a JSON object to the page. Build the text yourself:

```csharp
view.Post("{\"type\":\"state\",\"balance\":" + Wallet.Balance + ",\"presses\":" + presses + "}");   // balance and press count as JSON
```

The page reads it from the `message` event. `e.data` is already a ready-to-use object:

```js
host.addEventListener("message", (e) => {   // runs when your mod posts
  const m = e.data;   // the message as an object
  if (m.type === "theme") document.documentElement.setAttribute("data-theme", m.mode);   // the player changed the theme
  if (m.type === "state") {   // your own state message
    document.getElementById("balance").textContent = String(m.balance);
    document.getElementById("presses").textContent = String(m.presses);
  }
});
```

**`Post` returns `false` until the page's web view is attached,** so do not post right after `Open`. Let the page speak first. The page sends `{"type":"hello"}` when it loads, and the mod answers in `OnMessage`. The finished example does this.

### Step 6: extra folders with MapFolder

`view.MapFolder(name, folder)` gives a second folder its own address, `<name>.<modhost>`. For the info panel, `view.MapFolder("frames", ...)` gives `https://frames.example-infopanel.obx/`. Use it for pictures and other files:

```csharp
view.MapFolder("frames", Path.Combine(folder, "frames"));   // name, folder on disk
```

- **Files from the extra address can be shown, but pages there cannot send messages to your mod.**
- **Your page's security policy must name the extra address.** The policy rule `'self'` only covers the view's own address. See "Security" below.

### Step 7: hide, close and Escape

- **`view.Show()` and `view.Hide()` toggle the view.** `view.Close()` destroys it. A closed view stays closed, and `Open` makes a new one.
- **Escape hides a visible view when `view.EscapeHides` is `true`, which is the default.** With no `KeyPressed` handler, that is any visible view. With a handler, your handler runs first, and the framework then hides only a modal view. Set `view.EscapeHides = false` to handle Escape yourself, then call `Hide` or `Close`.
- **`view.KeyPressed` tells you about two keys.** It is a public `Action<int>` event. The framework raises it with the key code of Escape (27) or F10 (121), when the key is pressed in the focused view. No other key reaches it, so F9 does not while the panel has focus.
- **A scene load hides modal views.** That is a plain `Hide`, so `Dismissed` does not run.
- **`view.Dismissed` runs after the framework hides the view because the player pressed Escape.** It does not run when your mod calls `Hide`.
- **A modal view that is not ready within 20 seconds of `Show` closes itself,** and the log says so.

### Security

Your page runs in a locked-down window.

- **Only the view's own page can send messages.** A page loaded from a `MapFolder` address cannot.
- **The framework answers requests to addresses you did not map with `403` (not allowed).** Your page's security policy is the second layer, so always set one.
- **Going to other addresses, opening new windows, downloads, permission questions and other-program links are all blocked.** Each block is reported with a name ending in `-blocked`, and the view stays open. The report goes to the log and to `WebUi.LastError`. Your `Message` handler never sees it.
- **Developer tools and the right-click menu are off.** Game objects are not available to the page.
- **Service workers (background scripts a page can register) are hidden from the page.**
- **If the page sends a bare string, your mod receives it wrapped in quotes.** So `"hi"` arrives with its quotes.
- **The framework adds no content security policy to your page, so add one.** A content security policy (CSP) is a line in the page that lists where it may load files and scripts from. The Mods menu page sets its own. Set one on your page too:

```html
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; script-src 'self'; style-src 'self' https://obx-core.obx; img-src 'self' data:; connect-src 'none'; object-src 'none'; base-uri 'none'; form-action 'none'">
```

With this policy, keep your scripts and styles in their own files, not inside the page. `style-src` must name `https://obx-core.obx` when you link the shared style files.

### If WebView2 is missing

`WebUi.IsAvailable` becomes `false` when the framework's page helper is missing or WebView2 fails to start. `WebUi.LastError` says why. After `Open` returns `null`, check both. If WebUi is not available, build the same panel as a classic game-style window with `ModWindow` and `ModUi`:

```csharp
if (view == null && !WebUi.IsAvailable)
{
    ModWindow.Open(ModId, "Info panel", WindowDock.Center, w =>   // a classic window in the middle of the screen
    {
        ModUi.Title(w.Body, "Info panel");                                   // a heading
        ModUi.Text(w.Body, "Presses: " + presses, true);                     // a line of text
        ModUi.Button(w.Body, "Press", ButtonStyle.Primary, () => presses++); // a button that adds one
    });
    return;
}
```

**Before the first `Open`, `IsAvailable` is still `true` if the helper file is there.** Only the `Open` call shows whether the runtime is installed.

### Common mistakes

- **Calling any WebUi member from a background thread you started.** `Open` returns `null`, the `bool` members return `false`, and `Close` does nothing.
- **Passing a page path such as `/index.html` or `../other.html`.** `Open` returns `null`.
- **A missing `web` folder next to the DLL.** `Open` returns `null`.
- **Posting right after `Open`.** Wait for the page's first message.
- **Trusting a message.** Read it carefully, check the type, and limit numbers to sensible ranges.
- **Using a message type that starts with `obx.`.** It is dropped.
- **A security policy without `https://obx-core.obx` in `style-src`.** The shared style files do not load.
- **Opening a modal view while a menu is open.** `Open` returns `null`.

## 2. Settings pages in the Mods menu

The Mods menu opens from the Mods tab in the pause menu, and from the MODS button on the main menu. Every loaded plugin that has settings the player can see gets a settings page there. Custom pages made with `ModMenu.AddPage` are listed next to them.

### Step 1: bind config entries

A config entry is one setting your mod saves in its config file. Use BepInEx's config. The Mods menu builds a row for each entry. This code goes in your plugin:

```csharp
private ConfigEntry<bool> announce;                 // an on/off setting
private ConfigEntry<float> volume;                  // a number setting
private ConfigEntry<KeyboardShortcut> openKey;      // a key setting

private void Awake()   // runs once when the game starts
{
    announce = Config.Bind("General", "Announce", true, "Show a notice when the panel opens");   // section, name, default, help text
    volume = Config.Bind("General", "Volume", 0.5f, new ConfigDescription("Alert volume", new AcceptableValueRange<float>(0f, 1f)));   // limited to 0 to 1
    openKey = Config.Bind("Keys", "Open", new KeyboardShortcut(KeyCode.F9), "Key that opens the panel");   // default key is F9
}
```

The kind of row depends on the type of the entry:

| Entry type | Row |
|---|---|
| `bool` | toggle |
| enum, or a value list (`AcceptableValueList`) | stepper that cycles through the values |
| `KeyboardShortcut` | key binding: click it, then press a key |
| `int` or `float` with an `AcceptableValueRange` | slider |
| `string`, or a type that converts from text | text field |
| any other type | read-only |

Most rows save at once. Sliders save 0.25 seconds after the last change, and again when the game quits.

### Step 2: add a custom page

Use `ModMenu.AddPage` when the page needs more than rows of settings. The framework calls your function when the page is opened, and gives it the page's content area. You fill it with controls from `ModUi`. Put this in `Awake`, after the entries above:

```csharp
ModMenu.AddPage("example.settingsdemo", "Settings demo", content =>   // mod id, page title, function that builds the page
{
    RectTransform stack = ModUi.Stack(content);   // a column to put controls in
    ModUi.Title(stack, "Settings demo");          // a heading
    ModUi.Toggle(stack, "Announce", announce.Value, v => announce.Value = v);                 // a switch tied to the setting
    ModUi.Slider(stack, "Volume", 0f, 1f, volume.Value, v => volume.Value = v, 2);            // a slider from 0 to 1, 2 decimals
});
```

- **`AddPage` adds an entry to the Mods menu's sidebar.** It never adds a tab to the game's own menu. A page with the same title as another mod's page is still added, and one warning names both mods. Call it once: a second call from the same mod adds a second page.
- **The sidebar is read when the Mods window opens.** A page added while the window is open shows the next time it opens.
- **Your function runs when the player selects the page.** If it throws, the page shows a notice and the error goes to the log. The other pages still work.
- **When the Mods menu is showing its web page, a custom page opens as a classic pane inside it.**
- **`ModMenuWidgets` is obsolete.** Build new pages with `ModUi`.

### Common mistakes

- **Expecting the Mods menu to change the game's own options.** It edits your config. The game's display and quality options are read only (see [settings](../api/settings.md)).
- **A `ModUi` function that throws closes the Mods window.** It closes with the reason `Fault`. Keep the functions short, and do the work in your mod.
- **Expecting a page for a plugin with no visible config entries.** It gets no settings page.

## 3. Apps on the in-game computer

Apps you register appear as icons on the in-game computer desks. When the player clicks one, the framework calls your function with an empty area (the host) for you to fill.

### Step 1: register the app

```csharp
ComputerApps.Register("example.notesapp", "notes", "Notes", null, host => Build(host));   // mod id, app id, title, icon (or null), function that builds the screen
```

- **`appId` must not be empty and must not contain `/`.** Registering a second app with the same id returns `false` and writes a warning that names both mods. The first one stays.
- **`Register` returns `false` when `ComputerApps.IsAvailable` is `false`,** because the desk changes are then not installed.
- **`icon` may be `null`.** The title is then drawn on the icon.
- **The `open` function receives the host.** If it throws, the error goes to your mod's log and the host is hidden.

### Step 2: build into the host

The host is an area of the computer screen. Fill it with `ModUi` controls. The framework clears it before each open, so build everything again each time.

```csharp
private void Build(RectTransform host)
{
    RectTransform stack = ModUi.Stack(host);   // a column to put controls in
    ModUi.Title(stack, "Notes");               // a heading
    ModUi.Text(stack, note, false);            // a line of text, the note
    ModUi.Button(stack, "Reset", ButtonStyle.Secondary, () => note = "Buy bread.");   // a button that resets the note
}
```

**Keep your state in your mod, not in the host's children.** The framework destroys the host's children on the next open.

### Step 3: an HTML app on the monitor

A `WebView` can follow the monitor the player is using. This code opens it the first time a monitor is used, pins it to the screen each time the player starts using a computer, and hides it when they stop:

```csharp
string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "web");   // your web folder
WebView view = null;
ComputerApps.ScreenChanged += s =>   // s is the monitor's screen, or null when the player stops
{
    if (s == null)
    {
        if (view != null) view.Hide();   // the player left the computer
        return;
    }
    if (view == null) view = WebUi.Open(ModId, folder, "index.html", false);   // open it the first time, not modal
    if (view == null) return;   // the open was refused, so try again next time
    view.PinTo(s);   // put the view on the monitor
    view.Show();
};
```

`ComputerApps.ScreenChanged` is raised with the monitor's screen (a `RectTransform`), or `null` when the player stops using it. `ComputerApps.ActiveScreen` holds the current value. The view is not opened at game start, because `Open` returns `null` until the game window has focus. A refused `Open` is tried again on the next use.

### Step 4: full-screen window and world prompts

```csharp
ComputerApps.AddInteraction(board, "example.notesapp", "Whiteboard", "Read", () => ComputerApps.OpenStandalone("example.notesapp", "notes"));   // object, mod id, label, verb, what to do
```

- **`AddInteraction` gives a world object the game's use prompt.** The object needs a `Collider` (the invisible shape the game uses to know what the player is looking at). It returns `false` when the object already has a game prompt, and a warning is logged once.
- **`OpenStandalone` opens the app full screen over the game.** It returns `false` when the game is busy.
- **`ComputerApps.Close(appId)` hides the app.** `ComputerApps.AppClosed` runs when a view closes.

### Common mistakes

- **A host with no `GraphicRaycaster` above it gets no clicks.** The desk host has one. Add one to any canvas you create yourself.
- **Keeping UI objects in the host.** The framework destroys them on the next open.
- **An `appId` with `/`.** `Register` returns `false` and logs a warning.
- **A second `Register` with the same `appId`.** It returns `false`.

## 4. HUD and screen effects

The HUD is what the game shows over the picture: notices, subtitles and hints. `ScreenFx` is shaking and fading the screen. Put calls like these in an event handler or a key press.

```csharp
Hud.Notify("Example notice");                                           // a notice box
Hud.ShowSubtitle("Example subtitle", 2f);                               // a subtitle, fading out over 2 seconds
Hud.ShowHint("example.hudnotes", "Tip", new[] { "Example hint line." }); // a hint box, owned by your mod id
Hud.HideHint("example.hudnotes");                                       // remove that hint
ScreenFx.Shake(0.4f, 0.02f);                                            // shake for 0.4 seconds, a small amount
ScreenFx.FadeToBlack(0.1f);                                             // fade to black at speed 0.1
ScreenFx.FadeIn(0.1f);                                                  // fade back in
```

- **`Hud.Notify(text)` shows the game's notice box with the type line `Notice`.** `Hud.Notify(title, body)` shows the same box with your own title line.
- **`Hud.ShowSubtitle(text, fadeSeconds)` types the text on screen, then fades it out.** It types 20 characters per second, then fades over `fadeSeconds`.
- **`Hud.ShowHint(modId, title, lines)` shows one hint per mod id.** Each line is a paragraph. The hint appears in the game's hint box, the one the card game rules use, so the player can switch it with the Hints key (K by default). `Hud.HideHint(modId)` removes it.
- **`Hud.ShowNote(noteId)` opens a note the player already has.** It cannot show text of your own.
- **`ScreenFx.Shake(duration, amount)` shakes the camera.** A new shake replaces the one in progress.
- **`ScreenFx.FadeToBlack(speed)` leaves the screen black until `ScreenFx.FadeIn` runs.** Call both.
- **`ScreenFx.SetDeathMessage(text)` sets the reason shown on the death screen.** Call it just before the death screen, because the game replaces the text when it sets its own.
- **To change the field of view, use `ScreenFx.SetFovOverride(owner, fov)` and `ClearFovOverride(owner)`.** Do not set the camera's field of view yourself. The game resets it every frame on cameras that have its field-of-view component switched on, so your change is undone on the next frame.
  - Technical: the component is the game's `SetCameraFOV`, which writes the saved value back every frame. See [ScreenFx](../api/screen-fx/fov-override.md#fov-override).

### Common mistakes

- **The `Hud` and `ScreenFx` calls return `false` on the main menu or while loading,** when the game object they need is missing. Check the result if the message matters.
- **`Hud.Notify` and `Hud.ShowSubtitle` throw `ArgumentException` for empty text,** and `Hud.ShowHint` throws for an empty title.
- **A negative `fadeSeconds` throws `ArgumentException`.**
- **`ScreenFx.Shake` throws `ArgumentException` for a duration of 0 or less, or a negative amount.**
- **`ScreenFx.FadeToBlack` and `ScreenFx.FadeIn` throw `ArgumentException` for a speed of 0 or less.**
- **`Hud.ShowHint` and `Hud.HideHint` throw `ArgumentException` for a mod id that is not 1 to 64 characters of letters, digits, `.`, `_` or `-`.**
- **`FadeToBlack` without a later `FadeIn` leaves the player's screen black.**

## The finished mod: info panel

The info panel opens with F9. It shows the player's balance and a press count. The page asks for the state when it loads, and each button press goes back to the mod, which updates the count and sends the state again.

Make this folder, with the files below in it. `InfoPanel.cs` takes the place of `Plugin.cs` in your project. Add the `CopyWeb` block from Section 1, Step 1 to your `.csproj`.

```
InfoPanel\InfoPanel.cs
InfoPanel\web\index.html
InfoPanel\web\panel.css
InfoPanel\web\app.js
```

`InfoPanel.cs` is the mod. `Update` runs every frame, opens or toggles the view when F9 is pressed, and answers the page's messages.

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
    private const string ModId = "example.infopanel";   // your mod id
    private WebView view;                               // the open window, or null
    private int presses;                                // how many times the button was pressed

    private void Update()   // runs every frame
    {
        if (!Input.GetKeyDown(KeyCode.F9)) return;   // only act when F9 is pressed
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

    private void OnMessage(string json)   // the page sent a message
    {
        if (json.Contains("\"type\":\"hello\"")) SendState();   // the page loaded and wants the numbers
        else if (json.Contains("\"type\":\"press\""))           // the button was pressed
        {
            presses++;
            SendState();
        }
    }

    private void SendState()   // send the balance and the count to the page
    {
        view.Post("{\"type\":\"state\",\"balance\":" + Wallet.Balance + ",\"presses\":" + presses + "}");
    }
}
```

`web\index.html` is the page. It links the shared style files, sets the security policy, and loads your own style and script.

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

`web\panel.css` lays the panel out, using the framework's tokens.

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

`web\app.js` talks to the mod. It sends `hello` when it loads, sends `press` when the button is clicked, and shows the numbers the mod sends back.

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

Build with `dotnet build -c Release`. Copy `InfoPanel.dll` and the `web` folder from the build output into `BepInEx\plugins\InfoPanel\`, as in Section 1, Step 1. Run the game and load a save. Press F9 to open the panel, press the button and watch the count change. Press Escape to hide it, then F9 to show it again. If it does not open, read `BepInEx\LogOutput.log` for the line that starts `InfoPanel: the view did not open`.

## Where to go next

- [WebUi](../api/webui.md): the full WebUi and WebView reference, including pinning, navigation and the page helper's messages.
- [UI kit](../api/ui.md): the `ModUi` and `ModWindow` kit used by the fallback and the settings pages.
- [API reference](../api-core.md), sections `ModMenu` and `ComputerApps`: the Mods menu and computer app references.
- [HUD](../api/hud.md): `Hud`, `ConsoleCommands` and the other HUD calls.
- [ScreenFx](../api/screen-fx.md): `ScreenFx` in full, including the fade and field-of-view notes.
- [Settings](../api/settings.md): the game's own settings and `Keybinds`, which register mod keys in the Mods menu.
- [Using the framework alongside other mods](multiple-mods.md): what to do when other mods use the same calls.
