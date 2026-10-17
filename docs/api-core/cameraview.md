---
title: CameraView
parent: Framework core classes
grand_parent: Framework basics
nav_order: 10
---

# CameraView

Obenseuer is played in first person. The game also has a third person camera, but it is switched off in the normal game. `CameraView` lets your mod ask the game to use it. It does so through the game's own camera state, and it never moves a camera by hand.

Vanilla 0.5.08 is first person only. Third person is a mode the game left dormant, so the player's body, animations and clipping may look rough in it.

```csharp
public static class CameraView
{
    public static bool IsAvailable { get; }          // true when the game has a camera to control; false on the main menu
    public static bool IsFirstPerson { get; }        // true when the view is first person
    public static bool ThirdPersonAllowed { get; }   // false when third person cannot be used right now (see below)
    public static bool SetThirdPerson(float distance = 2.5f);  // go to third person at this distance; false when it is not allowed
    public static void SetFirstPerson();             // go back to first person
    public static bool Toggle();                     // switch to the other view; false when not available or refused
    public static event Action<bool> ModeChanged;    // the view changed; true means it is now first person
}
```

- **Nothing breaks on the main menu.** With no camera, the properties return `false` and the methods return `false` or do nothing.
- **`ThirdPersonAllowed` is `false` in three cases.** There is no camera, or a camera zone in the scene is forcing first person, or an object in the scene has asked the game to turn off camera zoom.
  - Technical: the zone case is a `PlayerCameraTrigger` that forces first person, and the zoom case is a non-empty `disabledZoomList`.
- **`SetThirdPerson(distance)` sets the camera distance and turns third person on.** The distance is kept between the game's minimum and maximum. It returns `false` when third person is not allowed.
  - Technical: it sets `currentDistance` clamped to `minDistance..maxDistance`. It clears `disableThirdPerson` and `firstpersonOnly`, and keeps the vanilla values so `Tweaks.Camera` can put them back. The game's `FixedUpdate` then calls `ThirdPerson()` on the next physics step.
- **The view changes on the next physics step, not inside the call.** `ModeChanged` fires then, not while `SetThirdPerson` runs.
- **A wall closer than about 0.6 metres keeps you in first person.** The game pulls the camera in when something is in the way.
  - Technical: the game's collision sets `distance` below 0.5.
- **`SetFirstPerson` does not turn the view.** The direction you are looking stays as it was, so the picture does not jump.
  - Technical: the controller's `rotX` and `rotY` stay as they are. It sets `currentDistance = 0` and calls `FirstPerson()`.
- **A camera zone can force first person, and the framework follows it.** A zone counts as forcing from the moment it asks for first person until it asks to stop, or until it is destroyed. A zone that is only switched off, without asking to stop, still counts. No game code uses these zones yet, so how a zone is entered and left depends on the scene and has not been checked.
  - Technical: the framework watches `PlayerCameraTrigger.FirstPersonOnly(bool)` with a postfix. A trigger counts from its `FirstPersonOnly(true)` call until its `FirstPersonOnly(false)` call, or until it is destroyed.
- **If the framework cannot watch the camera zones, third person is refused instead of forced.** `ThirdPersonAllowed` then stays `false`.
  - Technical: this is when the postfix on `PlayerCameraTrigger.FirstPersonOnly` cannot be installed.
- **The game's own first-person lock does not count as a zone.** The game sets that lock itself for the whole game, so it is not treated as a forced zone.
  - Technical: `ThirdPersonCameraCollision.Start` sets `firstpersonOnly` from `disableThirdPerson`.
- **The game saves the camera mode.** A save made in third person stores the cleared lock flags, so loading it restores third person.
  - Technical: the game saves `firstperson`, `currentDistance`, `disableThirdPerson` and `firstpersonOnly` (the public fields without `fsIgnore` in the game's `SaveController`).
- **The first-person lock can come back when a new camera starts.** A new `ThirdPersonCameraCollision` runs `Start` again, which sets the lock again. While `ThirdPersonToggle` or `AllowScrollZoom` is on, the framework clears it again on the next frame. It has not been checked that `Start` runs on every scene load.
- **Use `CameraView` rather than the game's camera methods, so held items stay right.** The held-item code listens to the same view-change event, and the game's `FirstPerson()` and `ThirdPerson()` fire it.
  - Technical: `PlayerHandItems.CameraModeChange` is subscribed to the same static event.

This plugin logs every change of view, and switches to third person once at start if the game allows it.

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

public class ChaseCam : BaseUnityPlugin
{
    private void Awake()
    {
        CameraView.ModeChanged += firstPerson => Logger.LogInfo(firstPerson ? "first person" : "third person");
    }

    private void Start()
    {
        if (CameraView.ThirdPersonAllowed) CameraView.SetThirdPerson(3f);
    }
}
```
