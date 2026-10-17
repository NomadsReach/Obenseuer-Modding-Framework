---
title: Web UI
parent: Screen and UI
grand_parent: API reference
has_children: true
nav_order: 6
---

# Web UI

Status: experimental.

## What it is

WebUi lets your mod show a web page, written in HTML, CSS and JavaScript, inside the game. The page is shown in a view that the framework hosts with WebView2, which is the web browser part that Windows already has. Your mod opens the view with `WebUi.Open`, and then the mod and the page talk to each other by sending messages. This page is the reference for the `WebUi` and `WebView` classes. For a step-by-step walkthrough, start with the [UI guide](../guides/ui.md). If you would rather build the screen from game-style controls in C#, see the [Ui page](ui.md).

## When to use it

- Show a screen that is easier to write as a web page than as C# controls, such as a shop, a form or a menu.
- Send numbers from your mod to a page, and get button presses back.
- Put a page on a computer monitor in the game while the player uses the computer.
- Show a live website inside your page, for example the game's wiki, in a frame.
- Make your page follow the light or dark look the player chose.

## Quick example

This mod shows a page when you press F9, and hides it when you press F9 again. The page has a button. When you press the button, the page sends a message to your mod, and your mod sends an answer back.

Your project is a BepInEx plugin. It needs references to `ObenseuerCore.dll` and `ObenseuerCore.WebUi.dll`, and your build must copy the `web` folder next to your DLL. The [UI guide](../guides/ui.md) shows the project set-up step by step. Put this C# in your plugin.

```csharp
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.webuihello", "WebUiHello", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
[BepInDependency("obenseuer.moddingframework.webui", ">=0.11.0")]
public class WebUiHello : BaseUnityPlugin
{
    private const string ModId = "example.webuihello";
    private WebView view;

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
            Logger.LogWarning("WebUiHello: the view did not open: " + WebUi.LastError);
            return;
        }
        view.Message += OnMessage;
    }

    private void OnMessage(string json)
    {
        if (json.Contains("\"ping\"")) view.Post("{\"type\":\"pong\",\"at\":" + Time.frameCount + "}");
    }
}
```

Put this page in a file `web\index.html` next to your project file.

```html
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>WebUiHello</title>
<link rel="stylesheet" href="https://obx-core.obx/obx-theme.css">
<link rel="stylesheet" href="https://obx-core.obx/obx-base.css">
<style>
  html, body { background: #20242b; color: #eee; }
  body { display: flex; align-items: center; justify-content: center; }
</style>
</head>
<body>
<div>
  <button class="obx-btn primary" id="ping">Ping the mod</button>
  <div class="obx-label" id="reply">No reply yet.</div>
</div>
<script>
  const reply = document.getElementById("reply");
  document.getElementById("ping").addEventListener("click", () => {
    chrome.webview.postMessage({ type: "ping" });
  });
  chrome.webview.addEventListener("message", (e) => {
    if (e.data.type === "pong") reply.textContent = "The mod answered at frame " + e.data.at + ".";
  });
</script>
</body>
</html>
```

`modal` is `true` here, so the view takes over the player's controls while it is shown. `Message` is an event: the framework calls your code each time the page sends a message. The page sends one with `chrome.webview.postMessage`, and your mod sends one back with `Post`.
