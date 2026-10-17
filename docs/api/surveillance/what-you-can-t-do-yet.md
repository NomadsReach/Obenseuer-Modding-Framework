---
title: What you can't do (yet)
parent: Surveillance
grand_parent: Law and crime
nav_order: 4
---

# What you can't do (yet)

- You can't react to the camera alarm. Use `PlayerSpotted` instead. <span class="tech">(The game's `OnPlayerDetected` output, after the detection timer, is not available to mods.)</span>
- You can't move a camera or change its range or width. The game has no method for it, so the range and width can only be read. <span class="tech">(`range` and `FOV` are public fields, but no game method sets them.)</span>
- You can't switch a trespassing area on or off. <span class="tech">(The game's `AreaDisabled` property is public, but this page does not offer it.)</span>
- You can't read theft records here. Thefts are checked through the same witness check, so a theft seen by someone fires `PlayerSpotted` and `Law.Spotted`. The records are on the Law page. <span class="tech">(`Inventory` and `Logic_Thief` check witnesses through `Witness.IsWitnessed`.)</span>
