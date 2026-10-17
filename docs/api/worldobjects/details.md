---
title: Details
parent: World objects
grand_parent: Objects and devices
nav_order: 7
---

# Details

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

- Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
- Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\ObenseuerCore.log`.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- Reads give you a plain default when nothing is ready. With no save or scene ready, you get `null`, `false` or an empty list. The writing calls return `false`.
- The only errors thrown are `ArgumentException` for arguments that can never be valid.

## If something fails to set up

- The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.
- Mod data depends on one change to the game. If it cannot be made, `SetData` returns `false` and nothing is written to the save folder. `Find`, `GuidOf`, `IsSaved`, `InfoOf`, `Near` and `Spawn.Item` do not depend on it. <span class="tech">(the change is on `NewGame.SaveCharacter`, which empties the mod data when a new game starts; the framework logs one warning, and the mod data store registers with `SaveData`)</span>

## Limits

- A load that fails after it started leaves the mod data as it was. The next save then writes that data into the slot it names, like any save does.
