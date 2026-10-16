---
title: UiState API
parent: API reference
nav_order: 86
---

# UiState API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.uistate`, used by the pause events only. The flag properties only read game state and have no patches.
Files: `src/Api/UiState.cs`, `src/Api/UiStatePatches.cs`.

Everything not listed here is internal.

## UiState

```csharp
public static class UiState
{
    public static bool IsMenuOpen { get; }
    public static bool IsPaused { get; }
    public static bool IsInputBlocked { get; }
    public static bool CursorVisible { get; }
    public static bool InventoryDisabled { get; }
    public static bool PauseEventsAvailable { get; }
    public static event Action<bool> PauseOpened;     // true when the pause screen is the death screen
    public static event Action PauseClosed;
}
```

- All flag members are read-only properties. Each returns `false` when `GameController.instance` or `GameUIController.instance` is missing (main menu, loading, no save).
- `IsMenuOpen`: `GameUIController.IsMenuVisible`. It is also `true` while the wait or sleep timer is on screen. It does not cover the pause menu, shops or dialogue. Those are separate flags.
- `IsPaused`: `GameController.GameIsPaused`. The pause menu, the saving screen and the load menu set it. Do not confuse it with `GameTime.IsPaused`, which reads `Time.timeScale == 0`: the pause menu and the saving screen set the time scale to 0 too, so `GameTime.IsPaused` is also `true` while either is open.
- `IsInputBlocked`: `GameController.keysDisabled`. It is `true` while a `ControlsDisabled` call with the default `disableKeys` is active (cutscenes, dialogue, sleep and similar). An open game menu does not set it, because `ShowGameMenu` passes `disableKeys: false`. The framework's own input lock also sets it.
- `CursorVisible`: `GameController.cursorVisible`. This is the game's cursor flag. It does not include the controller-input and computer-screen cases that `UpdateCursorVisibility` adds.
- `InventoryDisabled`: `GameController.inventoryDisabled`. It stays `true` while any holder from `DisableInventory` has not called `EnableInventory`.
- `PauseEventsAvailable` is `installed && !CorePatcher.IsDisabled(owner)`. It is `false` when the pause patches could not be installed, and then the two events never fire.

## Pause events

- `PauseOpened(bool isDeathScreen)` fires after the game's `PauseMenu.PauseMenuOpen` returns. The argument is `Death.instance.dead`, so it is `true` for the death screen. `PauseMenu.PausemenuDeath` calls the same open path after `dead` is set.
- `PauseOpened` can repeat. If the pause menu is already open when the player dies, `PausemenuDeath` runs the open path again with no close in between, so the handler gets a second `PauseOpened(true)`. The framework does not deduplicate it. A handler that must act once should keep its own flag.
- `PauseOpened` does not fire when `PauseMenuOpen` throws. On a scene with no `GameController` (the main menu), `PauseMenuOpen` throws at `PauseMenu` (`GameController.instance.ChangeCursorStatus`), before the postfix runs. The exception is the game's own; the framework does not catch it.
- `PauseClosed()` fires after `PauseMenu.PauseMenuClose` returns, and only when the menu was open before the close (`PauseIsActive`, `PauseMenu`, read in the prefix). In code, `PauseMenuClose` is called only from `PauseMenu.Resume`, and `CardGameUI` calls `Resume` only while the menu is open. A button bound to `Resume` in a scene is not visible in code.
- `PauseClosed` does not fire on quit to the main menu (`PauseMenu.QuitMainMenu`, `PauseMenu`) or quit to desktop (`PauseMenu.QuitDesktop`, `PauseMenu`), because neither calls `PauseMenuClose`. The scene load raises `Events.SceneLoaded` instead. Quit to desktop ends the process.
- Handlers run one at a time, so a handler that throws is logged once and the other handlers still run. The events are raised on the main thread from the game's pause code.
- The framework does not gate these events on `SaveData.Busy`, so a handler that should ignore a save or load can check `SaveData.Busy`.

## Threading and safety

- Main thread only.
- Reads never throw and never write.
- The pause patches never throw into the game: each postfix and prefix catches and logs once.

## Polling

There is no event for the flags above. They move through many game paths: `ControlsDisabled` and `ControlsEnabled` (`GameController`) write `keysDisabled` and `cursorVisible`, `DisableInventory` and `EnableInventory` write `inventoryDisabled`, and `ShowGameMenu` and `HideGameMenu` (`GameUIController`) change the menu state. One postfix would miss the others. Poll these properties instead, for example once per frame from a hotkey or overlay. Each read is a field read.
