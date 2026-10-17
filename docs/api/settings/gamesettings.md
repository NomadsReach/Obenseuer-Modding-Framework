---
title: GameSettings
parent: Settings
grand_parent: Framework basics
nav_order: 2
---

# GameSettings

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`GameSettings` reads the options the player chose in the game's Options screen. Each read gives a snapshot of what is saved when you ask.

```csharp
public static class GameSettings
{
    public static event Action Changed;   // the game has applied new display or quality options

    public static bool IsAvailable { get; }    // true when the Changed event works (see Details)

    public static GameDisplay Display();       // the display options; null when they are not available yet
    public static GameQuality Quality();       // the graphics quality options; null when they are not available yet
    public static int MouseSensitivity();      // the saved mouse sensitivity value
    public static bool InvertX();              // true when the mouse is inverted left to right
    public static bool InvertY();              // true when the mouse is inverted up and down
    public static bool HintsEnabled();         // true when the card game's hints are switched on
    public static bool ToggleSprint();         // true for toggle mode, false for hold mode
    public static bool ToggleCrouch();         // true for toggle mode, false for hold mode
    public static bool SetFov(int fov);                    // overwrites the player's saved field of view
    public static bool SetFov(string owner, int fov);      // sets the field of view in memory only
    public static bool ClearFov(string owner);             // gives the field of view back
}

public enum GameScreenMode
{
    Windowed = 0,     // a normal window
    Borderless = 1,   // a window with no border that fills the screen
    FullScreen = 2    // full screen
}

public sealed class GameDisplay    // the saved display options
{
    public int Width { get; }                  // the screen width in pixels; 0 until the game has saved it once
    public int Height { get; }                 // the screen height in pixels; 0 until the game has saved it once
    public int RefreshRate { get; }            // the refresh rate in hertz; 0 until the game has saved it once
    public GameScreenMode ScreenMode { get; } // window, borderless or full screen
    public bool VSync { get; }                 // true when vertical sync is on
    public int Fov { get; }                    // the field of view the player saved, in degrees
    public int Brightness { get; }             // the brightness slider value; the game's default is 100
    public bool RunInBackground { get; }       // true when the game keeps running while its window is not in front
}

public sealed class GameQuality    // the saved graphics options
{
    public int Level { get; }               // the number of the quality preset the player picked
    public bool Custom { get; }             // true when the player picked the Custom preset
    public int ShadowQuality { get; }       // the game's index for shadow quality
    public int ShadowResolution { get; }    // the game's index for shadow resolution
    public int TextureResolution { get; }   // the game's index for texture resolution
    public int Ssao { get; }                // the game's index for ambient shading (SSAO)
    public int Antialiasing { get; }        // the game's index for edge smoothing
    public int PostFx { get; }              // the game's index for screen effects
}
```

### Display and quality

- **`Display()` gives the display options the game last saved, or `null` when the game has not created them yet.**
  - Technical: it reads `DisplaySave.instance.displaySave`. `Width`, `Height` and `RefreshRate` are `0` until the game has saved them once.
- **`ScreenMode` turns the game's saved text into one of three values.** The text `Full Screen` becomes `FullScreen`, `Borderless` becomes `Borderless`, and anything else becomes `Windowed`.
- **`Brightness` is the slider value, and the game applies it as that number divided by 100.** The framework does not check that it is in a range.
- **`RunInBackground` is the saved `Run In Background` option. It is `false` by default.** `Brightness` and `RunInBackground` come from `Display()`, so they exist only when `Display()` does.
- **`Quality()` gives the graphics options. The numbers are the game's own index numbers, not named levels.** It gives `null` when the game has not created them yet. `Level` is the preset the player picked, and `Custom` is `true` for the Custom preset.
- **`MouseSensitivity()` is the raw saved value. It is 900 by default, and it is not a multiplier.** The game's camera uses this number divided by 100 with whole-number division, so 900 gives 9, and anything below 100 gives 0.
  - Technical: the division is in `SetControls.SetSensitivity`.
- **`InvertX()` and `InvertY()` are `true` when the saved mouse inversion includes that direction.**
  - Technical: the saved modes are `X`, `Y`, `X + Y`, and an empty text for none.
- **`ToggleSprint()` and `ToggleCrouch()` give `true` for toggle mode and `false` for hold mode.** In toggle mode you press once to start and press again to stop. In hold mode the action lasts while you hold the key. Read `false` as hold, which is the opposite of what the name may make you think. If the game has no controls save yet, or the read fails, both give `true`, which is the game's default. This is the one read that does not give `false` when nothing is there.
  - Technical: the game checks `!toggleSprint` and `!toggleCrouch` for hold mode in `OpenSewerCharacterController`.
- **`HintsEnabled()` tells you whether the card game's hints are switched on.** The hints are the rules box that the card game shows. The player turns them on and off with the Hints key, which is K by default. The game has no hints option in its Options screen, so there is nothing to find there. It is `true` by default.
  - Technical: it reads `HintOverlay.HintsEnabled`, a `PlayerPrefs` value (`HintOverlayEnabled`, default 1). The game's own `HintOverlay.HintsToggled` event exists, but the framework does not offer it, so subscribe to it directly if you need it.
- **`Changed` fires after the player applies display or quality options.** The game also applies the quality options once when the Options screen opens, so `Changed` can fire when that screen opens. Compare the values before you act on it.
- **`Changed` does not fire for every option.** It does not fire when the player changes only the field of view, the mouse sensitivity, the mouse inversion, the brightness, the run in background option, sprint or crouch mode, or the hints. Read those when you need them.
  - Technical: it fires after `OptionsMenu.applyDisplaySettings()` and `OptionsMenu.applyQualitySettings()`. `SetBrightness`, `SetRunInBackground`, `SetToggleSprint` and `SetToggleCrouch` on `OptionsMenu` do not call either one.

### Field of view

- **`SetFov(fov)` overwrites the player's own field of view setting. Do not use it for a zoom or a short change.** It writes the setting to `Display.json` on disk at once. The player's chosen value is lost, and it stays lost after a restart. A second mod that calls it simply replaces the first. Use `SetFov(owner, fov)` or `ScreenFx.SetFovOverride(owner, fov)` instead, which never touch the file.
- **`SetFov(fov)` gives `true` when it saved the value.** It gives `false` for a value outside 30 to 120, when the game has no display save yet, and while a save is loading or being written. The options screen reads the saved value when it opens, so a screen that is already open keeps its old slider position. The camera follows on the next frame, unless a field of view override is set. Then the camera stays where the override put it. The call does not raise `Changed`.
  - Technical: it calls the game's own `DisplaySave.SaveDisplay("Fov", ...)`, the same call the options slider makes, and the camera follows through `SetCameraFOV.Update`. The range 30 to 120 is the `ScreenFx` limit, which is not necessarily the range of the game's slider.
- **`SetFov(owner, fov)` sets the field of view in memory only, and `ClearFov(owner)` takes it back.** The newest owner wins, clearing falls back to the owner set before it, and a scene load that replaces the whole scene clears all of them. The player's saved setting is never written. It is the same as `ScreenFx.SetFovOverride(owner, fov)` and `ScreenFx.ClearFovOverride(owner)`. See [Screen effects](../screen-fx.md). An owner is your mod's id (see the [glossary](../../glossary.md#owner)).
- **`SetFov(owner, fov)` gives `false` for a value outside 30 to 120, or when the screen effects are not available.** An owner that is not a valid mod id throws an `ArgumentException`.
  - Technical: it returns `false` when `ScreenFx.IsAvailable` is `false`.

### What you can't do (yet)

- **You can't read or change a subtitles option.** The game's Options screen has no subtitle setting.
- **You can't read the display number.** The game saves which display it uses, but this API does not give it to you yet.
- **You can't change any other option.** The size, refresh rate, screen mode, vertical sync, quality, mouse, inversion and hints are changed by the Options screen and its apply methods. A change from here would skip those, so it is not offered. The field of view has its own save call, so it is the only option you can change.
  - Technical: the apply methods are `applyDisplaySettings` and `applyQualitySettings` on `OptionsMenu`.
