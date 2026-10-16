---
title: Add a Mods menu page
parent: Cookbook
nav_order: 4
---

# Add a Mods menu page

Add a page to the Mods menu with a title, a line of text and a button.

1. Add references to `Unity.TextMeshPro.dll` and `UnityEngine.UI.dll` from the game's `Obenseuer_Data\Managed` folder, with `Private` set to `false`. `ModUi` needs them.
2. Build and copy the DLL, as in [Your first mod](../getting-started/first-mod.md).

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;
using ObenseuerCore.Ui;
using UnityEngine;

[BepInPlugin("example.menupage", "Menu Page", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
public class MenuPage : BaseUnityPlugin
{
    private void Awake()
    {
        ModMenu.AddPage("example.menupage", "Example Page", BuildPage);
    }

    private void BuildPage(RectTransform parent)
    {
        ModUi.Title(parent, "Example Page");
        ModUi.Text(parent, "This page belongs to the example mod.", false);
        ModUi.Button(parent, "Log a click", ButtonStyle.Primary, () => Logger.LogInfo("menu page button clicked"));
    }
}
```

On BepInEx 5, write `"0.11.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](../getting-started/packaging.md#supporting-bepinex-5-too).

Notes:

- `ModMenu.AddPage` takes the mod id, the page title and a method that builds the page. The method runs when the player opens the page.
- Other controls are in [UI and HTML views](../ui.md) and the [UI reference](../../api/ui.md).
- Config entries show up in the Mods menu on their own. A page is only needed for your own controls. See [Config](../getting-started/config.md).
