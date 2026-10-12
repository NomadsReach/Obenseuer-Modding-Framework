# UiState API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. No patches, no owner, no `Init`: the facade only reads game state.
Files: `src/Api/UiState.cs`.

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
}
```

- All members are read-only properties. Each returns `false` when `GameController.instance` or `GameUIController.instance` is missing (main menu, loading, no save).
- `IsMenuOpen`: `GameUIController.IsMenuVisible` (`Core/GameUIController.cs:115`). It is also `true` while the wait or sleep timer is on screen. It does not cover the pause menu, shops or dialogue. Those are separate flags (see `CoreInput.GameBusy`).
- `IsPaused`: `GameController.GameIsPaused` (`Core/GameController.cs:7`). The pause menu, the saving screen and the load menu set it. It is not `GameTime.IsPaused`, which is the time scale.
- `IsInputBlocked`: `GameController.keysDisabled` (`Core/GameController.cs:9`). It is `true` while a `ControlsDisabled` call with the default `disableKeys` is active (cutscenes, dialogue, sleep and similar). An open game menu does not set it, because `ShowGameMenu` passes `disableKeys: false`. A mod that disables keys through `CoreInput` also sets it.
- `CursorVisible`: `GameController.cursorVisible` (`Core/GameController.cs:27`). This is the game's cursor flag. It does not include the controller-input and computer-screen cases that `UpdateCursorVisibility` adds.
- `InventoryDisabled`: `GameController.inventoryDisabled` (`Core/GameController.cs:13`). It stays `true` while any holder from `DisableInventory` has not called `EnableInventory`.

## Threading and safety

- Main thread only.
- Reads never throw and never write.

## Events and polling

There is no `Changed` event. The flags move through many game paths: `ControlsDisabled` and `ControlsEnabled` (`Core/GameController.cs:265`, `:324`) write `keysDisabled` and `cursorVisible`, `DisableInventory` and `EnableInventory` write `inventoryDisabled`, and `ShowGameMenu` and `HideGameMenu` (`Core/GameUIController.cs:240`, `:270`) change the menu state. One postfix would miss the others. Poll these properties instead, for example once per frame from a hotkey or overlay. Each read is a field read.
