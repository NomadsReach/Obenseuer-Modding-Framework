---
title: What stays after a save and a load
parent: World objects
grand_parent: Objects and devices
nav_order: 6
---

# What stays after a save and a load

What comes back after a load depends on the kind of object. The table shows each case.

| Thing | Saved | Back after loading | Notes |
|---|---|---|---|
| A spawned item (`Spawn.Item`) | Yes. | Yes, with its stack, position and expiry. | It has no save id, so `Find`, `GuidOf` and mod data do not work for it. `WorldItems.IdOf` gives it a path-style id, `<scene>/path:...`. Items the player dropped in this session work the same way. |
| A spawned item's expiry time | Yes. | Yes, with the time left. | Items run down on the game's item timer. A timer that fires while the player is more than 10 metres away removes the item. |
| A scene object with a save id (`IsSaved` is true) | Yes, if it is switched on when the game saves. | Yes, into the object that is already there. | The game only saves objects that are switched on. |
| Mod data (`SetData`) | Yes, with the save. | Yes, when the save loads. | Kept by mod, scene and save id. Emptied when a load or a new game starts. |
| A scene object that is switched off when the game saves | No. | No. | The game's save skips switched-off objects. |

## Things to know

- It is not known whether a spawned item stays when the player leaves its room. The game's list of spawned items keeps its entries by object, not by scene, and this has not been tested across a scene change.
