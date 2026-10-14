---
title: Add an HTML panel
parent: Cookbook
nav_order: 5
---

# Add an HTML panel

Show a page from your mod's `web` folder on F9. The panel needs the WebUi module.

1. Install the WebUi zip. See [Install](../../install.md#optional-webui).
2. Make a `web` folder next to your DLL, and add an `index.html` file to it.
3. Add the WebUi DLL, `ObenseuerCore.WebUi.dll`, from the game's `BepInEx\plugins\ObenseuerCore.WebUi\` folder (`ObenseuerCore.WebUi.BepInEx5.dll` on BepInEx 5), and the game's `UnityEngine.InputLegacyModule.dll` (for `Input`) to the project as `Reference` items with `Private` set to `false`.
4. Build and copy the DLL and the `web` folder into `BepInEx\plugins\ExamplePanel\`.

```csharp
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.panel", "Panel", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.10.0")]
[BepInDependency("obenseuer.moddingframework.webui", ">=0.10.0")]
public class Panel : BaseUnityPlugin
{
    private const string ModId = "example.panel";
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
        if (!WebUi.IsAvailable)
        {
            Logger.LogWarning("WebUi is unavailable: " + WebUi.LastError);
            return;
        }
        string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "web");
        view = WebUi.Open(ModId, folder, "index.html", true);
        if (view == null) Logger.LogWarning("the view did not open: " + WebUi.LastError);
    }
}
```

On BepInEx 5, write `"0.10.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](../getting-started/packaging.md#supporting-bepinex-5-too).

Notes:

- `WebUi.Open` returns `null` when the view cannot open. `WebUi.LastError` says why.
- The page sends messages to the mod through `view.Message`, and the mod replies with `view.Post`. See [WebUi](../../api/webui.md).
- If WebUi is missing, the mod still loads. Check `WebUi.IsAvailable` first, as the code does. See [WebUi is unavailable](../../faq.md#webui-is-unavailable).
