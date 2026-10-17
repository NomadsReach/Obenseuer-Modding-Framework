---
title: Details
parent: Registry
grand_parent: Items and crafting
nav_order: 11
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Files: `src/Api/RegistryIds.cs`, `src/Api/RegistryItems.cs`, `src/Api/RegistryRecipes.cs`, `src/Api/RegistryPatches.cs`, `src/Api/RegistryManifest.cs`, `src/Api/RegistryPlaceholders.cs`, `src/Api/RegistryMissing.cs`, `src/Api/RegistrySelfTest.cs`, and for models and icons `src/Api/ModelRegistry.cs`, `src/Api/ModelBuild.cs`, `src/Api/ModelPatches.cs`, `src/Api/ModelObj.cs`, `src/Api/ModelMtl.cs`, `src/Api/ModelIcons.cs`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes some of the game's methods when it starts, so it can add your items and recipes. The changes belong to four feature groups, and these are the names you will see in the log: `obenseuer.moddingframework.registry` (items and recipes), `obenseuer.moddingframework.registry.placeholders` (the placeholder for missing items), `obenseuer.moddingframework.models` (3D models) and `obenseuer.moddingframework.icons` (icons).

## When you can call it

- **Call everything from your mod's normal code (`Awake`, `OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- **Call `Define` before the game loads its item list and its recipe list.** The usual place is your plugin's `Awake`. The item list first loads in `ItemDatabase.Awake`, and the recipe list in `RecipeDatabase.Awake` (or `OnEnable`, if the list is empty).
- **After the first load, `Define` writes an error to the log and returns -1.** It is -1 and not 0 because 0 is a real item (the coin). A caller that passed a refused number on as an output would otherwise get a coin, while -1 is rejected as an invalid item number.
- **Defining the same mod id and key again before the load gives you the same number and keeps the first definition.**
- **Each time the game assigns an item list, every item you defined is added again.** The game assigns a list in `Awake`, `OnEnable` and `Reload`. `ItemRegistry.Loaded` fires once for each list.

## What to check for and what comes back

- **`IdFor` never fails for a valid mod id and key.** It only does arithmetic.
- **`Define` throws an `ArgumentException` for an invalid argument, or for a number that another key already holds.** A `null` definition throws an `ArgumentNullException`. Nothing else is thrown to your code.
- **If the game itself fails while the framework works, you get -1, `false`, `null` or an empty list instead of an error.** The first failure of each member goes to the log as an error.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the item and recipe changes can't be set up, `Define` returns -1 from then on.** The framework changes five game methods. If any of them can't be changed, it removes all the changes in the feature group `obenseuer.moddingframework.registry` and writes error lines and a warning to the log. If the player has turned on the framework's setting that refuses all changes on a game version other than 0.5.08, nothing is changed on such a version and `Define` also returns -1.
  - Technical: the five methods are `ItemDatabase.DeSerialize`, `ItemDatabase.Awake`, `OnEnable` and `Reload`, and `RecipeDatabase.LoadDatabase`. The setting is `RefuseUnknownBuild` in the `Safety` section of the framework's config, and it is off by default.
- **The placeholders for missing items have their own feature group, so a failure there does not stop `Define`.** If they can't be set up, an unknown item number shows as a coin again, as in the game without the framework.
- **`ItemRegistry.IsAvailable` and `RecipeRegistry.IsAvailable` are `true` when the changes are set up and their feature group is not turned off.** `ItemIcons.IsAvailable` and `ModelRegistry.IsAvailable` tell you the same for icons and models.
