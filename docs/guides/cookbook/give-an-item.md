---
title: Give an item
parent: Cookbook
nav_order: 1
---

# Give an item

Press F7 to give the player one item. This recipe finds the item by its title, the name the player sees in the game. Use it when your mod needs to hand the player something.

The mod registers a key with `Mod.Key`, then checks it every frame in `Update`, a method Unity (the game engine) calls on every frame. `Items.Find` looks the item up by title, and `PlayerInventory.Give` puts it in the player's inventory.

Set `ItemTitle` to the item's title. Capital letters do not matter, but the spelling must match the game's item list.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.giveitem", "Give Item", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class GiveItem : FrameworkMod
{
    // The title of the item to give.
    private const string ItemTitle = "Empty Glass Bottle";
    // The F7 key. It stays null if it could not be created.
    private ModKeybind giveKey;

    protected override void OnLoad()
    {
        // Register the key: short id, name shown in the Mods menu, default key.
        giveKey = Mod.Key("Give", "Give the item", KeyCode.F7);
    }

    // Unity calls this once per frame.
    private void Update()
    {
        // Skip when there is no key, a menu is open, the game is paused, or the game has switched its keys off.
        if (giveKey == null || UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        // True only on the frame the key goes down.
        if (!giveKey.Down()) return;
        // Look the item up by title, then add one to the player's inventory.
        ItemDef item = Items.Find(ItemTitle);
        if (item == null || !PlayerInventory.Give(item.Id, 1)) Log.Warn("could not give " + ItemTitle);
    }
}
```

Replace the code in `Plugin.cs` of your project from [Your first mod](../getting-started/first-mod.md) with the code above. Then run `dotnet build -c Release -p:DeployToGame=true` with the game closed, and start the game. Load a save and press F7. The item appears in your inventory. If it does not, open `BepInEx\logs\example.giveitem.log` in the game folder.

Notes:

- `Items.Find` returns `null` for an unknown title. `Items.Search` finds titles that contain some text.
- `PlayerInventory.Give` returns `false` when the main grid is full, or while a save runs.
- The item id is also in `Obenseuer_Data\StreamingAssets\Items.json`. See [Finding item ids](../content.md#finding-item-ids).

## Where to go next

Next: [Pay the player](pay-the-player.md).
