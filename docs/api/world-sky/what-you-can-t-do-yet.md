---
title: What you can't do (yet)
parent: World sky
grand_parent: Places and travel
nav_order: 6
---

# What you can't do (yet)

- **You can't set the Azure weather profile's own properties, such as fog distance, scattering, sky colours or sun direction.** The profile is a game asset, and its property names and order are not in the code, so the framework cannot name them safely.
- **You can't set the sun's direction.** The game computes it from the time of day on every step, so any value from a mod would be overwritten. Change the time of day instead (see [World](../world.md)).
  - Technical: Azure computes the light direction each physics step, and `SunDirection` computes its own angle.
- **You can't switch the whole sky on or off.** That swaps the game's whole weather profile, and a profile with fewer entries than the override needs causes an error in the game.
  - Technical: `TimeOfDayAzure.DisableSky` and `EnableSky` are not covered.
