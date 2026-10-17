---
title: Cameras and the checks
parent: Surveillance
grand_parent: Law and crime
nav_order: 2
---

# Cameras and the checks

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public static class Surveillance
{
    public static List<SurveillanceCameraInfo> Cameras();                      // every security camera in the scene you are in
    public static bool IsPlayerWitnessed(bool crime = true);                   // true when someone could see the player right now
    public static bool IsTrespassing { get; }                                  // true when the player is in a trespassing area
    public static bool SetCameraActive(string id, bool active);                // switches a camera on (true) or off (false)
    public static bool IsAvailable { get; }                                    // false when an event could not be set up
}

public sealed class SurveillanceCameraInfo    // a snapshot of one camera
{
    public string Id { get; }       // the camera's id, for SetCameraActive
    public bool Active { get; }     // true when the camera is switched on
    public float Range { get; }     // how far the camera sees, in metres
    public float Fov { get; }       // how wide the camera sees, in degrees
}
```

- **`Cameras()` lists every security camera in the scene, even those switched off in the scene.**
  - Technical: it includes cameras whose object is inactive. `Active` is the game's own `active` flag, `Range` is its `range` field and `Fov` is its `FOV` field. The id is the object's save GUID (see the [glossary](../../glossary.md#guid)), or its scene path when it has none.
- **`SetCameraActive(id, active)` switches a camera on or off.** The camera's own signals fire as they do when the game switches it. It returns `true` when the camera exists, and also when it is already in the state you asked for (nothing is done then). It returns `false` for an unknown, empty or `null` id (an empty or `null` id is also written to the log once), for a camera that has no way to see the player, and while a save is loading or saving.
  - Technical: it calls the game's `SurveilanceCamera.TurnOn` or `TurnOff`, which also set the camera's `Witness` component and fire the game's on and off outputs. It returns `false` when the camera has no `Witness` component.
- **`IsPlayerWitnessed()` tells you whether someone could see a crime right now.** It is `true` when at least one person or camera has the player in sight. It does not roll the dice the game rolls for a real sighting, so `true` means a crime committed now could be seen, not that it will be. It gives `false` with no save loaded. By default it skips watchers that do not look for crimes; pass `false` to count them too.
  - Technical: a witness is an NPC with a field of view or a surveillance camera, the same set the game's crime checks use. With `crime` true, witnesses with `dontDetectCrimes` are skipped. The game's own check (`Witness.IsWitnessed`) rolls `propability`, 0.5 by default. A camera counts when it is active and its own per-frame check had the player in view in the last frame. The call does not start the game's NPC detection cooldown, fire a camera's outputs or start its alarm timer, and it does not raise `PlayerSpotted` or `Law.Spotted`. Because it ignores the cooldown, it reports a witness that the game's own check would still skip for a moment after a detection.
- **`IsPlayerWitnessed` is slow, so do not call it every frame.** Call it at most a few times a second and keep the answer.
  - Technical: each call runs `FindObjectsByType` over every witness and one raycast for each NPC witness that has the player inside its range and view angle.
- **`IsTrespassing` is `true` while the player is in a trespassing area, and a moment after.** It stays `true` for about one second after the player leaves, so it can still be `true` right after `TrespassLeft`. An area that is turned off for this player, such as the other sex's bathroom, never counts.
  - Technical: it reads the game's `trespassing` flag on each `TrespassingArea`, which the game clears after a one second cooldown.
