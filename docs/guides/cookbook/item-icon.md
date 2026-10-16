---
title: Ship an item icon
parent: Cookbook
nav_order: 6
---

# Ship an item icon

Replace the icon of an existing item with a PNG that ships in your mod folder.

1. Make a square PNG with alpha, 128 or 256 pixels on each side. Name it `bottle.png`.
2. Put it in an `icons` folder next to your DLL, so the path is `BepInEx\plugins\ExampleIcon\icons\bottle.png`.

```csharp
using System.IO;
using System.Reflection;
using ObenseuerCore;

[ModInfo("example.icon", "Example Icon", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class ExampleIcon : FrameworkMod
{
    private const string ItemTitle = "Empty Glass Bottle";

    protected override void OnLoad()
    {
        Events.WhenReady(ApplyIcon);
    }

    private void ApplyIcon()
    {
        ItemDef item = Items.Find(ItemTitle);
        if (item == null) return;
        string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        ItemIcons.OverrideVanilla(item.Id, Path.Combine(folder, "icons", "bottle.png"));
    }
}
```

Replace the code in `Plugin.cs` of your project from [Your first mod](../getting-started/first-mod.md) with the code above. Then run `dotnet build -c Release -p:DeployToGame=true` and start the game.

Notes:

- The path must be absolute. `Assembly.GetExecutingAssembly().Location` gives one.
- `Events.WhenReady` runs the action now if a save is loaded or a new game's first level has loaded, or once on the next of those. The items must be loaded before `Items.Find` works.
- `ItemIcons.OverrideVanilla` returns `false` when the image cannot be read.
- Icons are not saved. Removing the mod brings back the game's icon.
- To add a new item with its own icon, see [Content](../content.md).
