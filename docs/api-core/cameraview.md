---
title: CameraView
parent: Framework core classes
grand_parent: Framework basics
nav_order: 10
---

# CameraView

Obenseuer is played in first person. The game also has a third person camera, but it is switched off in the normal game. `CameraView` asks the game to use it through the game's own camera state. It never moves a camera by hand.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CameraView</code> <span>first and third person</span></p>

Check `IsAvailable` first, because there is no camera on the main menu. Use `SetThirdPerson`, `SetFirstPerson` or `Toggle` to change the view, and `IsFirstPerson` to see which one is on.

<div class="api-pair" markdown="1">

```csharp
public static class CameraView
{
    public static bool IsAvailable { get; }
    public static bool IsFirstPerson { get; }
    public static bool ThirdPersonAllowed { get; }
    public static bool SetThirdPerson(float distance = 2.5f);
    public static void SetFirstPerson();
    public static bool Toggle();
    public static event Action<bool> ModeChanged;
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetThirdPerson(distance)` | `bool` | Switches to third person at this distance. Returns `false` when third person is not allowed. |
| `SetFirstPerson()` | `void` | Switches back to first person. |
| `Toggle()` | `bool` | Switches to the other view. Returns `false` when it is not available or refused. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when the game has a camera to control. `false` on the main menu. |
| `IsFirstPerson` | `bool` | `true` when the view is first person. |
| `ThirdPersonAllowed` | `bool` | `false` when third person cannot be used right now. See Things to know. |

| Event | Gives you | When it fires |
|---|---|---|
| `ModeChanged` | `bool` | The view changes. `true` means it is now first person. |

</div>
</div>

## Putting it together

Logs every change of view, and switches to third person once at start if the game allows it.

<div class="api-example" markdown="1">

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

| Step | What it does |
|---|---|
| `CameraView.ModeChanged += ...` | Logs each change of view. |
| `CameraView.ThirdPersonAllowed` | Checks whether third person can be used right now. |
| `CameraView.SetThirdPerson(3f)` | Switches to third person at a distance of 3. |

</div>

## Things to know

- Nothing breaks on the main menu. With no camera, the properties return `false`, and the methods return `false` or do nothing.
- `ThirdPersonAllowed` is `false` when there is no camera, when a camera zone forces first person, or when an object has asked the game to turn off camera zoom. <span class="tech">(The zone case is a `PlayerCameraTrigger` that forces first person. The zoom case is a non-empty `disabledZoomList`.)</span>
- `SetThirdPerson(distance)` sets the camera distance and turns third person on. The game keeps the distance between its minimum and maximum. <span class="tech">(It sets `currentDistance` clamped to `minDistance..maxDistance`. It clears `disableThirdPerson` and `firstpersonOnly`, and keeps the vanilla values so `Tweaks.Camera` can put them back. The game's `FixedUpdate` then calls `ThirdPerson()` on the next physics step.)</span>
- The view changes on the next physics step, not inside the call. `ModeChanged` fires then, not while `SetThirdPerson` runs.
- A wall closer than about 0.6 metres keeps you in first person, because the game pulls the camera in when something is in the way. <span class="tech">(The game's collision sets `distance` below 0.5.)</span>
- `SetFirstPerson` does not turn the view. The direction you are looking stays as it was, so the picture does not jump. <span class="tech">(The controller's `rotX` and `rotY` stay as they are. It sets `currentDistance = 0` and calls `FirstPerson()`.)</span>
- A camera zone can force first person, and the framework follows it. A zone counts as forcing from the moment it asks for first person until it asks to stop, or until it is destroyed. <span class="tech">(The framework watches `PlayerCameraTrigger.FirstPersonOnly(bool)` with a postfix. A trigger counts from its `FirstPersonOnly(true)` call until its `FirstPersonOnly(false)` call, or until it is destroyed.)</span>
- The game's own first-person lock does not count as a zone. The game sets that lock itself for the whole game. <span class="tech">(`ThirdPersonCameraCollision.Start` sets `firstpersonOnly` from `disableThirdPerson`.)</span>
- The game saves the camera mode. A save made in third person stores the cleared lock flags, so loading it restores third person. <span class="tech">(The game saves `firstperson`, `currentDistance`, `disableThirdPerson` and `firstpersonOnly`, the public fields without `fsIgnore` in the game's `SaveController`.)</span>
- Use `CameraView` rather than the game's camera methods, so held items stay right. The held-item code listens to the same view-change event, and the game's `FirstPerson()` and `ThirdPerson()` fire it. <span class="tech">(`PlayerHandItems.CameraModeChange` is subscribed to the same static event.)</span>

## Limits

- Vanilla 0.5.08 is first person only. Third person is a mode the game left dormant, so the player's body, animations and clipping may look rough in it.
- A zone that is only switched off, without asking to stop, still counts. No game code uses these zones yet. How a zone is entered and left depends on the scene, and that has not been checked.
- If the framework cannot watch the camera zones, third person is refused instead of forced, and `ThirdPersonAllowed` stays `false`. <span class="tech">(This is when the postfix on `PlayerCameraTrigger.FirstPersonOnly` cannot be installed.)</span>
- The first-person lock can come back when a new camera starts. A new `ThirdPersonCameraCollision` runs `Start` again, which sets the lock again. While `ThirdPersonToggle` or `AllowScrollZoom` is on, the framework clears it again on the next frame. It has not been checked that `Start` runs on every scene load.
