---
title: Add a Mods menu page
parent: Cookbook
nav_order: 4
---

# Add a Mods menu page

Add a page to the Mods menu with a title, a line of text and a button.

1. Add references to `Unity.TextMeshPro.dll` and `UnityEngine.UI.dll` from the game's `Obenseuer_Data\Managed` folder, with `Private` set to `false`. `ModUi` needs them.

```csharp
using ObenseuerCore;
using ObenseuerCore.Ui;
using UnityEngine;

[ModInfo("example.menupage", "Menu Page", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class MenuPage : FrameworkMod
{
    protected override void OnLoad()
    {
        ModMenu.AddPage("example.menupage", "Example Page", BuildPage);
    }

    private void BuildPage(RectTransform parent)
    {
        ModUi.Title(parent, "Example Page");
        ModUi.Text(parent, "This page belongs to the example mod.", false);
        ModUi.Button(parent, "Log a click", ButtonStyle.Primary, () => Log.Info("menu page button clicked"));
    }
}
```

Replace the code in `Plugin.cs` of your project from [Your first mod](../getting-started/first-mod.md) with the code above. Then run `dotnet build -c Release -p:DeployToGame=true` and start the game.

Notes:

- `ModMenu.AddPage` takes the mod id, the page title and a method that builds the page. The method runs when the player opens the page.
- Other controls are in [UI and HTML views](../ui.md) and the [UI reference](../../api/ui.md).
- Config entries show up in the Mods menu on their own. A page is only needed for your own controls. See [Config](../getting-started/config.md).
