---
title: FOV override
parent: Screen effects
grand_parent: Screen and UI
nav_order: 6
---

# FOV override

FOV is the field of view: how wide the camera's view is, in degrees. The override is also how you make a zoom.

```csharp
public static bool SetFovOverride(float fov);                // set the field of view, with no owner
public static bool ClearFovOverride();                       // remove that value
public static bool SetFovOverride(string owner, float fov);  // set the field of view, as your mod
public static bool ClearFovOverride(string owner);           // remove your value
```

- **`SetFovOverride` sets how wide the view is, in degrees, and keeps it there until you clear it.** A small number zooms in. It returns `true` when the value was stored and `false` when `IsAvailable` is `false`. A number below 30 is changed to 30 and above 120 to 120. A value that is not a real number throws an `ArgumentException`.
  - Technical: the limits are not taken from the game's settings slider, because the slider's minimum and maximum are set in the Unity editor and are not in the game code.
- **`ClearFovOverride` brings back the player's own field of view.** It returns `true` when it removed a value and `false` when none was set. The view goes back on the next frame.
  - Technical: on the next frame `SetCameraFOV.Update` finds the camera different from `DisplaySave.fov` and writes the saved FOV back.
- **The player's saved field of view and the options screen are not changed.** The override is only in memory and is not saved.
  - Technical: `DisplaySave` is never written.
- **A normal level load clears the override, but loading extra scenes in the background does not.** The clear happens before your `Events.SceneLoaded` handler runs, so a handler that sets it keeps it. Opening or closing a menu does not clear it either, so it stays through the pause menu.
  - Technical: only a Single scene load clears it, so an additive streaming load keeps it.
- **While an override is set, the Field of View slider in the options menu looks as if it does nothing.** The slider still changes the saved value, but the camera does not follow it until the override is cleared or a level loads.
  - Technical: `SetCameraFOV.Update` is skipped while an override is set.
- **The game's radiation check uses the player's saved view while an override is set.**
  - Technical: `RadiationEffectUI` passes the saved `DisplaySave.fov` to `RadiationController.LookingAtRadiation`, not the camera field of view.
- **The override applies to every camera the game gives a field of view.** If the framework's change to the game's camera code throws an error, it is logged once and the game's own code runs for that frame.
  - Technical: every camera with a `SetCameraFOV` component.
- **With an `owner`, each mod has its own value, so two mods don't overwrite each other silently.** The value set last is the one shown. When that mod clears its value, the value set before it shows again. Without an `owner`, the calls use one shared value of their own, and they never remove a mod's value.
- **A normal level load clears every value.** `SetFovOverride` also works inside `Events.SceneLoaded` and while a save is loading or being written.
- **This is the zoom feature.** A zoom key calls `SetFovOverride(owner, fov)` when pressed and `ClearFovOverride(owner)` when let go, as the quick example does. The [Player](../player.md) page has the camera.
  - Technical: `SetCameraFOV.Update` writes `fieldOfView` only when it differs from `DisplaySave.fov`, and `SetFOV` always writes it. The framework's prefix returns `false` while an override is set, so the game's write does not run and the override holds.
