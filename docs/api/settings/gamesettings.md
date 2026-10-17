---
title: GameSettings
parent: Settings
grand_parent: Framework basics
nav_order: 2
---

# GameSettings

This is the full list of what the API gives you, one section for each part. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

`GameSettings` reads the options the player chose in the game's Options screen. Each read gives a snapshot of what is saved when you ask.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>GameSettings</code> <span>reads the player's options</span></p>

This is the class to start from. `Display()` and `Quality()` give you the saved options, `SetFov` and `ClearFov` change the field of view, and `Changed` tells you when the game applies new display or quality options.

<div class="api-pair" markdown="1">

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
```

| Method | Returns | What it does |
|---|---|---|
| `Display()` | `GameDisplay` | The display options. `null` when they are not available yet. |
| `Quality()` | `GameQuality` | The graphics quality options. `null` when they are not available yet. |
| `MouseSensitivity()` | `int` | The saved mouse sensitivity value. |
| `InvertX()` | `bool` | `true` when the mouse is inverted left to right. |
| `InvertY()` | `bool` | `true` when the mouse is inverted up and down. |
| `HintsEnabled()` | `bool` | `true` when the card game's hints are switched on. |
| `ToggleSprint()` | `bool` | `true` for toggle mode, `false` for hold mode. |
| `ToggleCrouch()` | `bool` | `true` for toggle mode, `false` for hold mode. |
| `SetFov(fov)` | `bool` | Overwrites the player's saved field of view. |
| `SetFov(owner, fov)` | `bool` | Sets the field of view in memory only. |
| `ClearFov(owner)` | `bool` | Gives the field of view back. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when the `Changed` event works. See Details. |

| Event | Gives you | When it fires |
|---|---|---|
| `Changed` | Nothing | The game has applied new display or quality options. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>GameScreenMode</code> <span>window, borderless or full screen</span></p>

`GameDisplay.ScreenMode` gives you one of these.

<div class="api-pair" markdown="1">

```csharp
public enum GameScreenMode
{
    Windowed = 0,
    Borderless = 1,
    FullScreen = 2
}
```

| Value | Meaning |
|---|---|
| `Windowed` | A normal window. |
| `Borderless` | A window with no border that fills the screen. |
| `FullScreen` | Full screen. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>GameDisplay</code> <span>the saved display options</span></p>

`GameSettings.Display()` gives you one of these, or `null` when the game has not created its options yet. It is a snapshot of what is saved when you ask. <span class="tech">(It is built from the game's saved display settings.)</span>

<div class="api-pair" markdown="1">

```csharp
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
```

| Property | Type | What it means |
|---|---|---|
| `Width` | `int` | The screen width in pixels. `0` until the game has saved it once. |
| `Height` | `int` | The screen height in pixels. `0` until the game has saved it once. |
| `RefreshRate` | `int` | The refresh rate in hertz. `0` until the game has saved it once. |
| `ScreenMode` | `GameScreenMode` | Window, borderless or full screen. |
| `VSync` | `bool` | `true` when vertical sync is on. |
| `Fov` | `int` | The field of view the player saved, in degrees. |
| `Brightness` | `int` | The brightness slider value. The game's default is 100. |
| `RunInBackground` | `bool` | `true` when the game keeps running while its window is not in front. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>GameQuality</code> <span>the saved graphics options</span></p>

`GameSettings.Quality()` gives you one of these, or `null` when the game has not created its options yet. Its numbers are the game's own index numbers, not named levels. <span class="tech">(It is built from the game's saved quality settings.)</span>

<div class="api-pair" markdown="1">

```csharp
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

| Property | Type | What it means |
|---|---|---|
| `Level` | `int` | The number of the quality preset the player picked. |
| `Custom` | `bool` | `true` when the player picked the Custom preset. |
| `ShadowQuality` | `int` | The game's index for shadow quality. |
| `ShadowResolution` | `int` | The game's index for shadow resolution. |
| `TextureResolution` | `int` | The game's index for texture resolution. |
| `Ssao` | `int` | The game's index for ambient shading (SSAO). |
| `Antialiasing` | `int` | The game's index for edge smoothing. |
| `PostFx` | `int` | The game's index for screen effects. |

</div>
</div>

## Display and quality

- `Display()` gives the display options the game last saved, or `null` when the game has not created them yet. <span class="tech">(it reads `DisplaySave.instance.displaySave`; `Width`, `Height` and `RefreshRate` are `0` until the game has saved them once)</span>
- `ScreenMode` turns the game's saved text into one of three values. The text `Full Screen` becomes `FullScreen`, `Borderless` becomes `Borderless`, and anything else becomes `Windowed`.
- `Brightness` is the slider value, and the game applies it as that number divided by 100. The framework does not check that it is in a range.
- `RunInBackground` is the saved `Run In Background` option. It is `false` by default.
- `Brightness` and `RunInBackground` come from `Display()`, so they exist only when `Display()` does.
- `Quality()` gives the graphics options. The numbers are the game's own index numbers, not named levels. It gives `null` when the game has not created them yet.
- `Level` is the preset the player picked, and `Custom` is `true` for the Custom preset.
- `MouseSensitivity()` is the raw saved value. It is 900 by default, and it is not a multiplier. The game's camera uses this number divided by 100 with whole-number division, so 900 gives 9, and anything below 100 gives 0. <span class="tech">(the division is in `SetControls.SetSensitivity`)</span>
- `InvertX()` and `InvertY()` are `true` when the saved mouse inversion includes that direction. <span class="tech">(the saved modes are `X`, `Y`, `X + Y`, and an empty text for none)</span>
- `ToggleSprint()` and `ToggleCrouch()` give `true` for toggle mode and `false` for hold mode. In toggle mode you press once to start and press again to stop. In hold mode the action lasts while you hold the key. <span class="tech">(the game checks `!toggleSprint` and `!toggleCrouch` for hold mode in `OpenSewerCharacterController`)</span>
- Read `false` as hold, which is the opposite of what the name may make you think.
- If the game has no controls save yet, or the read fails, both give `true`, which is the game's default. This is the one read that does not give `false` when nothing is there.
- `HintsEnabled()` tells you whether the card game's hints are switched on. The hints are the rules box that the card game shows. The player turns them on and off with the Hints key, which is K by default. It is `true` by default. <span class="tech">(it reads `HintOverlay.HintsEnabled`, a `PlayerPrefs` value (`HintOverlayEnabled`, default 1))</span>
- The game has no hints option in its Options screen, so there is nothing to find there. <span class="tech">(the game's own `HintOverlay.HintsToggled` event exists, but the framework does not offer it, so subscribe to it directly if you need it)</span>
- `Changed` fires after the player applies display or quality options. The game also applies the quality options once when the Options screen opens, so `Changed` can fire when that screen opens. Compare the values before you act on it.
- `Changed` does not fire for every option. It does not fire when the player changes only the field of view, the mouse sensitivity, the mouse inversion, the brightness, the run in background option, sprint or crouch mode, or the hints. Read those when you need them. <span class="tech">(it fires after `OptionsMenu.applyDisplaySettings()` and `OptionsMenu.applyQualitySettings()`; `SetBrightness`, `SetRunInBackground`, `SetToggleSprint` and `SetToggleCrouch` on `OptionsMenu` do not call either one)</span>

## Field of view

- `SetFov(fov)` overwrites the player's own field of view setting, and it writes the setting to `Display.json` on disk at once. Do not use it for a zoom or a short change.
- The player's chosen value is lost, and it stays lost after a restart. A second mod that calls it simply replaces the first.
- Use `SetFov(owner, fov)` or `ScreenFx.SetFovOverride(owner, fov)` instead, which never touch the file.
- `SetFov(fov)` gives `true` when it saved the value. It gives `false` for a value outside 30 to 120, when the game has no display save yet, and while a save is loading or being written.
- The options screen reads the saved value when it opens, so a screen that is already open keeps its old slider position.
- The camera follows on the next frame, unless a field of view override is set. Then the camera stays where the override put it. The call does not raise `Changed`. <span class="tech">(it calls the game's own `DisplaySave.SaveDisplay("Fov", ...)`, the same call the options slider makes, and the camera follows through `SetCameraFOV.Update`; the range 30 to 120 is the `ScreenFx` limit, which is not necessarily the range of the game's slider)</span>
- `SetFov(owner, fov)` sets the field of view in memory only, and `ClearFov(owner)` takes it back.
- The newest owner wins, clearing falls back to the owner set before it, and a scene load that replaces the whole scene clears all of them.
- The player's saved setting is never written. It is the same as `ScreenFx.SetFovOverride(owner, fov)` and `ScreenFx.ClearFovOverride(owner)`. See [Screen effects](../screen-fx.md). An owner is your mod's id (see the [glossary](../../glossary.md#owner)).
- `SetFov(owner, fov)` gives `false` for a value outside 30 to 120, or when the screen effects are not available. An owner that is not a valid mod id throws an `ArgumentException`. <span class="tech">(it returns `false` when `ScreenFx.IsAvailable` is `false`)</span>

## Limits

- You can't read or change a subtitles option. The game's Options screen has no subtitle setting.
- You can't read the display number. The game saves which display it uses, but this API does not give it to you yet.
- You can't change any other option. The size, refresh rate, screen mode, vertical sync, quality, mouse, inversion and hints are changed by the Options screen and its apply methods. A change from here would skip those, so it is not offered.
- The field of view has its own save call, so it is the only option you can change. <span class="tech">(the apply methods are `applyDisplaySettings` and `applyQualitySettings` on `OptionsMenu`)</span>
