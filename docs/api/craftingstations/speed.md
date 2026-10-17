---
title: Speed
parent: Crafting stations
grand_parent: Items and crafting
nav_order: 3
---

# Speed

`SetSpeedFactor(modId, stationKind, speed)` sets how fast one kind of station works for your mod. A `speed` of `2` makes a craft take half as long, `0.5` makes it take twice as long, and `1` is the game's own time.

## Things to know

- The station kind is a word that says which sort of station you mean. It is one of `manufacturing` (stations that make things), `manufacturingProcess` (stations that run a timed batch, some of which make liquids), `Growing` (stations that grow plants) or `Dishwasher`. `CraftingSpeed.AllStations` (`"*"`) means every kind. <span class="tech">(The kind is the station's class name, the same string as `CraftingStationInfo.Kind`.)</span>
- A dishwasher is its own kind, so a factor for `manufacturing` does not reach it. Use `"*"` or `"Dishwasher"` for dishwashers.
- A speed only changes how long `manufacturing` and `manufacturingProcess` stations take. Growing stations never read the factor, so a factor for `Growing` does nothing.
- A station uses the factor for its own kind and the factor for `"*"` together. They multiply, and the result is held between 0.01 and 4, the same range as a single factor. Two mods that each ask for 0.01 get 0.01, not 0.0001.
- The player's own crafting speed setting multiplies on top of your factor. <span class="tech">(It is the game's `Tweaks.CraftingSpeed`.)</span>
- A very slow factor cannot make a craft finish at once. When a craft starts, the framework keeps its scaled time under 2,000,000,000 seconds. Without that, a very slow factor on a long recipe would overflow the game's timer and the craft would end immediately. <span class="tech">(The game stores the time in a whole-second counter. A `manufacturingProcess` station works its time out again when its progress is read and when a save loads. If the station's equipment quality or the player's skill dropped since the start, that time can grow past the cap, and a recipe already within about 7 percent of it can overflow. This needs an absurdly slow factor and is not guarded.)</span>
- A craft that has already started keeps the time it was given. Calling `Clear`, or loading a scene, does not change it. Only the next craft is affected. <span class="tech">(The game saves the scaled time with the craft.)</span>
- `SetSpeedFactor` returns `false` when it did not set anything. That happens for an invalid mod id, an empty station kind, a speed that is not a number or is outside 0.01 to 4, when `SpeedFactorsAvailable` is `false`, and while a save is loading or being written.

## Limits

- A very short craft at a high speed can take 0 seconds. The game rounds the time down to whole seconds, and then runs a timer that is already finished.
