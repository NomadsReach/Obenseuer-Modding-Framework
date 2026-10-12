# Obenseuer Modding Framework pursuit API (pursuit)

Status: experimental. Not checked in game.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.pursuit`. Logs go to `BepInEx\logs\obenseuer.moddingframework.pursuit.log`.

Everything not listed here is internal. Items marked **experimental** are not checked in game.

## Rules

- **Main thread only.** Call every member and handle every event on Unity's main thread.
- **Availability.** Every member is safe at any time: main menu, loading, no save loaded. Reads return a neutral default (`false`, or an empty list). `Pursuit.StopAll` returns `false` when no crime state exists.
- **Events.** `Pursuit.IsAvailable` is false when the pursuit hooks were not installed or the owner was disabled. The events never fire then. The reads keep working.
- **All or nothing.** The pursuit hooks are one patch set under the owner `obenseuer.moddingframework.pursuit`. If any hook is missing in this build, the owner is disabled, one warning is logged, and the events never fire.
- **Fines and arrests** are in the Law facade (`docs\api-story.md`). Pursuit does not repeat them.

## Reads

- `bool Pursuit.IsPursued`: `true` while a police officer on the map is in the chase state. Questioning and searching do not count. A police dialogue does not count.
- `List<int> Pursuit.ActivePolice()`: the NPC ids of the police the game has registered as active on the map. Each id gives a non-null `Npcs.Get(id)`. Not all of them are chasing. Use `IsPursued` for the chase.

## Writes

- `bool Pursuit.StopAll()`: calls the game's own stop (`Crime.StopPolicePursuits`), which the game runs on arrest and on teleport to prison. It ends chases and questioning for every police officer in the NPC database. It does not change crime records, fines or the arrest state. It returns `true` when the call ran, also when no one was chasing, and `false` when no crime or NPC state exists. The game marks each officer as having arrested the player, so they ignore the existing crime record until a new crime adds more than 100 to it or a chase is started.

## Events

- `event Action Started`: raised when `IsPursued` changes from `false` to `true`. It fires once for the first officer to start a chase, not once per officer.
- `event Action Ended`: raised when `IsPursued` changes from `true` to `false`.
- Both are edge events. The state is checked after a police officer's state changes, after an officer is arrested or stops, and after a save loads. A subscriber added during a pursuit does not get `Started` for it. Nothing is checked while no handler is subscribed.
- Loading a save stops every officer first, so a player who was pursued gets `Ended` when the load begins. If the loaded save is in a chase, `Started` follows.

## Not in this version

- **Spotted.** The police field-of-view callback repeats every frame while the player is visible, so it has no single sighting point. For a witnessed crime, use `Law.Spotted`.
- **Robbers.** `Act_Robber` has its own `Chase` and `Aggro` states. These are not police and are not part of this facade.
- **Starting a chase.** Not exposed. The game starts chases from its own crime flow (`Crime.CallPolice`, `NPC/Act_Police.cs:487`).
