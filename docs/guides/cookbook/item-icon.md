---
title: Ship an item icon
parent: Cookbook
nav_order: 6
---

# Ship an item icon

Replace the icon of an existing item with a PNG that ships in your mod folder.

1. Make a square PNG with alpha, 128 or 256 pixels on each side. Name it `bottle.png`.
2. Put it in an `icons` folder next to your DLL, so the path is `BepInEx\plugins\ExampleIcon\icons\bottle.png`.
3. Build and copy the DLL, as in [Your first mod](../getting-started/first-mod.md).

```csharp
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

[BepInPlugin("example.icon", "Example Icon", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
public class ExampleIcon : BaseUnityPlugin
{
    private const string ItemTitle = "Empty Glass Bottle";

    private void Awake()
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

On BepInEx 5, write `"0.11.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](../getting-started/packaging.md#supporting-bepinex-5-too).

Notes:

- The path must be absolute. `Assembly.GetExecutingAssembly().Location` gives one.
- `Events.WhenReady` runs the action now if a save is loaded or a new game's first level has loaded, or once on the next of those. The items must be loaded before `Items.Find` works.
- `ItemIcons.OverrideVanilla` returns `false` when the image cannot be read.
- Icons are not saved. Removing the mod brings back the game's icon.
- To add a new item with its own icon, see [Content](../content.md).
