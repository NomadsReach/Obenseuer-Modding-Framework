---
title: Pause and resume
parent: Crafting stations
grand_parent: Items and crafting
nav_order: 8
---

# Pause and resume

`Pause(modId, stationId)` freezes a process station that is running a batch, and `Resume(modId, stationId)` starts it again. Both refuse in the same situations as `Start` and `Stop`, and return `true` only when the station changed state.

- **A pause keeps the batch's remaining time and stops the countdown.** The station's humming sound stops too. The power switch freezes the same timer, but `Pause` does not touch the power. Fuel keeps burning while a station is paused.
  - Technical: `Pause` sets `Timer.stopped`. Fuel use runs on the game clock, not on `Timer.stopped`.
- **`Resume` starts the countdown and the sound again, but not while the station has no power.** A batch the game holds for lack of power stays held.
- **Losing power keeps a paused station frozen.** Only power coming back, or switching the station on again, ends the pause.
- **A pause lasts for this session only, and any mod can resume a station another mod paused.** The framework keeps no record of who paused it, and there is no `Clear`. A save made while a station is paused loads with it running, and loading a scene ends the pause.
- **A pause does not tell other mods that a craft stopped.** The game raises its craft start and stop events when power goes out and when Abort is pressed, and the framework cannot raise them for a pause.
- **`Stop` still works on a paused liquid batch.** It fails the batch as it would a running one.
