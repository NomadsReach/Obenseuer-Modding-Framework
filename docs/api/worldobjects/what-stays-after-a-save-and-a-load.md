---
title: What stays after a save and a load
parent: World objects
grand_parent: Objects and devices
nav_order: 6
---

# What stays after a save and a load

| Thing | Saved | Back after loading | Notes |
|---|---|---|---|
| A spawned item (`Spawn.Item`) | yes | yes, with its stack, position and expiry | It has no save id, so `Find`, `GuidOf` and mod data do not work for it. `WorldItems.IdOf` gives it an id that is a path (`<scene>/path:...`). Items the player dropped in this session work the same way. |
| A spawned item's expiry time | yes | yes, with the time left | Items run down on the game's item timer. A timer that fires while the player is more than 10 metres away removes the item. |
| A scene object with a save id (`IsSaved` is true) | yes, if it is switched on when the game saves | yes, into the object that is already there | The game only saves objects that are switched on. |
| Mod data (`SetData`) | yes, with the save | yes, when the save loads | Kept by mod, scene and save id. Emptied when a load or a new game starts. |
| A scene object that is switched off when the game saves | no | no | The game's save skips switched-off objects. |

- **It is not known whether a spawned item stays when the player leaves its room.** The game's list of spawned items keeps its entries by object, not by scene, and this has not been tested across a scene change.
