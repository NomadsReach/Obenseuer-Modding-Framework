---
title: The WebView class
parent: Web UI
grand_parent: Screen and UI
nav_order: 4
---

# The WebView class

A `WebView` is one open page. You get it from `WebUi.Open`.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WebView</code> <span>one open page</span></p>
<div class="api-pair" markdown="1">

```csharp
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

| Property | Type | What it means |
|---|---|---|
| `ModId` | `string` | The mod id the view was opened with. |
| `Origin` | `string` | The start of every web address this view loads, such as `https://example-webuihello.obx/`. |
| `IsVisible` | `bool` | `true` when the view is shown. |
| `IsClosed` | `bool` | `true` once the view is closed for good. |
| `Modal` | `bool` | `true` when the view takes over the player's controls. |
| `EscapeHides` | `bool` | Set it to `true` to hide the view when the player presses Escape. |

| Method | Returns | What it does |
|---|---|---|
| `Show()` | `bool` | Shows the view at its place. |
| `Hide()` | `bool` | Hides the view. |
| `SetBounds(int x, int y, int width, int height)` | `bool` | Places the view at a spot on the screen, in pixels of the game window. It stops the view from covering the whole window. |
| `FullWindow()` | `bool` | Makes the view cover the whole game window. |
| `PinTo(RectTransform target)` | `bool` | Makes the view cover a `RectTransform` on the screen, such as a monitor. `null` lets go and goes back to the whole window. |
| `Navigate(string page)` | `bool` | Loads another page from your folder. |
| `MapFolder(string name, string folder)` | `bool` | Makes another folder of yours available at its own web address. |
| `AllowFrameHost(string host)` | `bool` | Lets frames in your page load one live website. |
| `Post(string json)` | `bool` | Sends a JSON object to the page. |
| `Close()` | `void` | Closes the view for good. |

| Event | Gives you | When it fires |
|---|---|---|
| `Message` | `string`, the raw JSON text | The page sent a message with `chrome.webview.postMessage`. |
| `KeyPressed` | `int`, the key code | Escape (27) or F10 (121) was pressed in the view. |
| `Dismissed` | Nothing | The framework hid the view because of Escape. |

</div>
</div>

## Things to know

- `Modal` is set when you call `Open`.
- `Show()` gives `false` for a modal view while the game is busy, as listed under `Open` in [The WebUi class](the-webui-class.md). A modal view that shows also takes over the player's controls and shows the cursor (see [Step 2 of the UI guide](../../guides/ui.md#step-2-open-the-view)).
- A modal view that never says it is ready is closed after 20 seconds. The page does not have to do anything for this. The shim says it by itself once it has set the view up, whether or not the page has loaded, so a plain page needs no code for it. If it never does, the view is closed and an error is logged. <span class="tech">(The shim sends `obx.status`. The framework closes a modal view that gets none within 20 seconds of showing.)</span>
- `Hide()` hides the view, and a modal view gives the controls back.
- `Close()` hides the view, destroys it and gives the controls back. A closed view stays closed. Call `Open` for a new one.
- `SetBounds` gives `false` for a width or height of zero or less.
- `FullWindow()` is how a view starts. It follows the window each frame, and only tells the shim when the size changed.
- `PinTo(target)` gives `false` when the view is closed or you are not calling from your mod's normal code. See [Pinning](pinning.md#pinning).
- `Navigate(page)` loads another page from your folder. The path has to be relative (see [Navigation](navigation.md#navigation)). If the page cannot be loaded, the view keeps its current page.
- `MapFolder` gives an extra folder of yours its own web address. For example, `frames` gives `frames.example-webuihello.obx`. The address of an extra folder is the name, then a dot, then your view's host.
- Names are turned into a safe form the same way as mod ids. A name of at most 63 lowercase letters and digits with single dots between them has each dot turned into `-`, so `frames` stays `frames`. Any other name, such as `Frames`, gets an `h--` form, so `Frames` and `frames` get different addresses.
- `MapFolder` gives `false` for a view that is closed, a call that is not from your mod's normal code, an empty `name`, or a `folder` that does not exist.
- `AllowFrameHost(host)` lets frames in your page load one live website, such as `stalburg.net`. See [Frame hosts](frame-hosts.md#frame-hosts). It gives `false` and sets `LastError` when the host is not allowed. You can call it before or after the page loads.
- `Post(json)` sends a JSON object to the page. The page gets it in its `chrome.webview` `message` event. It gives `false` for a view that is closed, a call that is not from your mod's normal code, a `null` text, or when the shim call fails.
- `Message` gives you the raw JSON text, not the parts. A message with no `type`, or one that is not a JSON object, still reaches you as raw text.
- `KeyPressed` runs your handlers first. Then Escape may hide the view (see [Step 7 of the UI guide](../../guides/ui.md#step-7-hide-close-and-escape)). <span class="tech">(The code is the Windows virtual-key code that the shim forwards.)</span>
- `Dismissed` fires right after the framework hides the view because of Escape. See [Step 7 of the UI guide](../../guides/ui.md#step-7-hide-close-and-escape). It does not fire when your mod calls `Hide`, and not when a scene load that replaces the whole scene hides a modal view.

## Limits

- A message longer than 16384 characters is dropped. A message whose `type` starts with `obx.` is dropped too, because those names belong to the shim.
- `Post` queues the message for the shim, so a message that you queue before the web core is ready is lost. Send it from inside a `Message` handler, or after the page has sent its first message.
- In composition and windowed modes, a `Navigate` that fails gives `false` and sets `LastError`. In thread mode, `Navigate` gives `true` at once, and the shim loads the page on its own thread, so a failure arrives later as an error report with `where` set to `Obx_Navigate`.
- The extra folder from `MapFolder` can load content, but its pages cannot send messages to your mod. Only the page at your view's own host can. <span class="tech">(The shim accepts messages only from the view's own host.)</span>
- On an open view, a folder that the shim cannot map is not kept, so the address stays refused. In thread mode, `MapFolder` gives `true` at once and the shim does it on its own thread, so a failure arrives later as an error report with `where` set to `Obx_MapFolder`, and `LastError` is set.
