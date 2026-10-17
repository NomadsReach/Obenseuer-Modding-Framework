---
title: Add a Mods menu page
parent: Cookbook
nav_order: 4
---

# Add a Mods menu page

Add a page to the Mods menu (the "MODS" button on the main menu) with a title, a line of text and a button. Use it when your mod needs controls of its own that a plain setting cannot give.

`ModMenu.AddPage` registers the page. The method you give it builds the page when the player opens it, using `ModUi` helpers. `ModUi` draws with the game's text and button types, so your project needs two more references first.

1. Open your `.csproj` file in a text editor. Find the `<ItemGroup>` that holds the other `<Reference>` lines, and add these two references inside it, before its closing `</ItemGroup>`. They point at game files in the `Managed` folder. `Private` set to `false` keeps the build from copying the game's files into your output.

```xml
<Reference Include="Unity.TextMeshPro">
  <HintPath>$(ManagedDir)\Unity.TextMeshPro.dll</HintPath>
  <Private>false</Private>
</Reference>
<Reference Include="UnityEngine.UI">
  <HintPath>$(ManagedDir)\UnityEngine.UI.dll</HintPath>
  <Private>false</Private>
</Reference>
```

2. Replace the code in `Plugin.cs` of your project from [Your first mod](../getting-started/first-mod.md) with this code.

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
        // Add a page: your mod id, the page title, and the method that builds the page.
        ModMenu.AddPage("example.menupage", "Example Page", BuildPage);
    }

    // Runs when the player opens the page. "parent" is the empty page to fill.
    private void BuildPage(RectTransform parent)
    {
        // A heading.
        ModUi.Title(parent, "Example Page");
        // A line of text. false means normal text, true would make it dim.
        ModUi.Text(parent, "This page belongs to the example mod.", false);
        // A button. Clicking it writes a line to this mod's log.
        ModUi.Button(parent, "Log a click", ButtonStyle.Primary, () => Log.Info("menu page button clicked"));
    }
}
```

3. Run `dotnet build -c Release -p:DeployToGame=true` with the game closed, then start the game. Open the Mods menu and find Menu Page in the list. Click Log a click, then check `BepInEx\logs\example.menupage.log` in the game folder.

Pages added with `ModMenu.AddPage` are classic only. The build method runs in the classic uGUI window, whether the player opens the page from the classic sidebar or from the WebUi menu, which opens the classic pane for a page it does not render itself. A page never renders as HTML. Keep a classic page to labels, buttons and the `ModUi` kit. A mod that needs a rich control, such as a searchable dropdown, a colour picker or multi-line text, builds that in the WebUi menu instead, see [the WebUi menu](../../api/webui.md).

Notes:

- `ModMenu.AddPage` takes the mod id, the page title and a method that builds the page. The method runs when the player opens the page.
- Other controls are in [UI and HTML views](../ui.md) and the [UI reference](../../api/ui.md).
- Config entries show up in the Mods menu on their own. A page is only needed for your own controls. See [Config](../getting-started/config.md).

## Where to go next

Next: [Add an HTML panel](html-panel.md).
- The classic window is played with a controller as well as a mouse: the d-pad and the left stick move the focus, A or Enter activates the focused control, B or Esc goes back, and the bumpers switch sidebar pages. The footer shows the keys. `ModUi` controls are reachable this way; a raw `Selectable` that the page builds itself is not.
