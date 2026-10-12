# Obenseuer Modding Framework surveillance API (surveillance)

Status: experimental. Not checked in game.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.surveillance`. Logs go to `BepInEx\logs\obenseuer.moddingframework.surveillance.log`.

Everything not listed here is internal. Items marked **experimental** are not checked in game.

## Rules

- **Main thread only.** Call every member and handle every event on Unity's main thread.
- **Availability.** Every member is safe at any time: main menu, loading, no save loaded. Reads return a neutral default (`false`, or an empty list). `Surveillance.SetCameraActive` returns `false` when no camera has the id.
- **Events.** `Surveillance.IsAvailable` is false when the surveillance hooks were not installed or the owner was disabled. The events never fire then. The reads and writes keep working.
- **All or nothing.** The three hooks are one patch set under the owner `obenseuer.moddingframework.surveillance`. If any hook is missing in this build, the owner is disabled, one warning is logged, and the events never fire.
- **Crime records, fines and arrests** are in the Law facade (`docs\api-story.md`). Chases are in the Pursuit facade (`docs\api\pursuit.md`). Surveillance does not repeat them.

## Reads

- `List<SurveillanceCameraInfo> Surveillance.Cameras()`: every surveillance camera in the loaded scenes, including ones whose GameObject is inactive. Each entry has:
  - `string Id`: the camera id, for `SetCameraActive`.
  - `bool Active`: the game's own `active` flag.
  - `float Range`: the game's `range` field, in metres.
  - `float Fov`: the game's `FOV` field, in degrees.
- `bool Surveillance.IsTrespassing`: `true` while any trespassing area has the game's `trespassing` flag set. The flag clears about 1 s after the player leaves an area (the game's cooldown), so `IsTrespassing` can still read `true` briefly after `TrespassLeft`. An area that is disabled for this player (for example a bathroom of the other sex) never counts.

## Writes

- `bool Surveillance.SetCameraActive(string id, bool active)`: switches a camera on or off by calling the game's own `SurveilanceCamera.TurnOn` or `TurnOff`, which also set the camera's witness and fire the game's on and off outputs. It returns `true` when the camera exists, and also when it is already in the requested state (the game is not called again). It returns `false` when no camera has the id. An empty or null `id` throws `ArgumentException`.

## Events

- `event Action<PlayerSpottedArgs> PlayerSpotted`: raised when a witness check passes with `invoke` on. Only crime checks make the game react; plain sight checks still raise the event. That is a camera or an NPC witness that sees the player during a game check, such as a theft or a crime check. It fires at most once per witness per frame. `PlayerSpottedArgs` has:
  - `SurveillanceSource Source`: `Camera` or `Witness`.
  - `string Id`: the id of the camera, or of the witness component for `Witness` (same id form as `Cameras()`).
  - `int NpcId`: the NPC id for an NPC witness, `0` for cameras and for witnesses that are not on an NPC.
  - It is not the camera's alarm. The game's own camera alarm (`DetectionTimer`, after `detectionTime` in-game seconds) is private and is not exposed.
  - `Law.Spotted` is the crime-only sighting event and has no source. Use it for "a crime was seen". Both can fire on the same game check; `PlayerSpotted` fires first, inside the game's witness loop, so handlers must not add or remove witnesses synchronously.
- `event Action<TrespassArgs> TrespassEntered`: raised once when the player first sits in a trespassing area whose flag is set. Disabled areas do not raise it.
- `event Action<TrespassArgs> TrespassLeft`: raised when the player's trigger exit happens for an area whose entry was seen. It fires at the physical exit, before the game's 1 s cooldown clears the flag. Crimes are not raised here. Trespass crimes are in `Law.Committed` (`CrimeKind.Trespassing`).
- `TrespassArgs.AreaId`: the id of the area, for both events.
- Entry tracking runs only while a `TrespassEntered` or `TrespassLeft` handler is subscribed. A handler added while the player is already inside gets `TrespassEntered` on the next physics frame in the area. `TrespassLeft` is raised only for areas whose entry the facade saw.
- All events are raised through `Events.Raise`, so one bad handler does not stop the others.

## Not in this version

- **Camera alarm.** The game's `OnPlayerDetected` output after the detection timer is not exposed. Mods can react to the `PlayerSpotted` event instead.
- **Camera placement, range and FOV writes.** The game has public `range` and `FOV` fields, but no game method sets them, so they are read only.
- **Trespass area switches.** The game's `AreaDisabled` property is public, but it is not exposed here.
- **Theft.** `Inventory` and `Logic_Thief` check witnesses through `Witness.IsWitnessed`, which fires `PlayerSpotted` (when a witness sees the player) and `Law.Spotted` (crime-only). Theft records are in Law.
