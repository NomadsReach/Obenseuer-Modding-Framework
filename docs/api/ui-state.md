---
title: UI state
parent: Screen and UI
grand_parent: API reference
nav_order: 5
---

# UI state

Status: experimental.

## What it is

While the player plays, the game is often busy with something other than the world: a menu is open, the game is paused, a cutscene has taken the controls, or the inventory is locked. This API lets your mod ask about those states, so it does not react to a key press while the player is in a menu. It also tells you when the pause screen opens and closes.

## When to use it

- Ignore your mod's hotkey while a menu is open, the game is paused, or the keys are blocked.
- Hide your own on-screen display while the pause screen is open.
- React when the player pauses the game, dies and sees the death screen, or resumes.

## Quick example

This mod writes a log line when the pause screen opens and closes. Its `Update` method runs every frame, so it also logs when a menu opens or closes.

```csharp
using ObenseuerCore;

[ModInfo("example.uiwatch", "UI Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class UiWatch : FrameworkMod
{
    private bool menuWasOpen;

    protected override void OnLoad()
    {
        UiState.PauseOpened += death => Log.Info(death ? "The death screen opened." : "The pause screen opened.");
        UiState.PauseClosed += () => Log.Info("The pause screen closed.");
    }

    private void Update()
    {
        bool menuOpen = UiState.IsMenuOpen;
        if (menuOpen == menuWasOpen) return;
        menuWasOpen = menuOpen;
        Log.Info(menuOpen ? "A menu opened." : "A menu closed.");
    }
}
```

`PauseOpened` and `PauseClosed` are events: the framework calls your code each time they happen. `Update` is the method the game calls once for every frame. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

Only react to a key press when the player is really playing.

```csharp
bool CanAct()
{
    return !UiState.IsMenuOpen && !UiState.IsPaused && !UiState.IsInputBlocked;
}
```

Check whether the player can open the inventory right now.

```csharp
bool inventoryLocked = UiState.InventoryDisabled;
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>UiState</code> <span>menu, pause and input flags</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class UiState
{
    public static bool IsMenuOpen { get; }
    public static bool IsPaused { get; }
    public static bool IsInputBlocked { get; }
    public static bool CursorVisible { get; }
    public static bool InventoryDisabled { get; }
    public static bool PauseEventsAvailable { get; }
    public static event Action<bool> PauseOpened;
    public static event Action PauseClosed;
}
```

| Property | Type | What it means |
|---|---|---|
| `IsMenuOpen` | `bool` | `true` when a game menu is open. |
| `IsPaused` | `bool` | `true` when the game is paused. |
| `IsInputBlocked` | `bool` | `true` when the player's keys are switched off. |
| `CursorVisible` | `bool` | `true` when the game shows the mouse cursor. |
| `InventoryDisabled` | `bool` | `true` when something has locked the inventory. |
| `PauseEventsAvailable` | `bool` | `false` when the pause events could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `PauseOpened` | `bool` | The pause screen opened. The value is `true` when it is the death screen. |
| `PauseClosed` | Nothing. | The player left the pause screen and went back to the game. |

</div>
</div>

### Things to know

- Every flag is read only, and each one is `false` when there is no game to ask. On the main menu, while a level is loading, or with no save loaded, all five flags are `false`. Reading them never changes anything and never throws an error. <span class="tech">(Each returns `false` when `GameController.instance` or `GameUIController.instance` is missing.)</span>
- `IsMenuOpen` is `true` while a game menu is open, and also while the wait or sleep timer is on screen. It does not cover the pause menu, shops or dialogue. Those have their own states. <span class="tech">(It reads `GameUIController.IsMenuVisible`, which also checks `WaitingUI.waitingUIisVisible`.)</span>
- `IsPaused` is `true` while the pause menu, the saving screen or the load menu is open. Do not confuse it with `GameTime.IsPaused`. That one is `true` whenever the game clock is stopped, which also happens while the pause menu or the saving screen is open. <span class="tech">(`IsPaused` reads `GameController.GameIsPaused`. `GameTime.IsPaused` reads `Time.timeScale == 0`.)</span>
- `IsInputBlocked` is `true` while the game has switched off the player's keys. That is the case in cutscenes, dialogue, sleep and similar. An open game menu does not set it. A mod that blocks the player's input with the framework sets it too. <span class="tech">(It reads `GameController.keysDisabled`. A `ControlsDisabled` call with the default `disableKeys` sets it, and `ShowGameMenu` passes `disableKeys: false`.)</span>
- `CursorVisible` is the game's own cursor flag. It does not include the cases where the game also shows the cursor for a controller or a computer screen. <span class="tech">(It reads `GameController.cursorVisible`, without what `UpdateCursorVisibility` adds.)</span>
- `InventoryDisabled` stays `true` until everyone who locked the inventory has unlocked it. Several things can lock it at the same time. <span class="tech">(It reads `GameController.inventoryDisabled`, which stays `true` while any holder of `DisableInventory` has not called `EnableInventory`.)</span>
- `PauseOpened(isDeathScreen)` fires right after the pause screen opens. The value is `true` when the pause screen is the death screen shown after the player dies. <span class="tech">(It is raised after `PauseMenu.PauseMenuOpen` returns. The argument is `Death.instance.dead`. `PauseMenu.PausemenuDeath` calls the same open path after `dead` is set.)</span>
- `PauseOpened` can fire twice in a row. If the pause screen is already open when the player dies, the game opens the death screen without closing the pause screen first, so you get a second `PauseOpened(true)`. If your handler must act only once, keep your own flag. <span class="tech">(The framework does not remove the repeat. `PausemenuDeath` runs the open path again with no close between.)</span>
- `PauseOpened` does not fire when the game fails to open the pause screen. On a scene with no game controller, such as the main menu, the game's own opening code fails before the framework is told, and the framework does not catch that failure. <span class="tech">(`PauseMenuOpen` throws at `GameController.instance.ChangeCursorStatus` when there is no `GameController`, before the postfix runs.)</span>
- `PauseClosed` fires when the player goes back to the game from the pause screen. It fires only if the pause screen was really open. <span class="tech">(It is raised after `PauseMenu.PauseMenuClose` returns, and only when `PauseIsActive()` was true before the call (read in the prefix). In the game code only `PauseMenu.Resume` calls `PauseMenuClose`, and `CardGameUI` calls `Resume` only while the menu is open.)</span>
- `PauseClosed` does not fire when the player quits to the main menu or to the desktop. Quitting to the menu loads a scene, and `Events.SceneLoaded` fires for that instead. Quitting to the desktop ends the game. <span class="tech">(`PauseMenu.QuitMainMenu` and `PauseMenu.QuitDesktop` do not call `PauseMenuClose`.)</span>
- `PauseEventsAvailable` tells you whether the two events work. It is `false` when the framework could not set them up, and then neither event ever fires. The five flags still work.
- There are no events for the five flags, so check them yourself. The game changes them in many places, and the framework could not watch all of them. Read them when you need them, for example once per frame in `Update` or when a key is pressed. Each read is cheap. <span class="tech">(`ControlsDisabled` and `ControlsEnabled` (`GameController`) write `keysDisabled` and `cursorVisible`. `DisableInventory` and `EnableInventory` write `inventoryDisabled`. `ShowGameMenu` and `HideGameMenu` (`GameUIController`) change the menu state. One postfix would miss the others, and each read is a field read.)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Files: `src/Api/UiState.cs`, `src/Api/UiStatePatches.cs`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes some of the game's methods when it starts, so it can tell your mod when the pause screen opens and closes. Those changes belong to one feature group, `obenseuer.moddingframework.uistate`, the name you will see in the log. The five flags only read the game and need no changes to it.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- Your event handlers run inside the game's own call, one at a time. If one of your handlers throws an error, the framework logs it once and still runs the other handlers.
- The framework does not hold back the pause events while a save is loading or being written. A handler that should ignore those moments can check `SaveData.Busy`.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the pause events can't be set up, only `PauseOpened` and `PauseClosed` stop. The feature group `obenseuer.moddingframework.uistate` turns off, error lines and a warning go to the log, and `PauseEventsAvailable` is `false`. The five flags keep working. <span class="tech">(The game methods involved are `PauseMenu.PauseMenuOpen` and `PauseMenu.PauseMenuClose`. The framework's own code in them never throws into the game: each prefix and postfix catches an error and logs it once.)</span>
