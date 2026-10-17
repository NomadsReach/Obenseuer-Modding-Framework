---
title: The WebView class
parent: Web UI
grand_parent: Screen and UI
nav_order: 4
---

# The WebView class

A `WebView` is one open page. You get it from `WebUi.Open`.

```csharp
public sealed class WebView
{
    public string ModId { get; }          // the mod id the view was opened with
    public string Origin { get; }         // the start of every web address this view loads, such as https://example-webuihello.obx/
    public bool IsVisible { get; }        // true when the view is shown
    public bool IsClosed { get; }         // true once the view is closed for good
    public bool Modal { get; }            // true when the view takes over the player's controls
    public bool EscapeHides { get; set; }   // true to hide the view when the player presses Escape
    public event Action<string> Message;    // the page sent a message; you get its text
    public event Action<int> KeyPressed;    // Escape or F10 was pressed in the view; you get the key code
    public event Action Dismissed;          // the framework hid the view because of Escape
    public bool Show();                    // shows the view
    public bool Hide();                    // hides the view
    public bool SetBounds(int x, int y, int width, int height);   // places the view at a spot on the screen
    public bool FullWindow();              // makes the view cover the whole game window
    public bool PinTo(RectTransform target);   // makes the view cover a screen in the game, such as a monitor
    public bool Navigate(string page);     // loads another page from your folder
    public bool MapFolder(string name, string folder);   // makes another folder of yours available
    public bool AllowFrameHost(string host);   // lets frames in your page load one live website
    public bool Post(string json);         // sends a message to the page
    public void Close();                   // closes the view for good
}
```

- **`Origin` is the start of every web address this view loads**, such as `https://<host>/`.
- **`Modal` is set when you call `Open`.**
- **`Show()` makes the view visible at its place.** A modal view gives `false` while the game is busy, as listed under `Open`. A modal view that shows also takes over the player's controls and shows the cursor (see [Step 2 of the UI guide](../../guides/ui.md#step-2-open-the-view)).
- **A modal view that never says it is ready is closed after 20 seconds.** The page does not have to do anything for this. The shim says it by itself once it has set the view up, whether or not the page has loaded, so a plain page needs no code for it. If it never does, the view is closed and an error is logged.
  - Technical: the shim sends `obx.status`. The framework closes a modal view that gets none within 20 seconds of showing.
- **`Hide()` hides the view, and a modal view gives the controls back.**
- **`SetBounds(x, y, width, height)` places the view, in pixels of the game window, and it stops the view from covering the whole window.** It gives `false` for a width or height of zero or less.
- **`FullWindow()` makes the view cover the whole game window.** This is how a view starts. It follows the window each frame, and only tells the shim when the size changed.
- **`PinTo(target)` makes the view cover a `RectTransform` on the screen instead of the whole window.** See [Pinning](pinning.md#pinning). `null` lets go and goes back to the whole window. It gives `false` when the view is closed or you are not calling from your mod's normal code.
- **`Navigate(page)` loads another page from your folder.** The path has to be relative (see [Navigation](navigation.md#navigation)). If the page cannot be loaded, the view keeps its current page. How you hear about the failure depends on the mode: in composition and windowed modes, `Navigate` gives `false` and sets `LastError`. In thread mode, `Navigate` gives `true` at once, and the shim loads the page on its own thread, so a failure arrives later as an error report with `where` set to `Obx_Navigate`.
- **`MapFolder(name, folder)` makes an extra folder of yours available at its own web address.** For example, `frames` gives `frames.example-webuihello.obx`. The address of an extra folder is the name, then a dot, then your view's host. Names are turned into a safe form the same way as mod ids: a name of at most 63 lowercase letters and digits with single dots between them has each dot turned into `-`, so `frames` stays `frames`. Any other name, such as `Frames`, gets an `h--` form, so `Frames` and `frames` get different addresses.
  - The extra folder can load content, but its pages cannot send messages to your mod. Only the page at your view's own host can.
  - It gives `false` for a view that is closed, a call that is not from your mod's normal code, an empty `name`, or a `folder` that does not exist.
  - On an open view, a folder that the shim cannot map is not kept, so the address stays refused. In thread mode, `MapFolder` gives `true` at once and the shim does it on its own thread, so a failure arrives later as an error report with `where` set to `Obx_MapFolder`, and `LastError` is set.
  - Technical: the shim accepts messages only from the view's own host.
- **`AllowFrameHost(host)` lets frames in your page load one live website, such as `stalburg.net`.** See [Frame hosts](frame-hosts.md#frame-hosts). It gives `false` and sets `LastError` when the host is not allowed. You can call it before or after the page loads.
- **`Post(json)` sends a JSON object to the page.** The page gets it in its `chrome.webview` `message` event. It gives `false` for a view that is closed, a call that is not from your mod's normal code, a `null` text, or when the shim call fails. The message is queued for the shim, so a message that you queue before the web core is ready is lost. Send it from inside a `Message` handler, or after the page has sent its first message.
- **`Message` gives you the text of a message the page sent with `chrome.webview.postMessage`.** You get the raw JSON text, not the parts. A message longer than 16384 characters is dropped. A message whose `type` starts with `obx.` is dropped, because those names belong to the shim. A message with no `type`, or one that is not a JSON object, still reaches you as raw text.
- **`KeyPressed` gives you the key code when Escape (27) or F10 (121) is pressed in the view.** Your handlers run first. Then Escape may hide the view (see [Step 7 of the UI guide](../../guides/ui.md#step-7-hide-close-and-escape)).
  - Technical: the code is the Windows virtual-key code that the shim forwards.
- **`Dismissed` fires right after the framework hides the view because of Escape.** See [Step 7 of the UI guide](../../guides/ui.md#step-7-hide-close-and-escape). It does not fire when your mod calls `Hide`, and not when a scene load that replaces the whole scene hides a modal view.
- **`Close()` hides the view, destroys it and gives the controls back.** A closed view stays closed. Call `Open` for a new one.
