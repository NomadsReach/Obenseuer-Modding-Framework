---
title: Details
parent: Loot pools
grand_parent: Items and crafting
nav_order: 6
---

# Details

- Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
- Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.loot.log`.
- Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.
- The framework changes some of the game's methods when it starts, so it can add your items to the pools and tell your mod when a container is filled. All of those changes belong to one feature group, `obenseuer.moddingframework.loot`, the name you will see in the log. At start the framework logs `loot pools installed` or `loot pools not installed`.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, `Awake`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- Your `ContainerFilled` handler runs inside the game's own call, right after the fill. Do not change scenes from inside it.

## How the pools live and what is saved

- The game rebuilds its pools from its data file, and a rebuild drops every in-memory change. The framework keeps its own record of your entries and puts them back after each rebuild, and again when the item registry has finished loading. <span class="tech">(The pools are in the `StorageSpawnCategoryDatabase` component. Each rebuild reads `StreamingAssets\StorageSpawnCategory.json`. An entry whose item later leaves the item database is skipped on the next pass. `StorageSpawnCategoryDatabase.Save` is never called, so nothing is written to the game's files.)</span>
- Your pool entries are never saved. Your mod adds them again at every start, so a removed mod leaves no item number in the pools on the next start.
- An item in a container keeps its number in the save. After a mod is removed, those item numbers stay in old saves, as with any mod item.

## What to check for and what comes back

- If the game itself fails while the framework works, you get an empty answer instead of an error. The answer is the plain default (an empty list or `false`), and the first failure of each member goes to the log as an error. Later failures of that member are not logged again.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the loot changes can't be set up, `IsAvailable` is `false`. Then `Add`, `Remove`, `DefineCategory` and `ContainerFilled` do nothing, and `Categories` and `PoolFor` keep working. The feature group `obenseuer.moddingframework.loot` turns off and error lines and a warning go to the log. <span class="tech">(The game methods involved are `StorageSpawnCategoryDatabase.ConstructSpawnCategoryDatabase` and `Storage.SpawnItems`.)</span>
