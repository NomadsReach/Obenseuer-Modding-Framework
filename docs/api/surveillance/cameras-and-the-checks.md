---
title: Cameras and the checks
parent: Surveillance
grand_parent: Law and crime
nav_order: 2
---

# Cameras and the checks

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail you can skip on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Surveillance</code> <span>cameras and the crime checks</span></p>

This is the class to start from. `Cameras()` lists the security cameras, `SetCameraActive` switches one on or off, and `IsPlayerWitnessed` and `IsTrespassing` tell you whether the player can be seen or is on trespassing ground right now.

<div class="api-pair" markdown="1">

```csharp
public static class Surveillance
{
    public static List<SurveillanceCameraInfo> Cameras();
    public static bool IsPlayerWitnessed(bool crime = true);
    public static bool IsTrespassing { get; }
    public static bool SetCameraActive(string id, bool active);
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `Cameras()` | `List<SurveillanceCameraInfo>` | Every security camera in the scene you are in. |
| `IsPlayerWitnessed(crime)` | `bool` | `true` when someone could see the player right now. |
| `SetCameraActive(id, active)` | `bool` | Switches a camera on (`true`) or off (`false`). |

| Property | Type | What it means |
|---|---|---|
| `IsTrespassing` | `bool` | `true` while the player is in a trespassing area, and a moment after. |
| `IsAvailable` | `bool` | `false` when an event could not be set up. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SurveillanceCameraInfo</code> <span>a snapshot of one camera</span></p>

`Cameras()` gives you one of these for each security camera. Pass its `Id` to `SetCameraActive`. <span class="tech">(It is built from the game's `SurveilanceCamera`.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class SurveillanceCameraInfo
{
    public string Id { get; }
    public bool Active { get; }
    public float Range { get; }
    public float Fov { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The camera's id, for `SetCameraActive`. |
| `Active` | `bool` | `true` when the camera is switched on. |
| `Range` | `float` | How far the camera sees, in metres. |
| `Fov` | `float` | How wide the camera sees, in degrees. |

</div>
</div>

## Things to know

- `Cameras()` lists every security camera in the scene, even those switched off in the scene. <span class="tech">(It includes cameras whose object is inactive. `Active` is the game's own `active` flag, `Range` is its `range` field and `Fov` is its `FOV` field. The id is the object's save GUID (see the [glossary](../../glossary.md#guid)), or its scene path when it has none.)</span>
- `SetCameraActive(id, active)` switches a camera on or off. The camera's own signals fire as they do when the game switches it. It returns `true` when the camera exists, and also when it is already in the state you asked for (nothing is done then). <span class="tech">(It calls the game's `SurveilanceCamera.TurnOn` or `TurnOff`, which also set the camera's `Witness` component and fire the game's on and off outputs. It returns `false` when the camera has no `Witness` component.)</span>
- `IsPlayerWitnessed()` tells you whether someone could see a crime right now. It is `true` when at least one person or camera has the player in sight. It does not roll the dice the game rolls for a real sighting, so `true` means a crime committed now could be seen, not that it will be. By default it skips watchers that do not look for crimes; pass `false` to count them too. <span class="tech">(A witness is an NPC with a field of view or a surveillance camera, the same set the game's crime checks use. With `crime` true, witnesses with `dontDetectCrimes` are skipped. The game's own check (`Witness.IsWitnessed`) rolls `propability`, 0.5 by default. A camera counts when it is active and its own per-frame check had the player in view in the last frame. The call does not start the game's NPC detection cooldown, fire a camera's outputs or start its alarm timer, and it does not raise `PlayerSpotted` or `Law.Spotted`. Because it ignores the cooldown, it reports a witness that the game's own check would still skip for a moment after a detection.)</span>
- `IsPlayerWitnessed` is slow, so do not call it every frame. Call it at most a few times a second and keep the answer. <span class="tech">(Each call runs `FindObjectsByType` over every witness and one raycast for each NPC witness that has the player inside its range and view angle.)</span>
- `IsTrespassing` is `true` while the player is in a trespassing area, and a moment after. It stays `true` for about one second after the player leaves, so it can still be `true` right after `TrespassLeft`. An area that is turned off for this player, such as the other sex's bathroom, never counts. <span class="tech">(It reads the game's `trespassing` flag on each `TrespassingArea`, which the game clears after a one second cooldown.)</span>

## Limits

- `IsPlayerWitnessed()` gives `false` with no save loaded.
- `SetCameraActive` returns `false` for an unknown, empty or `null` id (an empty or `null` id is also written to the log once), for a camera that has no way to see the player, and while a save is loading or saving.
