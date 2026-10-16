---
title: Obenseuer Modding Framework pursuit API (pursuit)
parent: API reference
nav_order: 57
---

# Obenseuer Modding Framework pursuit API (pursuit)

Status: experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.pursuit`. Logs go to `BepInEx\logs\obenseuer.moddingframework.pursuit.log`.

Everything not listed here is internal. Items marked **experimental** may change.

Use `Pursuit` to find out whether the police are chasing the player, to call the police or stop all chases, and to get told when a chase starts and ends.

## Rules

- **Main thread only.** Call every member and handle every event on Unity's main thread.
- **Availability.** Every member is safe at any time: main menu, loading, no save loaded. Reads return a neutral default (`false`, or an empty list). `Pursuit.StopAll` returns `false` when no crime state exists, and while a save loads or saves (`SaveData.Busy`).
- **Events.** `Pursuit.IsAvailable` is false when the pursuit hooks were not installed or the owner was disabled. The events never fire then. The reads keep working.
- **All or nothing.** The pursuit hooks are one patch set under the owner `obenseuer.moddingframework.pursuit`. If any hook is missing in this build, the owner is disabled, one warning is logged, and the events never fire.
- **Fines and arrests** are in the Law facade ([api-story.md](../api-story.md)). Pursuit does not repeat them.

## Reads

- `bool Pursuit.IsPursued`: `true` while a police officer on the map is in the chase state. Questioning and searching do not count. A police dialogue does not count.
- `List<int> Pursuit.ActivePolice()`: the NPC ids of the police the game has registered as active on the map. Each id gives a non-null `Npcs.Get(id)`. Not all of them are chasing. Use `IsPursued` for the chase.

## Writes

- `bool Pursuit.CallPolice(bool severe, bool anyDistance)`: makes the police on the map react to the player, as the game's own crime call does. `severe` true starts a chase. `severe` false starts questioning, which does not count as a chase, so `IsPursued` stays `false`. With `anyDistance` false, only officers within 15 m of the player react. With `anyDistance` true, every officer on the map reacts. It returns `true` when at least one officer reacted. It returns `false` and alerts nobody when no crime state exists, a save loads or saves (`SaveData.Busy`), an arrest is in progress, a level change or the loading screen is active, the player is in prison, or no officer is in range. The officers are read from a copy of the police list, so one removed during the call is skipped, and one that throws is logged once while the others still react. A chase it starts raises `Started` like any other. The call is not synced in multiplayer, so use it on the host or in single player only.
- `bool Pursuit.StopAll()`: calls the game's own stop (`Crime.StopPolicePursuits`), which the game runs on arrest and on teleport to prison. It ends chases and questioning for every police officer in the NPC database. It does not change crime records, fines or the arrest state. It returns `true` when the call ran, also when no one was chasing, and `false` when no crime or NPC state exists. The game marks each officer as having arrested the player, so they ignore the existing crime record until a new crime adds more than 100 to it or a chase is started.

## Events

- `event Action Started`: raised when `IsPursued` changes from `false` to `true`. It fires once for the first officer to start a chase, not once per officer.
- `event Action Ended`: raised when `IsPursued` changes from `true` to `false`.
- Both are edge events. The state is checked after a police officer's state changes, after an officer is arrested or stops, and after a save loads. A subscriber added during a pursuit does not get `Started` for it. Nothing is checked while no handler is subscribed.
- Loading a save stops every officer first, so a player who was pursued gets `Ended` when the load begins. If the loaded save is in a chase, `Started` follows.

## Not in this version

- **Spotted.** The police field-of-view callback repeats every frame while the player is visible, so it has no single sighting point. For a witnessed crime, use `Law.Spotted`.
- **Robbers.** `Act_Robber` has its own `Chase` and `Aggro` states. These are not police and are not part of this facade.
