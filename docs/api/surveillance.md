---
title: Obenseuer Modding Framework surveillance API (surveillance)
parent: API reference
nav_order: 79
---

# Obenseuer Modding Framework surveillance API (surveillance)

Status: experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.surveillance`. Logs go to `BepInEx\logs\obenseuer.moddingframework.surveillance.log`.

Everything not listed here is internal. Items marked **experimental** may change.

## Rules

- **Main thread only.** Call every member and handle every event on Unity's main thread.
- **Availability.** Every member is safe at any time: main menu, loading, no save loaded. Reads return a neutral default (`false`, or an empty list). `Surveillance.SetCameraActive` returns `false` when no camera has the id, and while a save loads or saves (`SaveData.Busy`).
- **Events.** `Surveillance.IsAvailable` is false when either surveillance group was not installed or the owner was disabled. Each event needs only its own group: `PlayerSpotted` needs the witness group, and `TrespassEntered` and `TrespassLeft` need the trespass group. The reads and writes keep working.
- **Per group.** `Witnessed` is under `obenseuer.moddingframework.surveillance.witness`, and `OnTriggerStay` and `OnTriggerExit` are under `obenseuer.moddingframework.surveillance.trespass`. If a hook is missing in this build, its group owner is disabled, one warning is logged, and only that group's events stop. `Surveillance.IsAvailable` is false while any group is missing.
- **Crime records, fines and arrests** are in the Law facade ([api-story.md](../api-story.md)). Chases are in the Pursuit facade ([pursuit.md](pursuit.md)). Surveillance does not repeat them.

## Reads

- `List<SurveillanceCameraInfo> Surveillance.Cameras()`: every surveillance camera in the active scene, including ones whose GameObject is inactive. Each entry has:
  - `string Id`: the camera id, for `SetCameraActive`.
  - `bool Active`: the game's own `active` flag.
  - `float Range`: the game's `range` field, in metres.
  - `float Fov`: the game's `FOV` field, in degrees.
- `bool Surveillance.IsPlayerWitnessed(bool crime = true)`: `true` when at least one witness has the player in sight right now. A witness is an NPC with a field of view or a surveillance camera, the same set the game's crime checks use. With `crime` true, witnesses that do not detect crimes are skipped. It does not roll the witness's chance: the game's own check (`Witness.IsWitnessed`) rolls `propability`, 0.5 by default, so `true` means a crime committed now could be seen, not that it will be. It does not start the game's NPC detection cooldown, fire a camera's outputs or start its alarm timer, and it does not raise `PlayerSpotted` or `Law.Spotted`. A camera counts when it is active and its own per-frame check had the player in view in the last frame. The answer is `false` with no save loaded. Because it ignores the cooldown, it reports a witness that the game's own check would still be skipping for a moment after a detection. Each call runs `FindObjectsByType` over every witness and one raycast for each NPC witness that has the player inside its range and view angle, so do not call it every frame: call it at most a few times a second and cache the answer.
- `bool Surveillance.IsTrespassing`: `true` while any trespassing area has the game's `trespassing` flag set. The flag clears about 1 s after the player leaves an area (the game's cooldown), so `IsTrespassing` can still read `true` briefly after `TrespassLeft`. An area that is disabled for this player (for example a bathroom of the other sex) never counts.

## Writes

- `bool Surveillance.SetCameraActive(string id, bool active)`: switches a camera on or off by calling the game's own `SurveilanceCamera.TurnOn` or `TurnOff`, which also set the camera's witness and fire the game's on and off outputs. It returns `true` when the camera exists, and also when it is already in the requested state (the game is not called again). It returns `false` when no camera has the id, when the camera has no `Witness` component, or while a save is loading or being written. An empty or null `id` returns `false` and is logged once.

## Events

- `event Action<PlayerSpottedArgs> PlayerSpotted`: raised when a witness check passes with `invoke` on. Only crime checks make the game react; plain sight checks still raise the event. That is a camera or an NPC witness that sees the player during a game check, such as a theft or a crime check. It fires at most once per witness per frame. `PlayerSpottedArgs` has:
  - `SurveillanceSource Source`: `Camera` or `Witness`.
  - `string Id`: the id of the camera, or of the witness component for `Witness` (same id form as `Cameras()`).
  - `int NpcId`: the NPC id for an NPC witness, `0` for cameras and for witnesses that are not on an NPC.
  - It is not the camera's alarm. The game's own camera alarm (`DetectionTimer`, after `detectionTime` in-game seconds) is private and is not exposed.
  - `Law.Spotted` is the crime-only sighting event and has no source. Use it for "a crime was seen". Both can fire on the same game check; `PlayerSpotted` fires first, inside the game's witness loop, so handlers must not add or remove witnesses synchronously.
- `event Action<TrespassArgs> TrespassEntered`: raised once when the player first sits in a trespassing area whose flag is set. Disabled areas do not raise it. If the player is moved more than 3 m inside an area whose exit was not reported (for example by a teleport), `TrespassLeft` is raised for that area and `TrespassEntered` again, because the next report is a new entry. A move of 3 m or less is not detected, and neither is a move out and back when the game reports no exit in between. The facade sees the player only on the game's `OnTriggerStay` and `OnTriggerExit` reports, so these misses are not closed.
- `event Action<TrespassArgs> TrespassLeft`: raised when the player's trigger exit happens for an area whose entry was seen. It is also raised when a scene change destroys such an area while the player is in it, with the area id seen at entry. It fires at the physical exit, before the game's 1 s cooldown clears the flag. Crimes are not raised here. Trespass crimes are in `Law.Committed` (`CrimeKind.Trespassing`).
- `TrespassArgs.AreaId`: the id of the area, for both events.
- Entry tracking runs only while a `TrespassEntered` or `TrespassLeft` handler is subscribed. A handler added while the player is already inside gets `TrespassEntered` on the next physics frame in the area. `TrespassLeft` is raised only for areas whose entry the facade saw.
- All events are raised, so one bad handler does not stop the others.

## Not in this version

- **Camera alarm.** The game's `OnPlayerDetected` output after the detection timer is not exposed. Mods can react to the `PlayerSpotted` event instead.
- **Camera placement, range and FOV writes.** The game has public `range` and `FOV` fields, but no game method sets them, so they are read only.
- **Trespass area switches.** The game's `AreaDisabled` property is public, but it is not exposed here.
- **Theft.** `Inventory` and `Logic_Thief` check witnesses through `Witness.IsWitnessed`, which fires `PlayerSpotted` (when a witness sees the player) and `Law.Spotted` (crime-only). Theft records are in Law.
