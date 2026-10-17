---
title: Pinning
parent: Web UI
grand_parent: Screen and UI
nav_order: 5
---

# Pinning

Pinning puts a view on a screen inside the game, such as a computer monitor, instead of over the whole window.

`PinTo(target)` places the view over the screen area of `target` as the camera sees it. Each frame the view follows the camera, so it stays on a monitor while the camera moves in. `ComputerApps.ActiveScreen` gives you the monitor's `target` while the player uses a computer:

```csharp
WebView view = WebUi.Open("example.myapp", folder, "index.html", false);
if (view == null) return;
view.Hide();
ComputerApps.ScreenChanged += s => { if (s != null) { view.PinTo(s); view.Show(); } else view.Hide(); };
```

- **The view is a flat rectangle drawn over the monitor, not inside it.** It looks like part of the monitor only while the camera faces the screen. When the target is off the screen, behind the camera, or switched off, the view is hidden, but `IsVisible` stays `true`.
- **The rectangle is the inner part of the target.** This keeps a slightly angled view from spilling over the monitor's frame. The view is hidden when that rectangle is under 64 pixels on either side.
- **`SetBounds` and `FullWindow` are overwritten on the next frame while a view is pinned.**
- **A modal view pinned to a computer screen does not lock the controls itself.** The computer mode already holds them, so `Show` skips the busy check while the view is pinned. Pinning a view that already holds the lock lets the lock go.
- **`Close` unpins the view.**
