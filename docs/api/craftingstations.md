---
title: Crafting speed, quality and stations API
parent: API reference
nav_order: 105
---

# Crafting speed, quality and stations API

Experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owners: `obenseuer.moddingframework.craftingspeed`, `obenseuer.moddingframework.craftingquality` and `obenseuer.moddingframework.craftingstations`.
Files: `src/Api/CraftingSpeed.cs`, `src/Api/CraftingSpeedPatches.cs`, `src/Api/CraftingQuality.cs`, `src/Api/CraftingQualityPatches.cs`, `src/Api/CraftingStations.cs`.

Three small facades for crafting stations. `CraftingSpeed` scales how long a station takes, `CraftingQuality` scales the quality of what it makes, and `CraftingStations` starts, stops, pauses and resumes a station by its id. The station ids are `CraftingStationInfo.Id` from `Crafting` (see [Recipes, crafting and growing](recipes.md)).

Everything not listed here is internal.

## If a hook cannot be installed

The framework installs the speed and quality hooks at start. If a hook cannot be installed, that facade's availability flag is `false`, one warning is logged, and its setter returns `false`. `CraftingStations` installs no hooks. It calls the game's own methods and reports whether the station changed.

## Surface

```csharp
public static class CraftingSpeed
{
    public const string AllStations = "*";
    public static bool SpeedFactorsAvailable { get; }
    public static bool SetSpeedFactor(string modId, string stationKind, float speed);   // 0.01 to 4
    public static bool Clear(string modId);
}

public static class CraftingQuality
{
    public static bool QualityFactorAvailable { get; }
    public static bool SetQualityFactor(string modId, float factor);   // 0 to 4
    public static bool Clear(string modId);
}

public static class CraftingStations
{
    public static bool Start(string modId, string stationId);
    public static bool Stop(string modId, string stationId);
    public static bool Pause(string modId, string stationId);
    public static bool Resume(string modId, string stationId);
}
```

## Speed

`SetSpeedFactor(modId, stationKind, speed)` sets one mod's speed for one kind of station. `2` halves the craft time, `0.5` doubles it, and `1` is the game's own time.

- **Station kind.** The kind is the station's class name, the same string as `CraftingStationInfo.Kind`: `manufacturing`, `manufacturingProcess`, `Growing` or `Dishwasher`. `AllStations` (`"*"`) covers every kind. A `Dishwasher` is its own kind, so a factor for `manufacturing` does not reach it. Use `*` or `Dishwasher`.
- **What is scaled.** The craft time of `manufacturing` and `manufacturingProcess` stations. `Growing` stations never read the factor, so a factor for `Growing` does nothing.
- **Both keys apply.** The factor for the station's kind and the factor for `*` multiply. The product is clamped to 0.01 to 4, the same range as one factor. Two mods that each set 0.01 get 0.01, not 0.0001.
- **Time cap.** The scaled time never passes 2,000,000,000 seconds when a craft starts, so a very slow factor on a long recipe cannot overflow the game's whole-second timer and finish at once. A `manufacturingProcess` station works its time out again when its progress is read and when a save is loaded. If the station's equipment quality or the player's skill dropped since the start, that time can grow past the cap, and a recipe already within about 7 percent of it can overflow. This needs an absurdly slow factor and is not guarded.
- **Player setting.** The player's `Tweaks.CraftingSpeed` multiplies on top of a station factor.
- **Returns.** `SetSpeedFactor` returns `false` for an invalid mod id, an empty kind, a speed that is NaN or outside 0.01 to 4, `SpeedFactorsAvailable` being false, or a save or load running.
- **Rounding.** The game casts the time to a whole number of seconds. A short recipe at a high speed can round to 0 seconds, and the game then runs a zero-length timer.
- **Crafts already running.** A craft that has started keeps its scaled time. `Clear` and a scene load do not change it, and the game saves it with the craft. Only the next craft is affected.

## Quality

`SetQualityFactor(modId, factor)` sets one mod's factor for the quality of everything a station makes. `1` is the game's own quality, `2` doubles it, `0.5` halves it.

- **What is scaled.** Every output the station creates, including the outputs of a failed craft and the liquid and growing outputs. The result is clamped to 0.05 to 2, the range the game uses for crafted quality.
- **Process stations.** A liquid made by a `manufacturingProcess` station is scaled once, with the factor in force when the batch starts. The liquid left in the station keeps that quality, so `Clear` or a scene load during the batch does not change it.
- **Not scaled.** An output that already has a fixed quality, and every recipe that sets a fixed quality (for example the Gutter Oil recipes). The recipe info panel preview shows the game's own quality, not the factored one. Plant harvest is not a craft and is not covered.
- **Factor only.** There is no offset, because the game's quality formulas use the value as a multiplier.
- **Returns.** `SetQualityFactor` returns `false` for an invalid mod id, a factor outside 0 to 4, `QualityFactorAvailable` being false, or a save or load running.
- **Items already made.** The quality is saved on the item, so `Clear` and a scene load do not change items that exist.

## Multiple mods

Each mod keeps one speed per kind and one quality factor. For a given key the newest call applies. When a second mod sets a key that another mod already set, one warning names both mods, and the second mod's factor applies. A mod that calls again replaces its own value with no warning. `Clear(modId)` removes that mod's entries for every key, and the entry set before it applies again.

## Saved or not

Speed and quality factors are not saved. Every Single scene load clears them, so set them again from your `Events.SceneLoaded` handler. The finished items keep their quality, and a running craft keeps its time, because the game saves both.

## Start and stop

Both calls take the station id. They refuse while a crafting panel is open, while a save or load runs, for an empty id, and when two stations share the id. They return `true` only when the station changed state.

`Start(modId, stationId)`:

- **manufacturingProcess.** Starts the selected recipe when the station is loading items, powered, and holds the ingredients. `true` once the station is processing.
- **manufacturing.** Starts one craft of the selected recipe when the station is powered, has a free simultaneous slot and holds the ingredients. `true` when a craft started. A `manufacturing` station that needs the player at the panel (`RequirePlayer`) is refused, because its craft speeds up the game clock through the panel's waiting screen. A `Dishwasher` is refused too.
- **Growing.** Plants one unit of the selected recipe into a free growing spot when the station holds the ingredients. `true` when one spot fewer is free.
- **Ingredients.** The check counts the station's own slots and the extra containers from [Crafting sources](craftingsources.md).
- **Selecting the recipe.** The framework has no call that selects a recipe. Select it with the game's own `SetSelectedRecipe` before `Start`.

`Stop(modId, stationId)`:

- **Liquid recipes only.** `Stop` works on a `manufacturingProcess` station that is processing a liquid recipe. It is refused for a solid recipe. The game ends a stopped process through its normal finish. For a solid recipe that would hand out the full outputs at once, so a start followed by a stop would skip the process time.
- **A stopped liquid recipe fails.** The game does the same when the player presses Abort. The inputs are consumed, the batch counts as failed, and the station gives the recipe's fail outputs and ends in the failed stage until it is emptied. `true` when the station left the processing stage.
- **No second payout.** The stopped batch's timer is removed, so the station does not finish the batch again later. The game's own Abort button leaves that timer running.
- **Other stations.** `manufacturing` and `Growing` stations have no stop in the game, so there is none here.

## Pause and resume

`Pause(modId, stationId)` freezes a `manufacturingProcess` station that is processing, and `Resume(modId, stationId)` starts it again. Both refuse for the same reasons as `Start` and `Stop`, and return `true` only when the station changed state.

- **What is frozen.** The batch timer keeps the time it has left and stops counting down. The station's hum stops too. The power switch freezes the same timer, but `Pause` does not touch power. Fuel keeps burning while a station is paused, because fuel use runs on the game clock, not on `Timer.stopped`.
- **Resume.** It starts the timer and the hum again. It refuses while the station has no power, so a batch the game holds for lack of power stays held.
- **Power changes the pause.** Losing power keeps the station frozen. Only the power coming back on, or the station being switched back on, clears the pause.
- **Session only.** The framework keeps no record of a pause, so any mod can resume a station another mod paused, and there is no `Clear`. A save made while paused loads running, and a scene load clears the pause.
- **No craft events.** A pause does not raise the craft start or craft stop events. The game raises them on power out and on Abort, and the framework cannot raise them.
- **Stop.** `Stop` still works on a paused liquid batch and fails it as it would a running one.

## Not in this area

- Stopping `manufacturing` and `Growing` stations. A craft takes its ingredients when it starts and a plant takes its seeds, and the game has no cancel that hands them back. Its only removals pay out.
- Starting a `RequirePlayer` station. Its craft speeds up the game clock behind the panel's wait screen, and its timer freezes when the player leaves the panel.
- Pausing a growing station: use `GrowingStations.SetPaused` (see [Growing stations](growingstations.md)).
