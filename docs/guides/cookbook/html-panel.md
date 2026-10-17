---
title: Add an HTML panel
parent: Cookbook
nav_order: 5
---

# Add an HTML panel

Show a web page from your mod's `web` folder in a panel inside the game when the player presses F9. Use it when you want a screen that looks and works like a web page, with HTML and CSS. The panel needs the WebUi module, the part of the framework that shows web pages.

`WebUi.Open` opens a page and gives you a `WebView`, which is the open panel. The code keeps the `WebView` in a field, so pressing F9 again hides or shows the same panel.

1. Install the WebUi zip. See [Install](../../install.md#optional-webui).
2. Make a folder named `web` inside your project folder, and add a file named `index.html` to it. This one is enough to test:

```html
<!DOCTYPE html>
<html>
<body>
<h1>Hello from my mod</h1>
</body>
</html>
```

3. Open your `.csproj` file in a text editor. Find the `<ItemGroup>` that holds the other `<Reference>` lines, and add this reference inside it, before its closing `</ItemGroup>`. It points at the WebUi file that the WebUi zip put in the game. On BepInEx 5, use `ObenseuerCore.WebUi.BepInEx5.dll` instead. `Private` set to `false` keeps the build from copying that file into your output.

```xml
<Reference Include="ObenseuerCore.WebUi">
  <HintPath>$(GameDir)\BepInEx\plugins\ObenseuerCore.WebUi\ObenseuerCore.WebUi.dll</HintPath>
  <Private>false</Private>
</Reference>
```

4. Replace the code in `Plugin.cs` of your project from [Your first mod](../getting-started/first-mod.md) with this code.

```csharp
using System.IO;
using System.Reflection;
using ObenseuerCore;
using UnityEngine;

// The second ModDependency makes the framework skip this mod, with a warning, when WebUi is not installed.
[ModInfo("example.panel", "Panel", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
[ModDependency("obenseuer.moddingframework.webui", "0.11.0")]
public class Panel : FrameworkMod
{
    private const string ModId = "example.panel";
    // The open panel, or null before the first F9.
    private WebView view;
    // The F9 key.
    private ModKeybind openKey;

    protected override void OnLoad()
    {
        // Register the key: short id, name shown in the Mods menu, default key.
        openKey = Mod.Key("Open", "Open the panel", KeyCode.F9);
    }

    // Unity calls this once per frame.
    private void Update()
    {
        if (openKey == null || !openKey.Down()) return;
        // If the panel exists already, F9 hides it or shows it again.
        if (view != null && !view.IsClosed)
        {
            if (view.IsVisible) view.Hide();
            else view.Show();
            return;
        }
        // The native part of WebUi can be missing, so check before opening.
        if (!WebUi.IsAvailable)
        {
            Log.Warn("WebUi is unavailable: " + WebUi.LastError);
            return;
        }
        // The web folder sits next to your DLL.
        string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "web");
        // Open index.html from that folder. true means the panel takes the player's mouse and keys while it is open.
        view = WebUi.Open(ModId, folder, "index.html", true);
        if (view == null) Log.Warn("the view did not open: " + WebUi.LastError);
    }
}
```

5. Close the game and run `dotnet build -c Release -p:DeployToGame=true`. The build copies only the DLL, so copy your `web` folder into the same `BepInEx\plugins\<name>\` folder as the DLL, where `<name>` is your project's name. From a terminal in your project folder, this line does it, with your own game path in place of the example:

```
xcopy /E /I /Y web "C:\Program Files (x86)\Steam\steamapps\common\Obenseuer\BepInEx\plugins\MyFirstMod\web"
```

6. Start the game, load a save and press F9. The panel shows "Hello from my mod". Press F9 again to hide it.

Notes:

- `WebUi.Open` returns `null` when the view cannot open. `WebUi.LastError` says why.
- The page sends messages to the mod through `view.Message`, and the mod replies with `view.Post`. See [WebUi](../../api/webui.md).
- If the WebUi plugin is missing, the framework skips this mod and logs a warning, because of the `ModDependency` line. `WebUi.IsAvailable` is false when the native part is missing, so the code checks it first. See [WebUi is unavailable](../../faq.md#webui-is-unavailable).

## Where to go next

Next: [Ship an item icon](item-icon.md).
