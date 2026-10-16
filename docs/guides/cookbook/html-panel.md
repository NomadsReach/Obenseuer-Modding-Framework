---
title: Add an HTML panel
parent: Cookbook
nav_order: 5
---

# Add an HTML panel

Show a page from your mod's `web` folder on F9. The panel needs the WebUi module.

1. Install the WebUi zip. See [Install](../../install.md#optional-webui).
2. Make a `web` folder next to your DLL, and add an `index.html` file to it.
3. Add the WebUi DLL, `ObenseuerCore.WebUi.dll`, from the game's `BepInEx\plugins\ObenseuerCore.WebUi\` folder (`ObenseuerCore.WebUi.BepInEx5.dll` on BepInEx 5) to the project as a `Reference` item with `Private` set to `false`.

```csharp
using System.IO;
using System.Reflection;
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.panel", "Panel", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
[ModDependency("obenseuer.moddingframework.webui", "0.11.0")]
public class Panel : FrameworkMod
{
    private const string ModId = "example.panel";
    private WebView view;
    private ModKeybind openKey;

    protected override void OnLoad()
    {
        openKey = Mod.Key("Open", "Open the panel", KeyCode.F9);
    }

    private void Update()
    {
        if (openKey == null || !openKey.Down()) return;
        if (view != null && !view.IsClosed)
        {
            if (view.IsVisible) view.Hide();
            else view.Show();
            return;
        }
        if (!WebUi.IsAvailable)
        {
            Log.Warn("WebUi is unavailable: " + WebUi.LastError);
            return;
        }
        string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "web");
        view = WebUi.Open(ModId, folder, "index.html", true);
        if (view == null) Log.Warn("the view did not open: " + WebUi.LastError);
    }
}
```

Replace the code in `Plugin.cs` of your project from [Your first mod](../getting-started/first-mod.md) with the code above. Then run `dotnet build -c Release -p:DeployToGame=true`. Copy the `web` folder into the same `BepInEx\plugins\<name>\` folder as the DLL, then start the game.

Notes:

- `WebUi.Open` returns `null` when the view cannot open. `WebUi.LastError` says why.
- The page sends messages to the mod through `view.Message`, and the mod replies with `view.Post`. See [WebUi](../../api/webui.md).
- If the WebUi plugin is missing, the framework skips this mod and logs a warning, because of the `ModDependency` line. `WebUi.IsAvailable` is false when the native part is missing, so the code checks it first. See [WebUi is unavailable](../../faq.md#webui-is-unavailable).
