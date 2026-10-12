# Obenseuer Modding Framework Settings API (GameSettings, Keybinds, Localization)

Status: experimental. Not checked in game yet.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.settings`. Logs go to `BepInEx\logs\obenseuer.moddingframework.settings.log`.

Everything not listed here is internal.

## Rules

- **Main thread only.** Call every member from Unity's main thread.
- `GameSettings.IsAvailable` is false when its hooks were not installed or the owner was disabled.
- **Safe at any time.** Every member can be called on the main menu, during loading, and with no save loaded. Reads return a neutral value (`null`, `0`, `""`, `false`, or the key you passed to `Localize`). Keybind reads return `KeyCode.None` (`Key`, `Button`), `""` (`Label`) or `false` (`Down`, `Held`) before the game's input is ready.
- **Read only for the game's options.** `GameSettings` and the game-action parts of `Keybinds` never write a player option. Writes to mod keybinds go through the BepInEx config.
- **Bad arguments.** A null or empty action, id, display name, table or key throws `ArgumentException`, and so does a mod id that is not 1 to 64 characters of letters, digits, `.`, `_` or `-`, or an id that contains `=`, `\`, `"`, `'`, `[`, `]`, a line break or a tab, which the config file cannot store. Unknown game action names return `KeyCode.None`, `""` or `false`, without an exception.
- **Game failures.** When a call into the game throws, the call returns its fallback. The first throw of each member is logged to the settings log. Later throws of that member are not logged again.

## GameSettings

```csharp
public static class GameSettings
{
    public static event Action Changed;

    public static GameDisplay Display();
    public static GameQuality Quality();
    public static int MouseSensitivity();
    public static bool InvertX();
    public static bool InvertY();
    public static bool HintsEnabled();
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

- `Display()`: the saved display options (`DisplaySave.instance.displaySave`). `null` when the save object does not exist. Width, Height, RefreshRate, VSync and Fov are the values the game last saved, and Width, Height and RefreshRate are 0 until the game has saved them once. `ScreenMode` maps the saved text: `Full Screen` to `FullScreen`, `Borderless` to `Borderless`, anything else to `Windowed`.
- `Quality()`: the saved graphics options. The integers are the game's own indexes, not named levels. `Level` is the preset index. `Custom` is true when the player picked the `Custom` preset. `null` when the save object does not exist.
- `MouseSensitivity()`: the raw saved value (default 900). It is not a multiplier. The game's own camera setting is this value divided by 100 with integer division (`SetControls.SetSensitivity`), so 900 gives 9, and any value under 100 gives 0.
- `InvertX()` and `InvertY()`: true when the saved invert mode includes that axis. The saved modes are `X`, `Y`, `X + Y`, and empty for none.
- `HintsEnabled()`: the hint overlay toggle (`HintOverlay.HintsEnabled`, a PlayerPrefs value, default true).
- `Changed`: fires after the game runs `OptionsMenu.applyDisplaySettings()` or `OptionsMenu.applyQualitySettings()`. The game also calls `applyQualitySettings()` once when the options screen starts, so `Changed` can fire when the menu opens. It does not fire when the player changes only the field of view, the mouse sensitivity, the mouse inversion or the hint toggle, so read those when you need them. Handlers should compare values before they act. A handler that throws is logged and does not stop the others.

Not exposed, with reasons:
- **Subtitles toggle:** no subtitle option appears in the game's options code (`SaveSettings\` has no subtitle match, and `UI\Subtitles.cs` has no enable flag).
- **Brightness, Run In Background, Display number:** saved by the game, but not on the requested list. They can be added the same way.
- **Writes:** the game's options are changed by the player's Options screen. A mod that wrote them would bypass the game's own apply path.

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
    public bool Held();
}
```

Game actions use the game's own names, exactly as the game spells them:

`Forward`, `Backward`, `Left`, `Right`, `Walk`, `Crouch`, `Jump`, `Interact`, `Menu`, `Inventory`, `Map`, `Tasks`, `Tenement`, `Character`, `Notes`, `Pause Menu`, `Hide Hud`, `Hints`, `Primary Use`, `Auxiliary Use`, `Head Use`, `Primary Reload`, `Auxiliary Reload`, `Building Menu`, `Move Left`, `Move Right`, `Place Take`, `Rotate Object`, `Rotation Mode`, `Snap Object`, `Buy Sell`, `Resource Slots`, `Move Item`, `Drag And Drop`, `Drop`, `Switch Tab Left`, `Switch Tab Right`, `Use Item`, `Exit Cancel`, `screenshot`, `screenshotNoUI`.

- `Key(action)`: the keyboard key the player bound, respecting rebinds. `KeyCode.None` when the action is unbound or unknown.
- `Button(action)`: the controller button for the action. Controller buttons are not rebindable in the game, so this is the fixed default.
- `Label(action)`: the text the game shows for the active input device, for example `E` for a keyboard player, or `A` for a controller player. Use it for `Press [E]` prompts: `"Press [" + Keybinds.Label("Interact") + "]"`. Returns `""` when unbound.
- `Down(action)` and `Held(action)`: true when the player pressed or holds the bound key or the controller button. They respect rebinds. They do not check whether a menu or dialogue is open, so check the state yourself before you act on them.
- `Register(modId, id, displayName, defaultKey)`: a key the mod owns, saved in `config\ObenseuerCore.ModKeybinds.cfg`, section `modId`, key `id`. Calling it again with the same `modId` and `id` returns the same handle. The player changes it in the Mods menu, page **Mod Keybinds**, which Core adds on the first `Register` call. Returns `null` only when creating the config entry throws, for example when the config file cannot be opened (logged). Check for `null`. Core does not detect two mods, or a mod and a game action, that use the same key: every owner reacts to the press, and the player resolves it by rebinding in the Mods menu. A hand-edited value that does not parse leaves the key unbound (`ModKeybind.Key` is `KeyCode.None`) until the player picks a new one.
- `ModKeybind.Key`: the bound main key. Modifiers are not returned.
- `ModKeybind.Down()` and `ModKeybind.Held()`: BepInEx `KeyboardShortcut` checks, so the bound modifiers must be held too.

Where the mod key is shown: the Mods menu, not the game's Key Bindings screen. The game's Key Bindings screen was not used.

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

The Unity Localization assembly is not referenced at build time, so this class reaches it by reflection. The reflected members were checked against the game's `Unity.Localization.dll`.

## Init wiring

- `GameSettings.Init()` installs the two Harmony postfixes behind `GameSettings.Changed` (owner `obenseuer.moddingframework.settings`). Core calls it from `Core`. Keybinds and Localization need no Init.

## Not exposed, with reasons

- **Game Key Bindings screen rows for mod keys:** the game's options menu rows are fixed fields wired in the scene, and each game action is hard-coded in `KeyBindings` and `InputManager`. Adding rows is not clean, so mod keys go to the Mods menu.
- **Controller button remapping:** the game has no UI for it and its buttons are fixed per action, so there is nothing to wrap.
- **Hint toggle events:** `HintOverlay.HintsToggled` exists in the game, but it is not wrapped here. Subscribe to it directly if needed.
