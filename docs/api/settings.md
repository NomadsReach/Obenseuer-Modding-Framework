---
title: Obenseuer Modding Framework Settings API (GameSettings, Keybinds, Localization)
parent: API reference
nav_order: 70
---

# Obenseuer Modding Framework Settings API (GameSettings, Keybinds, Localization)

Status: experimental.

This API reads the player's game options (display, graphics quality, mouse, sprint and crouch mode), reads the game's key bindings, lets a mod register its own keys, and reads the game language. The only game option a mod can change is the field of view.

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.settings`. Logs go to `BepInEx\logs\obenseuer.moddingframework.settings.log`.

Everything not listed here is internal.

## Rules

- **Main thread only.** Call every member from Unity's main thread.
- `GameSettings.IsAvailable` is false when its hooks were not installed or the owner was disabled.
- **Safe at any time.** Every member can be called on the main menu, during loading, and with no save loaded. Reads return a neutral value (`null`, `0`, `""`, `false`, or the key you passed to `Localize`). Keybind reads return `KeyCode.None` (`Key`, `Button`), `""` (`Label`) or `false` (`Down`, `Held`) before the game's input is ready.
- **Read only for the game's options, with one write.** `GameSettings.SetFov` is the only write to a game option. The game-action parts of `Keybinds` never write one. Writes to mod keybinds go through the BepInEx config.
- **Bad arguments.** A null or empty action, id, display name, table or key throws `ArgumentException`, and so does a mod id that is not 1 to 64 characters of letters, digits, `.`, `_` or `-`, or an id that contains `=`, `\`, `"`, `'`, `[`, `]`, a line break or a tab, which the config file cannot store. Unknown game action names return `KeyCode.None`, `""` or `false`, without an exception.
- **Game failures.** When a call into the game throws, the call returns its fallback. The first throw of each member is logged to the settings log. Later throws of that member are not logged again.

## GameSettings

```csharp
public static class GameSettings
{
    public static event Action Changed;

    public static bool IsAvailable { get; }

    public static GameDisplay Display();
    public static GameQuality Quality();
    public static int MouseSensitivity();
    public static bool InvertX();
    public static bool InvertY();
    public static bool HintsEnabled();
    public static bool ToggleSprint();
    public static bool ToggleCrouch();
    public static bool SetFov(int fov);
    public static bool SetFov(string owner, int fov);
    public static bool ClearFov(string owner);
}

public enum GameScreenMode { Windowed = 0, Borderless = 1, FullScreen = 2 }

public sealed class GameDisplay
{
    public int Width { get; }
    public int Height { get; }
    public int RefreshRate { get; }
    public GameScreenMode ScreenMode { get; }
    public bool VSync { get; }
    public int Fov { get; }
    public int Brightness { get; }
    public bool RunInBackground { get; }
}

public sealed class GameQuality
{
    public int Level { get; }
    public bool Custom { get; }
    public int ShadowQuality { get; }
    public int ShadowResolution { get; }
    public int TextureResolution { get; }
    public int Ssao { get; }
    public int Antialiasing { get; }
    public int PostFx { get; }
}
```

- `Display()`: the saved display options (`DisplaySave.instance.displaySave`). `null` when the save object does not exist. Width, Height, RefreshRate, VSync and Fov are the values the game last saved, and Width, Height and RefreshRate are 0 until the game has saved them once. `ScreenMode` maps the saved text: `Full Screen` to `FullScreen`, `Borderless` to `Borderless`, anything else to `Windowed`. `Brightness` is the saved slider value, game default 100. The game applies it as `Brightness / 100f`. The framework does not check the range. `RunInBackground` is the saved `Run In Background` option, default false. Both come from `Display()` and have no fallback of their own, so they exist only when `Display()` does. `SetBrightness` and `SetRunInBackground` (`OptionsMenu`) do not call `applyDisplaySettings`, so `Changed` does not fire for them. Callers must poll.
- `Quality()`: the saved graphics options. The integers are the game's own indexes, not named levels. `Level` is the preset index. `Custom` is true when the player picked the `Custom` preset. `null` when the save object does not exist.
- `MouseSensitivity()`: the raw saved value (default 900). It is not a multiplier. The game's own camera setting is this value divided by 100 with integer division (`SetControls.SetSensitivity`), so 900 gives 9, and any value under 100 gives 0.
- `InvertX()` and `InvertY()`: true when the saved invert mode includes that axis. The saved modes are `X`, `Y`, `X + Y`, and empty for none.
- `HintsEnabled()`: whether the game's hint overlay is switched on (`HintOverlay.HintsEnabled`, a PlayerPrefs value, default true). The hint overlay is the card game's rules box. The game has no hints option in its Options screen: the player turns it on and off with the Hints key (K by default).
- `Changed`: fires after the game runs `OptionsMenu.applyDisplaySettings()` or `OptionsMenu.applyQualitySettings()`. The game also calls `applyQualitySettings()` once when the options screen starts, so `Changed` can fire when the menu opens. It does not fire when the player changes only the field of view, the mouse sensitivity, the mouse inversion or the hint overlay, so read those when you need them. Handlers should compare values before they act. A handler that throws is logged and does not stop the others.
- `ToggleSprint()` and `ToggleCrouch()`: the saved `Toggle Sprint` and `Toggle Crouch` options. `true` is toggle mode (press once to start, press again to stop), and `false` is hold mode (the action lasts while the key is held). The game checks `!toggleSprint` and `!toggleCrouch` for hold mode (`OpenSewerCharacterController`), so `false` means hold, the opposite of what a reader might assume from the name. With no controls save, or when the read throws, both return `true`, the game default. This is the one exception to the neutral-value rule above. `SetToggleSprint` and `SetToggleCrouch` (`OptionsMenu`) do not call `applyDisplaySettings` or `applyQualitySettings`, so `Changed` does not fire for them. Callers must poll.
- **Warning: `SetFov(fov)` overwrites the player's own setting.** It writes `Display.json` on disk at once, so the player's chosen field of view is lost and stays lost after a restart, and a second mod that calls it simply replaces the first. Do not use it for zoom or for a temporary change. Use `SetFov(owner, fov)` or `ScreenFx.SetFovOverride(owner, fov)` instead, which never touch the file.
- `SetFov(owner, fov)` and `ClearFov(owner)`: the same as `ScreenFx.SetFovOverride(owner, fov)` and `ScreenFx.ClearFovOverride(owner)`. The value is kept in memory per owner, the newest owner wins, clearing falls back to the owner set before it, and a Single scene load clears every owner. The saved setting is never written. `SetFov(owner, fov)` returns `false` for a value outside 30 to 120, or when `ScreenFx.IsAvailable` is `false`. `owner` must be a valid mod id, or the call throws `ArgumentException`.
- `SetFov(fov)`: writes the saved field of view by calling the game's own `DisplaySave.SaveDisplay("Fov", ...)`, the same call the options slider makes. The value is written to `Display.json` at once. The options screen reads `displaySave.fov` when it opens, so a screen that is already open keeps its old slider position. The camera follows on the next frame through `SetCameraFOV.Update`, unless a `ScreenFx.SetFovOverride` is set, which keeps the camera where the override put it. It returns `false` for a value outside 30 to 120, when no save object exists, and while `SaveData.Busy` is true. The 30 to 120 range is the `ScreenFx` clamp, not necessarily the range of the game's slider. It does not raise `Changed`, as the player's field of view change does not. For a zoom or field-of-view change that the saved option does not change, use `ScreenFx.SetFovOverride`, see [screen-fx.md](screen-fx.md).

Not exposed, with reasons:
- **Subtitles toggle:** the game's Options screen has no subtitle setting.
- **Display number:** the game saves it, but this API does not expose it yet.
- **Other writes:** the other options (size, refresh rate, screen mode, VSync, quality, mouse, invert, hints) are changed by the Options screen and its apply methods (`applyDisplaySettings`, `applyQualitySettings`). A write here would bypass those. The field of view has its own save call (`SetFov`), so it is the only one.

## Keybinds

```csharp
public static class Keybinds
{
    public static KeyCode Key(string action);
    public static KeyCode Button(string action);
    public static string Label(string action);
    public static bool Down(string action);
    public static bool Held(string action);
    public static ModKeybind Register(string modId, string id, string displayName, KeyCode defaultKey);
}

public sealed class ModKeybind
{
    public KeyCode Key { get; }
    public bool Down();
    public bool Up();
    public bool Held();
}
```

Game actions use the game's own names, exactly as the game spells them:

`Forward`, `Backward`, `Left`, `Right`, `Walk`, `Crouch`, `Jump`, `Interact`, `Menu`, `Inventory`, `Map`, `Tasks`, `Tenement`, `Character`, `Notes`, `Pause Menu`, `Hide Hud`, `Hints`, `Primary Use`, `Auxiliary Use`, `Head Use`, `Primary Reload`, `Auxiliary Reload`, `Building Menu`, `Move Left`, `Move Right`, `Place Take`, `Rotate Object`, `Rotation Mode`, `Snap Object`, `Buy Sell`, `Resource Slots`, `Move Item`, `Drag And Drop`, `Drop`, `Switch Tab Left`, `Switch Tab Right`, `Use Item`, `Exit Cancel`, `screenshot`, `screenshotNoUI`.

- `Key(action)`: the keyboard key the player bound, respecting rebinds. `KeyCode.None` when the action is unbound or unknown.
- `Button(action)`: the controller button for the action. Controller buttons are not rebindable in the game, so this is the fixed default.
- `Label(action)`: the text the game shows for the active input device, for example `E` for a keyboard player, or `A` for a controller player. Use it for `Press [E]` prompts: `"Press [" + Keybinds.Label("Interact") + "]"`. Returns `""` when unbound.
- `Down(action)` and `Held(action)`: true when the player pressed or holds the bound key or the controller button. They respect rebinds. They do not check whether a menu or dialogue is open, so check the state yourself before you act on them.
- `Register(modId, id, displayName, defaultKey)`: a key the mod owns, saved in `config\ObenseuerCore.ModKeybinds.cfg`, section `modId`, key `id`. Calling it again with the same `modId` and `id` returns the same handle. The player changes it in the Mods menu, page **Mod Keybinds**, which the framework adds on the first `Register` call. Returns `null` only when creating the config entry throws, for example when the config file cannot be opened (logged). Check for `null`. When a new bind overlaps a bind of another mod (same main key, and one set of modifiers contains the other, so one press fires both), `Register` logs one warning that names both mods, their ids and their keys. Both binds still fire. The check runs only in `Register`: a later rebind in the Mods menu is not checked, and a clash with a game action is not detected. The player resolves a clash by rebinding in the Mods menu. A hand-edited value that does not parse leaves the key unbound (`ModKeybind.Key` is `KeyCode.None`) until the player picks a new one.
- `ModKeybind.Key`: the bound main key. Modifiers are not returned.
- `ModKeybind.Down()`, `Up()` and `Held()`: the main key is read with `Input.GetKeyDown`, `Input.GetKeyUp` or `Input.GetKey`, and every bound modifier must be held. Other keys held at the same time, such as WASD, do not block the key.
- Self-check: `[Debug] KeyChordSelfCheck` in the framework config runs the chord rule at start and logs `keychord self-check ok`.

Mod keys are shown in the Mods menu, not on the game's Key Bindings screen.

Reads of `Key`, `Button`, `Label`, `Down` and `Held` on a held bound key or button can switch the game's input device indicator (`InputManager.inputType`), as the game's own reads do.

## Localization

```csharp
public static class Localization
{
    public static string Language();
    public static string Localize(string key);
    public static string Localize(string table, string key);
}
```

- `Language()`: the live locale name (`LocalizationSettings.SelectedLocale.LocaleName`), for example `English`. `""` before the localization system starts.
- `Localize(key)`: the string `key` in the game's `Menu` table, in the selected locale. `Localize(table, key)` reads any other table, for any other Unity Localization string table. The game itself only reads `Menu`.
- When the key has no translation, `Localize` returns `key` itself, which matches how the game's own `MenuLocalization` falls back.

The Unity Localization assembly is not referenced at build time, so this class reaches it by reflection.

## Not exposed, with reasons

- **Game Key Bindings screen rows for mod keys:** the game's options menu rows are fixed fields wired in the scene, and each game action is hard-coded in `KeyBindings` and `InputManager`. Adding rows is not clean, so mod keys go to the Mods menu.
- **Controller button remapping:** the game has no UI for it and its buttons are fixed per action, so there is nothing to wrap.
- **Hint toggle events:** `HintOverlay.HintsToggled` exists in the game, but it is not wrapped here. Subscribe to it directly if needed.

## Save loads

The framework does not gate `Changed` on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
