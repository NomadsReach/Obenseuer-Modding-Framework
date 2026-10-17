---
title: More than one mod
parent: Crafting stations
grand_parent: Items and crafting
nav_order: 5
---

# More than one mod

- **Each mod keeps one speed for each kind of station and one quality factor, and the newest call wins.** If a second mod sets a value another mod already set, the second mod's value applies and one warning in the log names both mods. A mod that calls again replaces its own value without a warning.
- **`Clear(modId)` removes all of that mod's changes, and then the earlier mod's value applies again.**
