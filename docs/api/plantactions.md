---
title: Plant actions
parent: Animals and plants
grand_parent: API reference
nav_order: 6
---

# Plant actions

## What it is

Plants grow in growing spots, such as the beds of a growing station. This API lets your mod change a single plant: harvest it, restart it, set how old it is, change how old it must be to be ripe, pause it, make it rot, or plant a new one in an empty spot. It can also pick up a plant that the player takes by hand. Listing the plants and getting told about a harvest are on the [Recipes page](recipes.md) (`Plants.All`, `Plants.Harvested`, `Plants.Picked`). Plant ids are the ids that `Plants.All` gives you. To act on a whole station at once, use [Growing stations](growingstations.md).

## When to use it

- Harvest a plant for the player, or harvest every ripe plant.
- Make a plant ripe at once, or restart it.
- Change how long a plant takes to grow.
- Pause a plant, or make it rot.
- Put a new plant in an empty growing spot.
- Pick up a plant that is picked by hand.

## Quick example

This mod waits for you to press F8. Then it harvests every plant in the scene that is fully grown, and logs how many it harvested.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.harvestall", "Harvest All", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class HarvestAll : FrameworkMod
{
    private ModKeybind harvestKey;

    protected override void OnLoad()
    {
        harvestKey = Mod.Key("Harvest", "Harvest every ripe plant", KeyCode.F8);
    }

    private void Update()
    {
        if (harvestKey == null || UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (!harvestKey.Down()) return;
        int harvested = 0;
        foreach (PlantInfo plant in Plants.All())
        {
            if (plant.FullyGrown && PlantActions.Harvest(Mod.Id, plant.Id)) harvested++;
        }
        Log.Info("Harvested " + harvested + " plants.");
    }
}
```

Every call takes your mod's id first, which is `Mod.Id`. Each call gives back `true` when it did the job and `false` when it did not. See the [keybinds guide](../guides/getting-started/keybinds.md) for `Mod.Key`.

## Common tasks

Make a plant fully grown, then harvest it. In the next snippets, `plantId` is the `Id` of a plant from `Plants.All()`.

```csharp
PlantActions.SetAgePercent(Mod.Id, plantId, 100);
PlantActions.Harvest(Mod.Id, plantId);
```

Make a plant ripen faster by lowering the age it must reach.

```csharp
PlantActions.SetFullGrownAge(Mod.Id, plantId, 5);
```

Stop a plant from growing, and let it grow again.

```csharp
PlantActions.Pause(Mod.Id, plantId, true);
PlantActions.Pause(Mod.Id, plantId, false);
```

Plant an item in an empty growing spot. `spotId` is the `SpotId` of the spot and `itemId` is the id of an item that grows.

```csharp
if (!PlantActions.Plant(Mod.Id, spotId, itemId)) Log.Info("Nothing was planted.");
```

Pick up a plant that is picked by hand. See [Growing stations](growingstations.md) for how to find the spot.

```csharp
PlantActions.HarvestSpot(Mod.Id, spotId);
```

## Reference

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### The calls

```csharp
public static class PlantActions
{
    public static bool Harvest(string modId, string plantId);                  // harvests a plant the way the player does
    public static bool Reset(string modId, string plantId);                    // sets the plant's age back to 0
    public static bool SetAgePercent(string modId, string plantId, int percent);   // sets the age as a percent of full grown, 0 to 100
    public static bool SetFullGrownAge(string modId, string plantId, int age);     // sets the age at which the plant is ripe
    public static bool Pause(string modId, string plantId, bool paused);       // stops or restarts the plant's growth
    public static bool SetRotting(string modId, string plantId, bool rotting); // starts or stops the plant's rot
    public static bool Plant(string modId, string spotId, int itemId);         // plants an item in an empty growing spot
    public static bool HarvestSpot(string modId, string spotId);               // picks up a plant that is picked by hand
}
```

In these calls, `modId` is the id of your mod, `plantId` is the id of a plant from `Plants.All`, and `spotId` is the id of a growing spot.

### Harvesting

- **`Harvest` takes the plant's crop the way the player does.** The items go to the player's inventory, and onto the floor when the inventory is full. `Plants.Harvested` fires when the call hands out a crop.
- **A plant with ripe fruit works differently.** The game picks the fruit and gives no crop, so `Plants.Picked` fires once for each fruit, and `Plants.Harvested` does not fire.
  - Technical: the check is whether any visible grown item is active and `readyToPick`.
- **A plant that is not ripe gives a smaller crop, scaled by its age.** The plant is then restarted or removed, as the game does. A plant that is removed this way also empties its growing spot.
- **`Harvest` gives `false` when the call handed out no items.** This happens, for example, when the smaller crop of a young plant rounds down to nothing. The plant is still restarted or removed in that case.
- **`Reset` sets the plant's age back to 0, as the game does after a pick.**
- **`HarvestSpot` picks up a plant that is picked by hand, and `Harvest` is for the plants that grow.** A plant picked by hand is one with no growth of its own, so it has no age. `HarvestSpot` does what the player's pick-up does. The items go to the inventory, and the game's warning shows when the inventory is full.
  - Technical: such a plant is a `CollectibleItem` in a growing spot with no `grow` component. It runs the game's `CollectibleItem.PerformPickup`.
- **`HarvestSpot` gives `true` when everything was picked up, and the spot is then empty.** If only part of it fits in the inventory, the part that fits is added, the rest stays on the plant, and you get `false`. It also gives `false` for a spot with no plant and for a plant that grows (use `Harvest` for that).
- **`Plants.Harvested` does not fire for `HarvestSpot`.**

### Changing a plant

- **`SetAgePercent` sets the age as a percent of the age at which the plant is ripe.** The percent must be from 0 to 100. Any other value gives `false`. Only the age changes. The plant's look catches up the next time it grows.
- **`SetFullGrownAge` sets how old the plant must be to be ripe, in the same unit as the plant's age.** It is set for that one plant, and it is saved with it. `Plants.All` shows the value as `PlantInfo.FullGrownAge`. If the plant's age is above the new value, the age is lowered to it. The call gives `false` for an age below 1, and for a plant id that matches no plant.
  - Technical: the growth stage models keep the timing they had when the plant started, so the look can lag the new age until the plant is created again.
- **`Pause` stops or restarts the plant's growth, but a rotting plant ignores it.** The growing station's own pause and unpause change the same setting, so a station can undo your pause. A rotting plant keeps rotting whatever you set.
  - Technical: it sets the plant's `Paused` flag, and the game skips `Paused` while the plant is rotting.
- **`SetRotting` makes one plant start (`true`) or stop (`false`) rotting.** The setting is saved with the plant. A plant whose planting failed has a second rot setting that `SetRotting(false)` does not clear, so that plant keeps rotting.
  - Technical: it sets the plant's `Rotting` flag, the flag the growing station's rot sets on all its plants.

### Planting

- **`Plant` puts an item in a growing spot that is empty.** The item must be one that grows. It gives `false` when the spot already holds a plant, when the item is not known, or when the item has nothing to grow.
  - Technical: the item needs a prefab with a `grow` component.
- **You can only plant in a spot you know the id of.** The framework lists a spot only while it holds a plant, so take the `SpotId` from [Growing stations](growingstations.md) before you empty the spot.

### Rules for every call

- **Your mod's id is checked and used in the log.** These calls do not hold a claim on the plant, so two mods can call them without conflict. The last call wins. An invalid mod id throws an error.
  - Technical: `ModIds.Require` throws an `ArgumentException` for an id that is not 1 to 64 characters of letters, digits, `.`, `_` or `-`.
- **A call gives `false` while a save is loading or being written.**
  - Technical: it checks `SaveData.Busy` first.
- **A plant id or spot id that matches nothing gives `false`.**

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.plantactions.log`.

Everything not listed in this reference is internal and may change without notice.

### If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when something happens. This part of the API changes none of them and has no events, so there is nothing that can fail to set up. Its name in the log is `obenseuer.moddingframework.plantactions`. The harvest events are on the [Recipes page](recipes.md).

### When you can call it

- **Call everything from your mod's normal code (OnLoad, event handlers, Update), not from a background thread you started.** The game only works safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- **An error from the game is caught and logged once, and you get `false`.** The first error of each call goes to the log. Later errors of that call are not logged again.
