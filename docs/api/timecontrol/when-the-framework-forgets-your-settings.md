---
title: When the framework forgets your settings
parent: Time control
grand_parent: Places and travel
nav_order: 3
---

# When the framework forgets your settings

Speeds and timers are kept in memory only. Nothing is written to the save.

- **Your speed and timers are dropped when a save is loaded from the main menu or pause menu, when the main menu loads, and when a new game starts.** Moving to another level keeps them, so a speed or timer set in one level still applies in the next. A timer set before a save is saved is not in the save, so it is gone when that save is loaded from the main menu.
- **A call made while a save is loading can be dropped.** Set your speed again from `Events.WhenReady`, which runs after the load.
  - Technical: `SetTimeScale`, `ClearTimeScale`, `In` and `Cancel` are not blocked while a save loads (they are not gated by `SaveData.Busy`), so the load can clear a call made during it. They do work inside `Events.SceneLoaded`.
