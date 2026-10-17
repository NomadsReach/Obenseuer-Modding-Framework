---
title: Quality
parent: Crafting stations
grand_parent: Items and crafting
nav_order: 4
---

# Quality

`SetQualityFactor(modId, factor)` sets how good everything is that a station makes, for your mod. A `factor` of `1` is the game's own quality, `2` doubles it and `0.5` halves it.

## Things to know

- A quality factor changes every output a station makes. That includes the outputs of a failed craft, liquids and grown plants. The result is held between 0.05 and 2, the range the game uses for crafted quality.
- A liquid from a process station gets the factor that was in force when the batch started. The liquid left in the station keeps that quality, so calling `Clear` or loading a scene during the batch does not change it. <span class="tech">(This is a `manufacturingProcess` station. The factor is applied once, when the batch starts.)</span>
- There is only a multiplier, not an offset. The game's quality formulas use the value as a multiplier, so an offset would not fit.
- Items that already exist keep their quality. Calling `Clear` or loading a scene does not change them. <span class="tech">(The game saves the quality on the item.)</span>
- `SetQualityFactor` returns `false` when it did not set anything. That happens for an invalid mod id, a factor outside 0 to 4, when `QualityFactorAvailable` is `false`, and while a save is loading or being written.

## Limits

- An output that already has a fixed quality is left alone, and so is every recipe that sets a fixed quality (for example the Gutter Oil recipes).
- Harvesting a plant is not a craft, so it is not covered.
- The recipe info panel still shows the game's own quality, not the one your factor gives.
