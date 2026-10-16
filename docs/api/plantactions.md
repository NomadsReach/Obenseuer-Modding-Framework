---
title: Plant actions API
parent: API reference
nav_order: 97.1
---

# Plant actions API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.plantactions`.
Files: `src/Api/PlantActions.cs`.

Changes ordinary plants: harvest, reset, set the age, set the full-grown age, pause, rot and plant in a growing spot. Picks collectible plants in a growing spot (`HarvestSpot`). Reads and the harvest event stay in `Plants` (`docs/api/recipes.md`). Plant ids are the ids `Plants.All` reports.

## API

```csharp
public static class PlantActions
{
    public static bool Harvest(string modId, string plantId);
    public static bool Reset(string modId, string plantId);
    public static bool SetAgePercent(string modId, string plantId, int percent);
    public static bool SetFullGrownAge(string modId, string plantId, int age);
    public static bool Pause(string modId, string plantId, bool paused);
    public static bool SetRotting(string modId, string plantId, bool rotting);
    public static bool Plant(string modId, string spotId, int itemId);
    public static bool HarvestSpot(string modId, string spotId);
}
```

- `Harvest` takes the plant's crop the way the player does. The items go to the inventory, or to the floor when it is full. `Plants.Harvested` fires when the call hands out a crop. A plant with a ready fruit takes another path: the game picks the fruit and returns no crop, so `Plants.Picked` fires once per fruit and `Plants.Harvested` does not fire. An unripe plant gives a partial crop, scaled by its age, and the plant is reset or removed, as the game does. A plant removed this way also empties its growing spot. `Harvest` returns `false` when the call gave no items, for example when an unripe plant's age-scaled crop rounds to zero; the plant is still reset or removed.
- `Reset` sets the age back to 0, as the game does after a pick.
- `SetAgePercent` takes a percent from 0 to 100 of the full-grown age. Any other value returns `false`. It sets only the plant's age. The plant's visuals catch up on its next growth tick.
- `Pause` sets the plant's paused flag. The growing station's own pause and unpause change the same flag, so a station can undo it. A rotting plant ignores the flag: the game skips `Paused` while the plant is rotting, so `Pause` has no effect on it.
- `SetFullGrownAge` sets the age at which the plant is ripe, in the same unit as the plant's age. It is per plant and is saved with the plant. `Plants.All` reads the current value as `PlantInfo.FullGrownAge`. If the plant's current age is above the new value, the age is clamped down to it. It returns `false` for an age below 1 and for a plant id that matches no plant. The growth stage models keep the timing they had when the plant started, so the look can lag the new age until the plant is created again.
- `SetRotting` starts (`true`) or stops (`false`) the rot of one plant by setting its `Rotting` flag, the flag the growing station's rot sets on all its plants. A plant whose planting failed also has a second rot flag that `SetRotting(false)` does not clear, so that plant keeps rotting. The flag is saved with the plant.
- `Plant` puts the item in a growing spot that is empty. It returns `false` when the spot already holds a plant, or when the item has no growing prefab.
- `HarvestSpot` picks a collectible plant: a plant in a growing spot with no `grow` component, which is a `CollectibleItem`. It runs the game's pickup (`CollectibleItem.PerformPickup`), so the items go to the inventory and the game's warning shows when the inventory is full. When the whole pickup succeeds, the spot is emptied and `true` is returned. A partial pickup adds what fits, leaves the rest on the plant, and returns `false`. It returns `false` for a spot with no plant and for a grow plant (use `Harvest`). `Plants.Harvested` does not fire for this call.

## Behaviour

- `modId` is checked and used for logs. These calls hold no claim, so two mods can call them without conflict. The last call wins.
- Refused while a save loads or saves (`SaveData.Busy`).
- A plant id that matches no plant returns `false`.

## Example

```csharp
PlantActions.SetAgePercent("my.mod", plantId, 100);
PlantActions.Harvest("my.mod", plantId);
```
