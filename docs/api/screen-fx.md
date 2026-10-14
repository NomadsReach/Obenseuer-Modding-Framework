---
title: ScreenFx API
parent: API reference
nav_order: 68
---

# ScreenFx API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.screenfx`, used only by the FOV override.
Files: `src/Api/ScreenFx.cs`, `src/Api/ScreenFxPatches.cs`.

Everything not listed here is internal.

## If a hook cannot be installed

The framework installs the FOV override hooks (`Update` and `SetFOV` on `SetCameraFOV`) under one owner. If either is missing, the owner is removed and `ScreenFx.IsAvailable` becomes `false`. Shake, the fades and the death message do not depend on the hooks and keep working.

## ScreenFx

```csharp
public static class ScreenFx
{
    public static bool Shake(float duration, float amount);
    public static bool FadeToBlack(float speed = 0.1f);
    public static bool FadeIn(float speed = 0.1f);
    public static bool FadeToBlack(string owner, float speed);
    public static bool FadeIn(string owner, float speed);
    public static bool SetDeathMessage(string text);
    public static bool SetFovOverride(float fov);
    public static bool ClearFovOverride();
    public static bool SetFovOverride(string owner, float fov);
    public static bool ClearFovOverride(string owner);

    public static bool IsAvailable { get; }
}
```

- Each method returns `true` when the game call was made, and `false` when the game object is missing (`CameraShake.instance`, `BlackCanvas.instance`, `BlackCanvas.fadeCanvasGroup`, `DeathMessage.instance`), for example on the main menu or while loading. `Shake`, `FadeToBlack` and `SetDeathMessage` also return `false` while a save loads or saves (`SaveData.Busy`). `FadeIn` always runs, so the screen can clear during a save.
- Invalid arguments throw `ArgumentException`: `duration` must be greater than 0 and finite, `amount` must not be negative and must be finite, `speed` must be greater than 0, and `text` must not be `null`. Nothing else throws to the caller.

## Threading and safety

- Main thread only.
- Each call runs inside a try/catch that logs once and returns `false`.

## Shake

- `Shake(duration, amount)` calls the game's static `CameraShake.Shake`. `duration` is in seconds and `amount` is the maximum offset per frame, in world units.
- A new shake replaces the one in progress. The game's `Shake` stops its own coroutine first, so a mod shake cancels a game shake and the reverse. Health hits use the same call.
- The shake pauses while `PauseMenu.paused` is set.
- A shake in progress is restored first. Before a new shake, the camera goes back to the base position from before the running shake, so an interrupted shake leaves no offset. This is done only while a shake is running, and it covers game shakes too.

## FadeToBlack and FadeIn

- `FadeToBlack(speed)` calls the game's `BlackCanvas.FadeIn`. It fades the black canvas up to full black and leaves it there until `FadeIn` runs. It does not change player controls.
- `FadeIn(speed)` calls `fadeCanvasGroup.StartFadeOut(speed)` on the same canvas. That is what the game's `BlackCanvas.FadeOut` does, minus the `ControlsEnabled` call on line 41.
- Why the facade skips `BlackCanvas.FadeOut`: `ControlsEnabled` removes only its own holder. It also resets the two camera look flags unconditionally (`ControlsEnabledMove`, which calls `ThirdPersonCameraController.EnableMovement` and `EnableFirstPersonMovement`). A fade from a mod during dialogue or a cutscene would free the camera that the cutscene locked. The facade does the same fade without that call. Do not call `BlackCanvas.ShowBlackCanvas` either: it disables movement and keys.
- `speed` is the game's fade step per tick. The game's `StartFadeInAndOut` uses `0.1f` by default, and that is the default here. The game also uses `0.5f` for tenement fades. Smaller values are slower. Steps are paced by `WaitForSecondsRealtime`, and the canvas's `realTime` flag changes the step size.
- A new fade on the same canvas stops the one in progress (`FadeCanvasGroup`). The game uses the same canvas for its own blackouts (`BlackoutTrigger`), treatment, dialogue and tenement fades. A mod's `FadeIn` can lift a black screen the game is holding, and a mod's `FadeToBlack` can cut off a game fade in progress.
- Owners: `FadeToBlack(owner, speed)` counts per owner, and `FadeIn(owner, speed)` takes one count off. The screen fades back in only when no owner holds a count, so mod B's `FadeIn` does not lift mod A's black screen. `FadeIn(owner, speed)` returns `false` for an owner with no count, and `true` when the count was taken, even while another owner keeps the screen black. `owner` must be a valid mod id.
- `FadeToBlack(speed)` and `FadeIn(speed)` with no owner use one shared entry that is held or not, never counted. `FadeIn(speed)` releases it and fades in only when no owner holds the screen, otherwise it returns `false`. With no owner at all it fades in as before, so it can still lift a game blackout.
- A Single scene load drops every count. It does not fade in, so a game fade is not cut off.
- Nothing is saved. Restore a faded screen with `FadeIn`.

## SetDeathMessage

- `SetDeathMessage(text)` calls `DeathMessage.ChangeDeathMessage`, which sets the reason text.
- The text stays until the game calls `ChangeDeathMessage` again. The game does so in `PlayerStatsUI`. Set the text just before the death screen is shown (`Death`, `ShowDeathMessage`), or it may be replaced.
- Nothing is saved.

## FOV override

- `SetFovOverride(fov)` stores the value in memory. While it is set, `SetCameraFOV.Update` and `SetCameraFOV.SetFOV` do not copy the saved display FOV. They write the override into the camera's `fieldOfView`. It returns `true` when the value is stored. It returns `false` when `IsAvailable` is `false`. It works inside `Events.SceneLoaded` and while a save loads or saves.
- The value is clamped to 30 to 120 degrees. The clamp is not taken from the game's settings slider, because the slider's min and max are serialized on the UI slider and are not in the game code. A `NaN` or infinite value throws `ArgumentException`.
- `ClearFovOverride()` removes the value. On the next frame `SetCameraFOV.Update` finds the camera different from `DisplaySave.fov` and writes the saved FOV back. It returns `true` when a value was cleared, and `false` when none was set.
- `DisplaySave` is never written, so the player's saved FOV and the options screen are unchanged. The override is not saved. Only a `Single` scene load clears it, so an additive streaming load keeps it. The clear runs before the `SceneLoaded` event reaches any mod, so a mod that sets the override from its `Events.SceneLoaded` handler keeps it. Nothing clears it on a menu open or close, so it stays through the pause menu until the next single scene load or `ClearFovOverride`.
- While the override is set, the options menu FOV slider still changes the saved display FOV and its number, but the camera does not follow it, because `SetCameraFOV.Update` is skipped. The slider looks as if it does nothing until the override is cleared or the scene loads.
- `RadiationEffectUI` passes the saved `DisplaySave.fov` to `RadiationController.LookingAtRadiation`, not the camera FOV, so while the override is set its radiation check uses the saved FOV.
- The override applies to every camera that has a `SetCameraFOV` component. If the hook throws, the error is logged once and the game's own code runs for that frame.
- Owners: `SetFovOverride(owner, fov)` and `ClearFovOverride(owner)` keep one value per mod id, so two mods do not overwrite each other silently. The last owner to set a value is the one applied. When that owner clears, the value of the owner set before it applies again. `SetFovOverride(fov)` and `ClearFovOverride()` with no owner use one shared entry of their own, so they never remove an owner's value. A `Single` scene load clears every entry.
- Per-frame reset, checked in the game code: `SetCameraFOV.Update` writes `fieldOfView` only when it differs from `DisplaySave.fov`, and `SetFOV` always writes it. The framework's FOV prefix returns `false` while an override is set, so the game's write does not run and the override holds through the reset.
- Zoom: this facade is the zoom API. A zoom key calls `SetFovOverride(owner, fov)` on press and `ClearFovOverride(owner)` on release. See `docs/api/player.md` for the camera.

Zoom example, from a mod's `Update`. The owner is the mod id. Clearing it restores the saved FOV on the next frame:

```csharp
ModKeybind zoom = Keybinds.Register("com.example.zoom", "zoom", "Zoom", KeyCode.Z);

void Update()
{
    if (zoom.Down()) ScreenFx.SetFovOverride("com.example.zoom", 30f);
    if (zoom.Up()) ScreenFx.ClearFovOverride("com.example.zoom");
}
```
