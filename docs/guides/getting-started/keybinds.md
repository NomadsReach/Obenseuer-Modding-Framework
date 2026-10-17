---
title: Keybinds
parent: Getting started
nav_order: 4
---

# Keybinds

A keybind is a keyboard key that makes your mod do something. You register it once when your mod loads, and the player can change the key in the Mods menu.

This page continues the project from [Your first mod](first-mod.md). Replace the code in `Plugin.cs` with the code below, then run `dotnet build -c Release -p:DeployToGame=true` with the game closed. `Mod.Key` registers the key. `Update` is a method that Unity, the game engine, calls on every frame, so it is where you check whether the key was pressed.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.hello", "Example Hello", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class ExampleHello : FrameworkMod
{
    // The key you registered. It stays null if the framework could not create it.
    private ModKeybind greetKey;

    protected override void OnLoad()
    {
        // Short id, name shown in the Mods menu, and the default key (G).
        greetKey = Mod.Key("Greet", "Greet now", KeyCode.G);
    }

    // Unity calls this once per frame.
    private void Update()
    {
        // Skip when there is no key, a menu is open, the game is paused, or the game has switched its keys off.
        if (greetKey == null || UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        // True only on the frame the key goes down.
        if (greetKey.Down()) Log.Info("greet key pressed");
    }
}
```

Start the game, load a save and press G. Then open `BepInEx\logs\example.hello.log` in the game folder and look for `greet key pressed`.

- **`Mod.Key(id, displayName, defaultKey)` gives you a `ModKeybind`.** It returns `null` if the key cannot be created, so check for `null` before you use it, as the code above does.
- **`Down()` is true on the frame the key is pressed. `Held()` is true while it is held.** Use `Down()` for one action per press.
- **`Down()` and `Held()` do not check menus, dialogues or the pause screen.** Your mod would react to the key while a menu is open. The code above checks `UiState` first to avoid that.
- **The player changes the key on the Mods menu page "Mod Keybinds".** The choice is saved in `BepInEx\config\ObenseuerCore.ModKeybinds.cfg`.
- **Two mods can use the same key.** Each mod gets the press. The framework logs one warning that names both mods and the key when the second one registers.
- **To read a game action, such as Interact, use `Keybinds.Key("Interact")`.** That gives the `KeyCode` the player has set for the game's own action.
  - Technical: `Mod.Key` calls `Keybinds.Register(modId, id, displayName, defaultKey)` with your mod's id.

## Where to go next

Next: [Using events](using-events.md).
