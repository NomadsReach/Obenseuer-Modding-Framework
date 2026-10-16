---
title: Give an item
parent: Cookbook
nav_order: 1
---

# Give an item

Press F7 to give the player one item. The item is found by its title.

1. Set `ItemTitle` to the exact title. The title must match the game's list.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.giveitem", "Give Item", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class GiveItem : FrameworkMod
{
    private const string ItemTitle = "Empty Glass Bottle";
    private ModKeybind giveKey;

    protected override void OnLoad()
    {
        giveKey = Mod.Key("Give", "Give the item", KeyCode.F7);
    }

    private void Update()
    {
        if (giveKey == null || UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (!giveKey.Down()) return;
        ItemDef item = Items.Find(ItemTitle);
        if (item == null || !PlayerInventory.Give(item.Id, 1)) Log.Warn("could not give " + ItemTitle);
    }
}
```

Replace the code in `Plugin.cs` of your project from [Your first mod](../getting-started/first-mod.md) with the code above. Then run `dotnet build -c Release -p:DeployToGame=true` and start the game.

Notes:

- `Items.Find` returns `null` for an unknown title. `Items.Search` finds titles that contain some text.
- `PlayerInventory.Give` returns `false` when the main grid is full, or while a save runs.
- The item id is also in `Obenseuer_Data\StreamingAssets\Items.json`. See [Finding item ids](../content.md#finding-item-ids).
