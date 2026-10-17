---
title: Ship an item icon
parent: Cookbook
nav_order: 6
---

# Ship an item icon

Replace the icon of an item that is already in the game with a PNG picture that ships in your mod folder. Use it to give an existing item a new look, such as a re-drawn bottle.

`Items.Find` looks the item up by its title. `ItemIcons.OverrideVanilla` swaps its icon for your picture. The items load after the game starts, so the code waits with `Events.WhenReady` until a save has loaded.

1. Make a square PNG with a transparent background, 128 or 256 pixels on each side. Name it `bottle.png`.
2. Put it in an `icons` folder next to your DLL, so the path is `BepInEx\plugins\<name>\icons\bottle.png` in the game folder. `<name>` is your project's name, the folder the build made for your DLL. Run the build in step 4 first if the folder is not there yet, and make `icons` by hand, because the build copies only the DLL.
3. Replace the code in `Plugin.cs` of your project from [Your first mod](../getting-started/first-mod.md) with this code.

```csharp
using System.IO;
using System.Reflection;
using ObenseuerCore;

[ModInfo("example.icon", "Example Icon", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class ExampleIcon : FrameworkMod
{
    // The title of the item whose icon you change.
    private const string ItemTitle = "Empty Glass Bottle";

    protected override void OnLoad()
    {
        // Run ApplyIcon once a save has loaded, because items are not ready before that.
        Events.WhenReady(ApplyIcon);
    }

    private void ApplyIcon()
    {
        // Look the item up by title. null means the game has no item with that title.
        ItemDef item = Items.Find(ItemTitle);
        if (item == null) return;
        // The folder that holds your DLL.
        string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        // Swap the icon for icons\bottle.png in that folder.
        ItemIcons.OverrideVanilla(item.Id, Path.Combine(folder, "icons", "bottle.png"));
    }
}
```

4. Close the game, run `dotnet build -c Release -p:DeployToGame=true` and start the game. Load a save and open your inventory. The empty glass bottle has your picture.

Notes:

- The path must be absolute. `Assembly.GetExecutingAssembly().Location` gives one.
- `Events.WhenReady` runs the action now if a save is loaded or a new game's first level has loaded, or once on the next of those. The items must be loaded before `Items.Find` works.
- `ItemIcons.OverrideVanilla` returns `false` when the image cannot be read.
- Icons are not saved. Removing the mod brings back the game's icon.
- To add a new item with its own icon, see [Content](../content.md).

## Where to go next

You have finished the cookbook. To add new items, recipes or furniture, see [Content](../content.md). For the player, the world and money, see [Gameplay and economy](../gameplay.md).
