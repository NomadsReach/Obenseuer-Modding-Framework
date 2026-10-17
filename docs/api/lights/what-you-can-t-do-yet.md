---
title: What you can't do (yet)
parent: Lights
grand_parent: Objects and devices
nav_order: 4
---

# What you can't do (yet)

- **You can't switch a multi-state fixture on or off as a whole.** Only the lit setting can be changed.
  - Technical: the game's `LightTurnOffOnMultiState.on` flag and `Toggle` method have no framework call.
- **You can't address the `LightsON` script.** It has no save id and no saved state, so it can't be pointed at by an id, and a change would be lost the next time the scene starts.
- **You can't wait for a fade to finish.** The fades run over several frames, and `Toggled` fires when the switch flips, not when the light has finished fading.
  - Technical: `TurnLightOnFade` and `TurnLightOffFade`.
- **You can't change a light's brightness or materials.** Only on or off, and the lit setting, can be changed.
