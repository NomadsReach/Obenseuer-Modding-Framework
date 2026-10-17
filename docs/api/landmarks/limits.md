---
title: Limits
parent: Landmarks
grand_parent: Places and travel
nav_order: 6
---

# Limits

- **The framework tracks a landmark by its id text.** It is not known whether two landmarks in the game can share one id.
- **If the map screen will not show a marker, the entry is still kept, and the call can still look successful.** `Unlock` reads the loaded map back, so it gives back `true`.
  - Technical: `AddLandMark` still keeps the entry and gives back `false` in that case.
- **`Unlock` does not check that the landmark belongs to the map that is loaded.** Whether the marker then shows on the wrong map is not known.
- **A saved landmark is only put back if the game's map load succeeds for that map.** A map the save never wrote has none, so nothing is restored there until the next save.
  - Technical: the restore needs `OnLoadingGame` to give back `true` for the map's controller.
- **Reading the map's data directly can still show your landmarks.**
  - Technical: `SaveData.Capture` on the map controller, called by a mod outside a game save, still sees the mod landmarks.
