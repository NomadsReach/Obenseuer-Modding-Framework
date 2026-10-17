---
title: What you can't do (yet)
parent: Surveillance
grand_parent: Law and crime
nav_order: 4
---

# What you can't do (yet)

- **You can't react to the camera alarm.** Use `PlayerSpotted` instead.
  - Technical: the game's `OnPlayerDetected` output, after the detection timer, is not available to mods.
- **You can't move a camera or change its range or width.** The game has no method for it, so the range and width can only be read.
  - Technical: `range` and `FOV` are public fields, but no game method sets them.
- **You can't switch a trespassing area on or off.**
  - Technical: the game's `AreaDisabled` property is public, but this page does not offer it.
- **You can't read theft records here.** Thefts are checked through the same witness check, so a theft seen by someone fires `PlayerSpotted` and `Law.Spotted`. The records are on the Law page.
  - Technical: `Inventory` and `Logic_Thief` check witnesses through `Witness.IsWitnessed`.
