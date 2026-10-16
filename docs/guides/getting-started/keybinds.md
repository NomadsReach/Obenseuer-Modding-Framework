---
title: Keybinds
parent: Getting started
nav_order: 4
---

# Keybinds

Register a key once in `Awake`. The player can rebind it in the Mods menu.

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.hello", "Example Hello", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
public class ExampleHello : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.hello");
    private ModKeybind greetKey;

    private void Awake()
    {
        greetKey = Keybinds.Register("example.hello", "Greet", "Greet now", KeyCode.G);
    }

    private void Update()
    {
        if (greetKey == null || UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (greetKey.Down()) Log.Info("greet key pressed");
    }
}
```

On BepInEx 5, write `"0.11.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](packaging.md#supporting-bepinex-5-too).

- `Keybinds.Register(modId, id, displayName, defaultKey)` returns a `ModKeybind`, or `null` if the key cannot be created. Check for `null`.
- `Down()` is true on the frame the key is pressed. `Held()` is true while it is held.
- `Down()` and `Held()` do not check menus, dialogues or the pause screen. The code above checks `UiState` first.
- The key is saved in `BepInEx\config\ObenseuerCore.ModKeybinds.cfg`. The player changes it on the Mods menu page "Mod Keybinds".
- Two mods can use the same key. Each mod gets the press, and the framework logs one warning that names both mods and the key when the second one registers.
- To read a game action, such as Interact, use `Keybinds.Key("Interact")`.

Next: [Using events](using-events.md).
