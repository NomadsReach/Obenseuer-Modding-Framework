---
title: Settings
parent: Framework basics
grand_parent: API reference
has_children: true
nav_order: 10
---

# Settings

Status: experimental.

## What it is

This API lets a mod read the options a player has chosen: display, graphics quality, mouse, and sprint and crouch mode. It also reads which keys the player has bound to the game's actions, lets a mod register keys of its own, and reads the language the game is set to. The only game option a mod can change is the field of view.

## When to use it

- Show a prompt such as "Press [E]" with the key the player really uses, whatever they rebound it to.
- Add a hotkey for your mod that the player can rebind in the Mods menu.
- Adjust your mod to the player's options, for example the screen size or the sprint mode.
- Check whether the card game's hints are switched on with `HintsEnabled()`. The player turns them on and off with the Hints key (K by default). The game has no hints option in its settings menu.
- Show your text in the language the player picked.

## Quick example

This mod registers a key, logs the player's screen size and language once a save is loaded, and logs a line when the key is pressed.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.settings", "Settings Demo", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class SettingsDemo : FrameworkMod
{
    private ModKeybind greetKey;

    protected override void OnLoad()
    {
        greetKey = Mod.Key("Greet", "Say hello", KeyCode.F8);
        Events.WhenReady(OnReady);
    }

    private void OnReady()
    {
        GameDisplay display = GameSettings.Display();
        if (display != null) Log.Info("screen " + display.Width + "x" + display.Height + ", " + display.ScreenMode);
        Log.Info("language " + Localization.Language() + ", interact key " + Keybinds.Label("Interact"));
    }

    private void Update()
    {
        if (greetKey == null || UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (greetKey.Down()) Log.Info("hello, hints are " + (GameSettings.HintsEnabled() ? "on" : "off"));
    }
}
```

`Mod.Key` registers a key that the player can change in the Mods menu. `Down()` is `true` on the frame the key is pressed. The `UiState` check stops your key from firing while a menu is open. See the [keybinds guide](../guides/getting-started/keybinds.md) for more on keys, and [Using events](../guides/getting-started/using-events.md) for `WhenReady`.
