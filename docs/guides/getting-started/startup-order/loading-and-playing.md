---
title: Loading and playing
parent: When things run
grand_parent: Getting started
nav_order: 4
---

# Loading and playing

This page is steps 5 to 8 of the order: a new game, loading a save, a door or level change, and every frame.

**Step 5. A new game.** `Events.NewGameStarted` fires when the player confirms a new character, before the first level. The game's difficulty and the chosen start are not applied yet, so do not read them in that handler. When the first level loads, the order is `Events.SceneLoaded`, then the framework's ready point, then the actions you queued with `Events.WhenReady`. `Events.Loaded` does not fire for a new game, because a new game does not go through the game's save load path. The first `Events.Loaded` of that session comes with the first door or level change.

**Step 6. Loading a save.** The framework is busy for the whole load. The scene loads and `Events.SceneLoaded` fires while it is still busy, so calls that change the game are refused there. Use `Events.WhenReady` instead. When the load finishes, the framework sets the loaded save folder, then raises `Events.Loaded`. The framework reads your saved mod data back before your own `Loaded` handlers run, so `Loaded` is a safe place to read it. After `Loaded` the framework's ready point runs, and then your `WhenReady` actions.

`Events.Loaded` also fires on every door and level change, not only on a real load. Use `Events.WhenReady` for work that should happen once a save is ready. For quests, use the quests ready event instead, because the task panel and the notifications may not exist yet.

**Step 7. A door or level change.** The game saves first, which raises `Events.Saved` and then `Events.AfterSave`. Then the next level loads: `Events.SceneLoaded`, then `Events.Loaded` again, then the ready point and your `WhenReady` actions. Per-save mod data is read back from the save the door just wrote.

**Step 8. Every frame.** The framework ticks its own features in `Update` and `LateUpdate`. Your mod is a component, so its own `Update` runs too. `Events.HourPassed` and `Events.DayPassed` come from the game clock and are checked once a game minute.

Next: [Saving and quitting](saving-and-quitting.md).
