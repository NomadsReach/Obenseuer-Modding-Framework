# Obenseuer Modding Framework WebUi API (WebUi, WebView)

Status: experimental. Not checked in game yet.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.webui`. Logs go to `BepInEx\logs\obenseuer.moddingframework.webui.log`.
Native part: `ObxWebView.dll`, a WebView2 shim that Core ships next to `ObenseuerCore.dll`.
Requirements: the WebView2 Runtime, preinstalled on Windows 10/11, needed only by mods that use WebUi.

Everything not listed here is internal.

## Surface

```csharp
public static class WebUi
{
    public static bool IsAvailable { get; }
    public static string LastError { get; }
    public static string Theme { get; }
    public static event Action<string> ThemeChanged;
    public static WebView Open(string modId, string folder, string page, bool modal);
}

public sealed class WebView
{
    public string ModId { get; }
    public string Origin { get; }
    public bool IsVisible { get; }
    public bool IsClosed { get; }
    public bool Modal { get; }
    public bool EscapeHides { get; set; }
    public event Action<string> Message;
    public event Action<int> KeyPressed;
    public event Action Dismissed;
    public bool Show();
    public bool Hide();
    public bool SetBounds(int x, int y, int width, int height);
    public bool FullWindow();
    public bool PinTo(RectTransform target);
    public bool Navigate(string page);
    public bool MapFolder(string name, string folder);
    public bool Post(string json);
    public void Close();
}
```

## Rules

- **Main thread only.** Call every member from Unity's main thread. Off the main thread, `Open` returns `null`, the `bool` members return `false`, and `Close` does nothing.
- **Bad mod id throws.** `Open` throws `ArgumentException` when `modId` is not 1 to 64 characters of letters, digits, `.`, `_` or `-`, as the other Core APIs do.
- **Bad page or folder returns null.** `Open` returns `null` when `folder` is not an existing directory, or when `page` is not a relative path (see Navigation).
- **Failures do not throw**, except `ArgumentException` for a bad `modId`. A failed shim call returns `false` or `null`. `LastError` then holds the shim's error text.

## WebUi

- `IsAvailable`: true on Windows when `ObxWebView.dll` is next to `ObenseuerCore.dll` and the native start has not failed. It becomes false once the native start fails or a fatal error with no view id arrives (see Messages from the shim), and `LastError` says why.
- `LastError`: the last shim error, or `null`. It is set by a failed call, by an `obx.error`, by a failed start, and by a missing `ObxWebView.dll`.
- `Theme`: `"light"` or `"dark"`. It comes from the Core config `Appearance.WebTheme` (`light`, `dark` or `system`; default `light`). `system` reads the Windows apps theme once at start and again when the setting changes. It does not follow a later change of the Windows theme.
- `ThemeChanged`: raised with the new mode after the setting changes. Core posts the same change to every open view first (see Theme).
- `Open(modId, folder, page, modal)`: creates a view and shows it. It returns `null` when:
  - the native start is not possible (not Windows, no shim, no active game window yet, or the start failed);
  - `modal` is true and `CoreInput.GameBusy()` is true (a menu, the pause menu, a shop, a dialogue or the developer console is open);
  - `folder` is missing or `page` is not a relative path;
  - the view cannot be shown. It is closed again.
  A non-modal `Open` ignores the busy check.
- The first `Open` starts the shim (lazy). The shim's user data folder is `BepInEx\cache\ObxWebView`.
- The view's host is `<modid>.obx`: the mod id lowercased, every character outside `a-z 0-9 -` replaced by `-`, cut to 63 characters. For example `example.webuihello` gives `example-webuihello.obx`. `folder` is mapped to that host, so `https://<host>/index.html` is `<folder>\index.html`.
- Two open views with the same mod id are allowed. Each has its own id.

## WebView

- `ModId`, `Origin`: `Origin` is `https://<host>/`, the start of every URL this view loads.
- `IsVisible`, `IsClosed`, `Modal`: `Modal` is set by `Open`.
- `Show`: makes the view visible at its bounds. A modal view returns `false` while `CoreInput.GameBusy()` is true. A modal view that shows also disables the player controls with the cursor shown (see Modal views).
- `Hide`: hides the view and releases the controls of a modal view.
- `SetBounds(x, y, width, height)`: sets the bounds in client pixels and turns off full-window. It returns `false` for a size of zero or less.
- `FullWindow()`: the default. The view covers the game client rect, follows it each frame, and only calls the shim when the size changes.
- `PinTo(target)`: covers a `RectTransform` on screen instead of the full window (see Pinning). `null` unpins it and returns to full window. It returns `false` when the view is closed or not on the main thread.
- `Navigate(page)`: loads a relative page in the view's folder (see Navigation).
- `MapFolder(name, folder)`: maps an extra folder to `<name>.<modhost>`, for example `frames.example-webuihello.obx`. `name` is cleaned the same way as the mod id. The extra host can load content, but its pages cannot post messages to the view (the shim accepts messages only from the view's own host).
- `Post(json)`: sends a JSON object to the page's `chrome.webview` message event. It returns `false` until the view's web core is ready, so a post made right after `Open` can fail. Post from a `Message` handler, or after the page has sent its first message.
- `Message`: raw JSON text the page sent with `chrome.webview.postMessage`. The handler gets the text, not parsed fields. A message longer than 16384 characters is dropped. A message whose `type` starts with `obx.` is dropped, because those names are reserved for the shim. A message with no `type`, or that is not a JSON object, is delivered as raw text.
- `KeyPressed`: the virtual-key code the shim forwards when Escape (27) or F10 (121) is pressed in the view. The handlers run first; then Escape may hide the view (see Escape).
- `Dismissed`: raised right after Core hides the view for Escape (see Escape). It is not raised when the mod calls `Hide`.
- `Close`: hides the view, destroys it and releases its controls. A closed view stays closed; `Open` makes a new one.

## Pinning

`PinTo(target)` places the view over `target`'s projected screen rectangle. Each frame the view follows the camera, so it stays on a monitor while the camera zooms in to it. `ComputerApps.ActiveScreen` gives the monitor's `target` while the player uses a computer:

```csharp
WebView view = WebUi.Open("example.myapp", folder, "index.html", false);
if (view == null) return;
view.Hide();
ComputerApps.ScreenChanged += s => { if (s != null) { view.PinTo(s); view.Show(); } else view.Hide(); };
```

- The view is a flat rectangle. It is drawn over the monitor, not into it, so it looks inset only while the camera faces the screen. Off-screen, behind the camera, or with its target inactive, the view is hidden natively and `IsVisible` stays true.
- The rectangle is the inner part of the target, so a slightly angled view does not spill over the bezel. It is hidden when that rectangle is under 64 px on either side.
- While pinned, `SetBounds` and `FullWindow` are overwritten on the next frame.
- A modal view pinned to a computer screen shows during computer use and does not lock the controls itself; the computer mode already holds them. Its `Show` skips the `CoreInput.GameBusy()` check while pinned.
- `Close` unpins.

## Navigation

`Navigate` and `Open` take a relative path. It is refused when it:
- is empty or starts with `/` or `\`;
- contains `:`, `\`, `%` or `..`.

A refused path returns `null` from `Open` and `false` from `Navigate`. The shim also blocks navigation outside the view's mapped hosts, new windows, downloads, permission requests and external URI schemes.

## Shared assets

Core ships two shared asset files in `web\`, next to `ObenseuerCore.dll`. Every view maps that folder as `https://obx-core.obx/`, so any page can link them. The `web\modmenu\` folder in it holds the Mods menu's own page. It is also reachable at `https://obx-core.obx/modmenu/` from any view, but it is not an API and may change between versions.

- `https://obx-core.obx/obx-theme.css`: design tokens and generic component classes. It has `:root` tokens for light, dark under `:root[data-theme="dark"]`, and dark under `prefers-color-scheme: dark` unless the page sets `data-theme="light"`. The tokens cover surfaces, text, muted text, lines and controls, amber, red, the two brand colours, fonts, letter spacing, font sizes, spacing, radii and motion. The component classes are `.obx-brand`, `.obx-btn` (with `primary`, `danger`, `ghost`, `sm`, `block`), `.obx-tabs` and `.obx-tab`, `.obx-card`, `.obx-label`, `.obx-value`, `.obx-table`, `.obx-input`, `.obx-badge` and `.obx-seg`. It has no global element rules and no game or OS chrome.
- `https://obx-core.obx/obx-base.css`: optional global rules. It sets `box-sizing`, `html`/`body` (no scroll, transparent background, no text selection), the `[hidden]` rule, focus outline, and the body transition. A page opts in by linking it. A page that does not link it keeps the browser's defaults.

A page that links `obx-theme.css` gets its colours from `data-theme`. Core sets `data-theme` on `<html>` before the page runs, from `WebUi.Theme`. When the setting changes, Core posts `{"v":1,"type":"theme","mode":"light"}` (or `"dark"`) to every view. A page should set the attribute from that message, as the example does.

Core adds no Content-Security-Policy to mod pages, so a page should set its own `<meta http-equiv="Content-Security-Policy">`, as `web/modmenu/index.html:5` does.

## Theme

- The initial `data-theme` comes from an init script that Core registers on each view. It sets the attribute on `<html>` as soon as the element exists.
- A change is posted to the page as `{"v":1,"type":"theme","mode":"light|dark"}`. Core also adds the new mode's init script to each open view, so a later navigation starts in that mode. Core does not change the attribute itself after the page loads.

## Messages from the shim

Core reads the shim's queue each frame. Messages from the shim (`obx.key`, `obx.error`, `obx.status`) are not page messages.

- `obx.key`: a view gets Escape or F10. It goes to `KeyPressed`, and Escape may then hide the view (see Escape).
- `obx.error`: `LastError` is set to `where hr=N` and logged once for each distinct pair. Fatal errors (`environment`, `windowed`, `get_CoreWebView2`, and `process-failed` with kind 0 or 1) close that view. Other errors, such as `navigation-blocked` or `resource-blocked`, are logged and the view stays open. An error with no view id (`environment`) is fatal to every view: they all close and `IsAvailable` becomes false.
- `obx.status`: the first one for a view, sent when its web controller is ready, counts as started. A modal view that gets none within 20 s of `Show` is closed and logged once.
- Other shim messages are ignored.

## Modal views

A modal view disables the player controls with the cursor shown, one holder per view, while it is visible. `Hide`, `Close` and any scene change release the controls. A non-modal view never touches the controls.

## Escape

Escape hides a modal view by default, after the `KeyPressed` handlers run. Core then raises `Dismissed` only if the view was visible and is now hidden, so the mod that opened it can close its own state too:

```csharp
WebView view = WebUi.Open("example.myapp", folder, "index.html", true);
if (view == null) return;
view.Dismissed += () => MyMod.Close();
```

- `EscapeHides` (default `true`): when it is `false`, Core never hides the view on Escape. Use it when the page handles Escape itself, for example to close a menu first: `view.EscapeHides = false;`. The page or the mod then decides when the view closes, by calling `Hide` or `Close`.
- Without a `KeyPressed` handler, Escape hides the view at once, modal or not, when `EscapeHides` is true. With a handler, a modal view is hidden after the handler runs.

## Threading and lifetime

- `Core.Update` polls the shim (`WebUi.Tick`). Handlers run on the main thread, inside the poll.
- `Core.OnApplicationQuit` flushes the config saves, then closes every view and shuts the shim down. After that, `Open` does not start the shim again.
- A handler that throws is logged through `ApiLog` once per handler kind (`WebView.Message`, `WebView.KeyPressed`). The other handlers still run.

## Security notes

- Page messages are untrusted. `Message` gets the raw text, and the mod must check it.
- Pages run from the mod's own folder, mapped to a host. They cannot load from other hosts, open windows, download files or ask for permissions. The shim denies these and reports them as `obx.error` with a `*-blocked` name.
- Dev tools are off, the default context menu is off, and host objects are not exposed to the page.
- Only the view's own host can post messages. A frame or an extra mapped host cannot.

## Packaging

- `build.cmd` builds `native\ObxWebView`, copies `ObxWebView.dll` into `bin\`, copies `web\` recursively into a cleared `bin\web\`, and copies the WebView2 licence to `bin\ThirdPartyNotices-WebView2.txt`.
- `WebView2Loader` is linked statically (`WebView2LoaderStatic.lib`), so no loader DLL ships.
- `tools\package.cmd` copies `ObxWebView.dll` and `web\` into the plugin folder, and the WebView2 SDK licence as `ThirdPartyNotices-WebView2.txt`.

## Example

`Examples/WebUiHello` is a minimal mod: F9 opens a modal view of `web\index.html`, F9 again shows or hides it, the page sends `{"type":"ping"}` and the mod answers `{"type":"pong","at":N}`. Escape hides it.

## Smoke test (not run in game)

1. Start the game with Core and `WebUiHello`. Press F9. The view opens over the game, the cursor shows, and the player cannot move. If the shim sends no `obx.status` for the view within 20 s of its opening, the modal view closes, and `BepInEx\logs\obenseuer.moddingframework.webui.log` says `got no obx.status within 20 s, closed`.
2. Press the page button. The page shows "The mod answered at frame N".
3. Press Escape. The view hides and the controls come back. Escape hides a modal view after any `KeyPressed` handlers have run, unless `EscapeHides` is false. Press F9 again. The view shows again.
4. Change `Appearance.WebTheme` to `dark` in the Mod Menu (BepInEx does not watch the `.cfg` file). The open view changes to dark.
5. Set `Appearance.WebTheme` to `system`. The view follows the Windows apps theme.
6. Load a save, then quit. The log has no shim error.

The native probes (`native\ObxWebView\test`, `ObxHost.exe --selftest` and `--lockdown`) run without the game.
