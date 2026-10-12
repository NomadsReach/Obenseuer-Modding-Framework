# ScreenFx API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. No patches, no owner, no `Init`: the facade only calls game methods.
Files: `src/Api/ScreenFx.cs`.

Everything not listed here is internal.

## ScreenFx

```csharp
public static class ScreenFx
{
    public static bool Shake(float duration, float amount);
    public static bool FadeToBlack(float speed = 0.1f);
    public static bool FadeIn(float speed = 0.1f);
    public static bool SetDeathMessage(string text);
}
```

- Each method returns `true` when the game call was made, and `false` when the game object is missing (`CameraShake.instance`, `BlackCanvas.instance`, `BlackCanvas.fadeCanvasGroup`, `DeathMessage.instance`), for example on the main menu or while loading.
- Invalid arguments throw `ArgumentException`: `duration` must be greater than 0 and finite, `amount` must not be negative and must be finite, `speed` must be greater than 0, and `text` must not be `null`. Nothing else throws to the caller.

## Threading and safety

- Main thread only.
- Each call runs inside a try/catch that logs once through `ApiLog` and returns `false`.

## Shake

- `Shake(duration, amount)` calls the game's static `CameraShake.Shake` (`Player/CameraShake.cs:24`). `duration` is in seconds and `amount` is the maximum offset per frame, in world units.
- A new shake replaces the one in progress. The game's `Shake` stops its own coroutine first (`Player/CameraShake.cs:26`), so a mod shake cancels a game shake and the reverse. Health hits use the same call (`Player/PlayerStatsHealth.cs:145`).
- The shake pauses while `PauseMenu.paused` is set (`Player/CameraShake.cs:68`).
- A shake in progress is restored first. Before a new shake, the camera goes back to the base position from before the running shake, so an interrupted shake leaves no offset. This is done only while a shake is running, and it covers game shakes too.

## FadeToBlack and FadeIn

- `FadeToBlack(speed)` calls the game's `BlackCanvas.FadeIn` (`UI/BlackCanvas.cs:33`). It fades the black canvas up to full black and leaves it there until `FadeIn` runs. It does not change player controls.
- `FadeIn(speed)` calls `fadeCanvasGroup.StartFadeOut(speed)` on the same canvas (`AudioVisual/FadeCanvasGroup.cs:45`). That is what the game's `BlackCanvas.FadeOut` does (`UI/BlackCanvas.cs:38`), minus the `ControlsEnabled` call on line 41.
- Why the facade skips `BlackCanvas.FadeOut`: `ControlsEnabled` removes only its own holder (`Core/GameController.cs:324`). It also resets the two camera look flags unconditionally (`ControlsEnabledMove` at `:389`, which calls `ThirdPersonCameraController.EnableMovement` and `EnableFirstPersonMovement`). A fade from a mod during dialogue or a cutscene would free the camera that the cutscene locked. The facade does the same fade without that call. Do not call `BlackCanvas.ShowBlackCanvas` either: it disables movement and keys (`UI/BlackCanvas.cs:21`).
- `speed` is the game's fade step per tick. The game's `StartFadeInAndOut` uses `0.1f` by default (`UI/BlackCanvas.cs:64`), and that is the default here. The game also uses `0.5f` for tenement fades (`World/TenementController.cs:808`). Smaller values are slower. Steps are paced by `WaitForSecondsRealtime`, and the canvas's `realTime` flag changes the step size (`AudioVisual/FadeCanvasGroup.cs:63-72`). Not verified in game.
- A new fade on the same canvas stops the one in progress (`AudioVisual/FadeCanvasGroup.cs:33`, `:52`). The game uses the same canvas for its own blackouts (`Events/BlackoutTrigger.cs:93`, `Events/BlackCanvasTrigger.cs`), treatment (`Player/TreatmentPhysicalHealth.cs:247`), dialogue (`UI/WaitingController.cs:409`) and tenement fades. A mod's `FadeIn` can lift a black screen the game is holding, and a mod's `FadeToBlack` can cut off a game fade in progress.
- Nothing is saved. Restore a faded screen with `FadeIn`.

## SetDeathMessage

- `SetDeathMessage(text)` calls `DeathMessage.ChangeDeathMessage` (`Player/DeathMessage.cs:26`), which sets the reason text.
- The text stays until the game calls `ChangeDeathMessage` again. The game does so in `Player/PlayerStatsUI.cs:298`. Set the text just before the death screen is shown (`Player/Death.cs:48`, `ShowDeathMessage`), or it may be replaced.
- Nothing is saved.

## FOV: not wrapped

- The game has no public method that sets an arbitrary FOV. `SetCameraFOV.SetFOV` copies the saved display FOV (`Player/SetCameraFOV.cs:13`).
- `SetCameraFOV.Update` (`Player/SetCameraFOV.cs:5`) compares the camera's FOV with the saved one every frame and resets it on any difference. A mod that writes `Camera.fieldOfView` directly would be reverted on the next frame, for any camera that has this component enabled.
- To restore the game's FOV after a direct write, change it through the game's display settings, which write `DisplaySave.fov`. Nothing is persisted by this facade.
