---
title: Give an item
parent: Cookbook
nav_order: 1
---

# Give an item

Press F7 to give the player one item. The item is found by its title.

1. Set `ItemTitle` to the exact title. The title must match the game's list.
2. Build and copy the DLL, as in [Your first mod](../getting-started/first-mod.md).

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.giveitem", "Give Item", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
public class GiveItem : BaseUnityPlugin
{
    private const string ItemTitle = "Empty Glass Bottle";
    private ModKeybind giveKey;

    private void Awake()
    {
        giveKey = CoreMod.For(this).Key("Give", "Give the item", KeyCode.F7);
    }

    private void Update()
    {
        if (giveKey == null || UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (!giveKey.Down()) return;
        ItemDef item = Items.Find(ItemTitle);
        if (item == null || !PlayerInventory.Give(item.Id, 1)) Logger.LogWarning("could not give " + ItemTitle);
    }
}
```

On BepInEx 5, write `"0.11.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](../getting-started/packaging.md#supporting-bepinex-5-too).

Notes:

- `Items.Find` returns `null` for an unknown title. `Items.Search` finds titles that contain some text.
- `PlayerInventory.Give` returns `false` when the main grid is full, or while a save runs.
- The item id is also in `Obenseuer_Data\StreamingAssets\Items.json`. See [Finding item ids](../content.md#finding-item-ids).
