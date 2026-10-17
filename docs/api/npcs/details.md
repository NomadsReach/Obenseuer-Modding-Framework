---
title: Details
parent: NPCs
grand_parent: People
nav_order: 4
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later). Logs go to `BepInEx\logs\obenseuer.moddingframework.npcs.log`.

Unlisted items are internal and may change.

## When you can call it

- Call from the [main thread](../../glossary.md#main-thread), not a thread you started.
- It is safe to call at any time. Empty reads return `0`, `false`, `null` or an empty list.
- Only `Animals.Damage` changes the game.
- If a handler throws, the error is logged and the other handlers still run.
- Don't change scenes or NPCs from a handler.

## What to check for and what comes back

- `Npcs.IsAvailable` is also `false` if a feature group is off; reads still work.
- `Relationships.GetDisposition` and `Relationships.Has` return `0f` and `false` for a null or empty id.
- Apart from `Npcs.Find` and `Animals.Damage`, no argument can make a call throw.
- A failed read returns the default and logs its first failure.

## If something fails to set up

- If a game method cannot be changed, its feature group turns off and is logged.
- Only that group's events stop. Group names are under [Under the hood](under-the-hood.md).
