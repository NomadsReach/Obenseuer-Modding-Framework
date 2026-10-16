---
title: Obenseuer Modding Framework WebUi API (WebUi, WebView)
parent: API reference
nav_order: 90
---

# Obenseuer Modding Framework WebUi API (WebUi, WebView)

Status: experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.webui`. Logs go to `BepInEx\logs\obenseuer.moddingframework.webui.log`.
Native part: `ObxWebView.dll`, a WebView2 shim that the WebUi module ships next to `ObenseuerCore.WebUi.dll`.
Requirements: the optional WebUi module (`ObenseuerCore.WebUi.dll`, Windows only) and the WebView2 Runtime, preinstalled on Windows 10/11. Needed only by mods that use WebUi and by the web Mods menu.

Everything not listed here is internal.

## Install and requirements

WebUi is a separate BepInEx plugin, `ObenseuerCore.WebUi.dll` (GUID `obenseuer.moddingframework.webui`, version 0.11.0). The framework is required, and the module depends on it. Install the module from the WebUi zip, `ObenseuerModdingFramework-WebUi-<version>-beta.zip`. It goes to `BepInEx\plugins\ObenseuerCore.WebUi\` with `ObxWebView.dll` and `web\`.

- A mod that uses WebUi declares `[BepInDependency("obenseuer.moddingframework.webui", ">=0.11.0")]` and references `ObenseuerCore.WebUi.dll`. The WebUi types are not in `ObenseuerCore.dll`. On BepInEx 5, write `"0.11.0"` instead, because BepInEx 5 cannot parse a version range. See [Supporting BepInEx 5 too](../guides/getting-started/packaging.md#supporting-bepinex-5-too).
- Without the module, the WebUi types are missing and the Mods menu uses the classic window.
- The WebView2 Runtime is needed for the view. It is preinstalled on Windows 10 and 11.

## Surface

```csharp
public static class WebUi
{
    public static bool IsAvailable { get; }
    public static string LastError { get; }
    public static string Theme { get; }
    public static event Action<string> ThemeChanged;
    public static WebView Open(string modId, string folder, string page, bool modal);
    public static void SuspendAll(string reason);
    public static void ResumeAll(string reason);
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
    public bool AllowFrameHost(string host);
    public bool Post(string json);
    public void Close();
}
```

## Rules

- **Main thread only.** Call every member from Unity's main thread. Off the main thread, `Open` returns `null`, the `bool` members return `false`, and `Close` does nothing.
- **Bad mod id throws.** `Open` throws `ArgumentException` when `modId` is not 1 to 64 characters of letters, digits, `.`, `_` or `-`, as the other framework APIs do.
- **Bad page or folder returns null.** `Open` returns `null` when `folder` is not an existing directory, or when `page` is not a relative path (see Navigation).
- **Failures do not throw**, except `ArgumentException` for a bad `modId`. A failed shim call returns `false` or `null`. `LastError` then holds the shim's error text.

## WebUi

- `IsAvailable`: true on Windows when `ObxWebView.dll` is next to `ObenseuerCore.WebUi.dll` and the native start has not failed. It becomes false once the native start fails, or once a fatal error with no view id arrives outside thread mode (see Messages from the shim). `LastError` says why. In thread mode such an error restarts the shim in composition for the rest of the session instead, so `IsAvailable` stays true unless that start fails.
- `LastError`: the last shim error, or `null`. It is set by a failed call, by an `obx.error`, by a failed start, by an `Open` that finds no active game window (`no active game window`) or runs after shutdown (`WebUi is shut down`), and by a missing `ObxWebView.dll`.
- `Theme`: `"light"` or `"dark"`. It comes from the WebUi module config `Appearance.WebTheme` (`light`, `dark` or `system`; default `light`). `system` reads the Windows apps theme once at start and again when the setting changes. It does not follow a later change of the Windows theme.
- Config `Debug.WebUiMode` (`thread`, `composition` or `windowed`, default `thread`) selects how WebView2 is hosted. `thread`: the shim runs on its own thread, and each view is a WebView2 control in a popup window owned by the game window, placed over the view's bounds in the game's client rectangle. `composition`: the composition root on the game thread, the mode before 0.9. `windowed`: a child window of the game on the game thread. It is read once when the shim starts, so a change needs a game restart. `Debug.WebUiWindowed` (`true` or `false`, default `false`) is deprecated: use `WebUiMode = windowed` instead. When it is `true`, the mode is `windowed` whatever `WebUiMode` says. Thread mode falls back to composition for the rest of the session in three cases, and each is logged: when exclusive fullscreen is on at the first `Open`; when the thread does not start (composition is then tried at once); and when a fatal shim error arrives (the views close, and the next `Open` uses composition).
- `ThemeChanged`: raised with the new mode after the setting changes. The framework posts the same change to every open view first (see Theme).
- `SuspendAll(reason)` and `ResumeAll(reason)`: hold and release a suspension for a reason. Each reason is counted, so every `SuspendAll` needs one `ResumeAll` with the same reason. While any reason is held, views that are popup windows (thread mode) are hidden. An empty reason is ignored, and both calls do nothing off the main thread.
- `Open(modId, folder, page, modal)`: creates a view and shows it. It returns `null` when:
  - the native start is not possible (not Windows, no shim, no active game window yet, or the start failed);
  - `modal` is true and the game is busy (a menu, the pause menu, a shop, a dialogue or the developer console is open, no game camera is loaded, or the player's controls are disabled);
  - `folder` is missing or `page` is not a relative path;
  - the view cannot be shown. It is closed again.
  A non-modal `Open` ignores the busy check.
- The first `Open` starts the shim (lazy). The shim's user data folder is `BepInEx\cache\ObxWebView`.
- The view's host is `<modid>.obx` when the mod id is lowercase letters and digits with single dots between them, at most 63 characters, and not `obx.core`. Each dot becomes `-`, so `example.webuihello` gives `example-webuihello.obx`. Any other id gets `h--` followed by the first 16 hex digits of the SHA-256 of the id, then `.obx`, so `My_Mod` gives `h--faa957b3378b73e0.obx`. Different ids get different hosts (a hashed host is 64 bits of SHA-256, so a clash is not a practical risk), and no id maps to `obx-core.obx`, the host of the shared assets. `folder` is mapped to that host, so `https://<host>/index.html` is `<folder>\index.html`.
- Two open views with the same mod id are allowed. Each has its own id.

## WebView

- `ModId`, `Origin`: `Origin` is `https://<host>/`, the start of every URL this view loads.
- `IsVisible`, `IsClosed`, `Modal`: `Modal` is set by `Open`.
- `Show`: makes the view visible at its bounds. A modal view returns `false` while the game is busy, as listed under `Open`. A modal view that gets no `obx.status` within 20 seconds of showing is closed, and an error is logged. A modal view that shows also disables the player controls with the cursor shown (see [Step 2 of the UI guide](../guides/ui.md#step-2-open-the-view)).
- `Hide`: hides the view and releases the controls of a modal view.
- `SetBounds(x, y, width, height)`: sets the bounds in client pixels and turns off full-window. It returns `false` for a size of zero or less.
- `FullWindow()`: the default. The view covers the game client rect, follows it each frame, and only calls the shim when the size changes.
- `PinTo(target)`: covers a `RectTransform` on screen instead of the full window (see Pinning). `null` unpins it and returns to full window. It returns `false` when the view is closed or not on the main thread.
- `Navigate(page)`: loads a relative page in the view's folder (see Navigation). In composition and windowed modes a navigation the shim cannot start returns `false` and sets `LastError`. In thread mode the call returns `true` at once and the shim navigates on its own thread, so such a failure arrives later as an `obx.error` with `where` `Obx_Navigate`. Either way the view keeps its current page.
- `MapFolder(name, folder)`: maps an extra folder to `<name>.<modhost>`, for example `frames.example-webuihello.obx`. `name` gets the same label as a mod id (see the view's host above). A name of at most 63 lowercase letters and digits, with single dots between them, has each dot turned into `-`, so `frames` gives `frames`. Any other name, such as `Frames`, gets an `h--` label, so `Frames` and `frames` get different hosts. It returns `false` for a closed view, a call off the main thread, an empty `name`, or a `folder` that is not an existing directory. The extra host can load content, but its pages cannot post messages to the view (the shim accepts messages only from the view's own host). On an open view, a mapping that the shim cannot make is not kept, so the host stays refused. In thread mode the call returns `true` at once and the shim makes the mapping on its own thread, so a failure arrives later as an `obx.error` with `where` `Obx_MapFolder`, and `LastError` is set.
- `AllowFrameHost(host)`: lets frames of this view load one live website, for example `stalburg.net`. The host must be an exact lower-case DNS name (see Frame hosts). It returns `false` and sets `LastError` for any other value. It can be called before or after the page loads.
- `Post(json)`: sends a JSON object to the page's `chrome.webview` message event. It returns `false` for a closed view, a call off the main thread, a `null` json, or when the shim call fails. The message is queued for the shim, so a message queued before the web core is ready is dropped. Post from a `Message` handler, or after the page has sent its first message.
- `Message`: raw JSON text the page sent with `chrome.webview.postMessage`. The handler gets the text, not parsed fields. A message longer than 16384 characters is dropped. A message whose `type` starts with `obx.` is dropped, because those names are reserved for the shim. A message with no `type`, or that is not a JSON object, is delivered as raw text.
- `KeyPressed`: the virtual-key code the shim forwards when Escape (27) or F10 (121) is pressed in the view. The handlers run first; then Escape may hide the view (see [Step 7 of the UI guide](../guides/ui.md#step-7-hide-close-and-escape)).
- `Dismissed`: raised right after the framework hides the view for Escape (see [Step 7 of the UI guide](../guides/ui.md#step-7-hide-close-and-escape)). It is not raised when the mod calls `Hide`, or when a `Single` scene load hides a modal view.
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
- A modal view pinned to a computer screen shows during computer use and does not lock the controls itself; the computer mode already holds them. Its `Show` skips the busy check while pinned. Pinning a view that already holds the lock releases the lock.
- `Close` unpins.

## Frame hosts

`AllowFrameHost(host)` is the only way a page shows a live website. It allows one exact HTTPS host, in frames of that view.

- `host` must be an exact lower-case DNS name, for example `stalburg.net`. It has at least two labels separated by dots. Each label is 1 to 63 characters of lower-case letters, digits and hyphens, and does not start or end with a hyphen. The whole name is at most 253 characters. A name whose last label is all digits, such as `127.0.0.1`, is refused. A scheme, port, path, user name or wildcard returns `false` and sets `LastError`. Each call adds one host. There is no wildcard.
- A frame navigation to `https://<host>/` (default port only) loads. Requests to `https://<host>/` from the frame pass too.
- Still blocked: a top-level navigation to the host; a frame on the shell host (refused, so a frame cannot load the page that owns the bridge); any other host; new windows, downloads, permission requests and external URI schemes.
- Messages from a frame of the host are dropped as `message-source-blocked`. Only the view's own top document can post messages.
- The shell page's `message` listener must check `e.source` and `e.origin` before it acts on a message. A frame of the allowed host can call `parent.postMessage` on the shell page, so a message is trusted only when `e.origin` is the shell's own origin and `e.source` is the window the page expects.
- The `url` in `obx.frame` comes from remote content. Treat it as data, never as HTML: write it with `textContent`, not `innerHTML`.
- The page must add `frame-src https://<host>` to its own Content-Security-Policy. Without it the page's CSP refuses the frame.
- Put the frame in a sandbox: `<iframe sandbox="allow-scripts" src="https://<host>/...">`. Do not add `allow-same-origin`, `allow-top-navigation` or `allow-popups`. A frame with `sandbox=""` runs no script, so the script rules below do not apply to it.
- The shim cannot tell which frame sent a request, so requests from an allowed host pass for the whole view. Requests to any other host are refused with `resource-blocked`, whether the page or a frame makes them. The filter is registered for every request context and source kind that WebView2 reports. The lockdown test requires `resource-blocked` for external requests from the page, and it checks that an external image in a frame is refused. It does not check each request type, or `fetch` and `XMLHttpRequest` from a frame.
- The page's CSP does not reach the frame. A frame runs under its own site's policy, so the page's `connect-src` does not cover it.
- In every document (the page, and each frame), the init script removes `WebSocket`, `WebSocketStream`, `EventSource`, `WebTransport`, `RTCPeerConnection`, `webkitRTCPeerConnection`, `Worker` and `navigator.sendBeacon` before the page's scripts run, and the page cannot put them back in that document. So a document cannot open a socket, an event stream, a WebTransport session, a WebRTC connection or a dedicated worker. A dedicated worker has its own global scope without the init script, so `Worker` is removed rather than filtered. The SDK has no resource context for WebRTC and no dedicated-worker request source kind, so the shim cannot filter those requests. Whether the resource filter sees WebSocket and EventSource requests is not tested. An `about:blank` frame that a script creates is known to get the init script in headless Edge, checked by hand; the lockdown harness does not test it. Worklets (`audioWorklet`, `paintWorklet`) were not checked.
- A second host, for example one for images, needs its own `AllowFrameHost` call. The Currencies page on stalburg.net loads one image from `licensebuttons.net`, so a page showing it must allow that host as well.
- Frames share the view's profile with the OS UI (`BepInEx\cache\ObxWebView`). The host's cookies are not isolated.
- Browser storage (`localStorage` and cookies) is one profile in `BepInEx\cache\ObxWebView`, so every save shares it. Keep per-save state in `SaveData`, not in a page's storage.
- Each frame navigation reports `obx.frame` (see Messages from the shim), for at most 64 navigations per view.

## Navigation

`Navigate` and `Open` take a relative path. It is refused when it:
- is empty or starts with `/` or `\`;
- contains `:`, `\`, `%` or `..`.

A refused path returns `null` from `Open` and `false` from `Navigate`. The shim also blocks navigation outside the view's mapped hosts (frames may also load the hosts named by `AllowFrameHost`, see Frame hosts), new windows, downloads, permission requests and external URI schemes.

## Shared assets

The WebUi module ships two shared asset files in `web\`, next to `ObenseuerCore.WebUi.dll`. Every view maps that folder as `https://obx-core.obx/`, so any page can link them. The `web\modmenu\` folder in it holds the Mods menu's own page. It is also reachable at `https://obx-core.obx/modmenu/` from any view, but it is not an API and may change between versions.

- `https://obx-core.obx/obx-theme.css`: design tokens and generic component classes. It has `:root` tokens for light, dark under `:root[data-theme="dark"]`, and dark under `prefers-color-scheme: dark` unless the page sets `data-theme="light"`. The tokens cover surfaces, text, muted text, lines and controls, amber, red, the two brand colours, fonts, letter spacing, font sizes, spacing, radii and motion. The component classes are `.obx-brand`, `.obx-btn` (with `primary`, `danger`, `ghost`, `sm`, `block`), `.obx-tabs` and `.obx-tab`, `.obx-card`, `.obx-label`, `.obx-value`, `.obx-table`, `.obx-input`, `.obx-badge` and `.obx-seg`. It has no global element rules and no game or OS chrome.
- `https://obx-core.obx/obx-base.css`: optional global rules. It sets `box-sizing`, `html`/`body` (no scroll, transparent background, no text selection), the `[hidden]` rule, focus outline, and the body transition. A page opts in by linking it. A page that does not link it keeps the browser's defaults. The transparent background does not show the game in thread mode, the default: each view is a separate popup window that paints black where the page draws nothing, so a transparent page shows black over the game. A page that can run in thread mode should give `html` or `body` its own background. In composition mode the page is drawn onto the game window with a transparent default, so the game shows through where the page draws nothing. Windowed mode is not covered.

A page that links `obx-theme.css` gets its colours from `data-theme`. The framework sets `data-theme` on `<html>` before the page runs, from `WebUi.Theme`. When the setting changes, the framework posts `{"v":1,"type":"theme","mode":"light"}` (or `"dark"`) to every view. A page should set the attribute from that message, as the example does.

The framework adds no Content-Security-Policy to mod pages, so a page should set its own `<meta http-equiv="Content-Security-Policy">`, as `webui/web/modmenu/index.html:5` does.

## Theme

- The initial `data-theme` comes from an init script that the framework registers on each view. It sets the attribute on `<html>` as soon as the element exists.
- A change is posted to the page as `{"v":1,"type":"theme","mode":"light|dark"}`. The framework also replaces each open view's init script with the new mode's, so a later navigation starts in that mode. A view keeps one such script. The framework does not change the attribute itself after the page loads.

## Messages from the shim

The framework reads the shim's queue each frame. Messages from the shim (`obx.key`, `obx.error`, `obx.status`) are not page messages. `obx.frame` is the exception: the framework posts it to the page unchanged. When the queue overflows, the dropped count is reported as `obx.error` `messages-dropped` and logged as a warning at most once a minute. A burst inside that minute is added to the next warning, so a count with no later burst is not logged.

- `obx.key`: a view gets Escape or F10. It goes to `KeyPressed`, and Escape may then hide the view (see [Step 7 of the UI guide](../guides/ui.md#step-7-hide-close-and-escape)).