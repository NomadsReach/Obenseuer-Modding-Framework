---
title: Start and stop
parent: Crafting stations
grand_parent: Items and crafting
nav_order: 7
---

# Start and stop

These calls act on a station you name by its id. They return `true` only when the station changed state. `Start(modId, stationId)` works differently for each kind of station, and `Stop(modId, stationId)` ends a batch early.

## Things to know

- They refuse in several situations. That is while a crafting panel is open, while a save is loading or being written, for an empty id, and when two stations have the same id. <span class="tech">(The checks are `CraftingBase.currentCraftingBase != null`, `SaveGate.Busy`, an empty id and a duplicate `GUID`.)</span>
- A `manufacturingProcess` station starts when it is loaded and ready. It must be waiting for ingredients, have power and hold the ingredients for the selected recipe. It returns `true` once the station is processing. <span class="tech">(The station must be in its `LoadingItems` stage and `PoweredAndActive`.)</span>
- A `manufacturing` station starts one craft of the selected recipe. It must have power, a free slot for another craft at the same time, and the ingredients. It returns `true` when a craft started.
- A `Growing` station plants one unit of the selected recipe into a free growing spot. It needs the ingredients, and it returns `true` when one fewer spot is free.
- The ingredient check counts the station's own slots and the extra containers from [Crafting sources](../craftingsources.md).
- `Stop` only works on a liquid recipe that a process station is running. It is refused for a solid recipe, because the game ends a stopped process through its normal finish, and for a solid recipe that would hand out the full result at once. A start followed by a stop would skip the process time.
- A stopped liquid batch fails, as it does when the player presses Abort. The ingredients are used up, the batch counts as failed, and the station gives the recipe's failure results and stays in a failed state until it is emptied. `Stop` returns `true` when the station left the running state. <span class="tech">(It opens the lid, which ends the processing stage through the game's normal abort.)</span>
- The stopped batch cannot pay out a second time. The framework removes its timer, so the station does not finish the batch later. The game's own Abort button leaves that timer running.

## Limits

- A `manufacturing` station that makes the player stand at it is refused, and so is a dishwasher. A craft that needs the player speeds up the game's clock through the waiting screen of its panel, which a script cannot do safely. <span class="tech">(The station has `RequirePlayer` set. A `Dishwasher` is refused because the check is for the exact `manufacturing` type.)</span>
- You can't select the recipe from the framework. There is no call that picks a recipe for a station. Select it first with the game's own `SetSelectedRecipe`, then call `Start`.
- `manufacturing` and `Growing` stations cannot be stopped. The game has no stop for them, so there is none here. See [What you can't do (yet)](what-you-can-t-do-yet.md#what-you-cant-do-yet).
