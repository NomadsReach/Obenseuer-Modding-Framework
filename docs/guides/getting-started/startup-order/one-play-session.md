---
title: One play session
parent: When things run
grand_parent: Getting started
nav_order: 2
---

# One play session

This page follows one small mod through a whole play session, from launching the game to quitting.

A tiny mod that only subscribes in `OnLoad`:

```csharp
protected override void OnLoad()
{
    Events.SceneLoaded += scene => Log.Info("scene " + scene);
    Events.Loaded += () => Log.Info("loaded");
    Events.Saved += () => Log.Info("saved");
    Events.HourPassed += hour => Log.Info("hour " + hour);
    Events.DayPassed += day => Log.Info("day " + day);
    Events.WhenReady(() => Log.Info("save is ready"));
}
```

One session with that mod installed, in order:

1. **You launch the game.** Every plugin's `Awake` runs, the framework's first. The framework's `Start` then finds your mod, adds its component (Unity runs its `Awake`), and calls `OnLoad`. `OnLoad` subscribes to five events and queues one `WhenReady` action. No save is loaded, so `SaveData.Folder` is `null`.

2. **The main menu appears.** The `Main Menu` scene loads, so `Events.SceneLoaded` runs with `"Main Menu"`. The framework also clears the session here. No `Loaded` runs, because nothing was loaded.

3. **You load a save.** The scene loads first, and `Events.SceneLoaded` runs while the framework is still busy, so only reads are safe there. Then the load finishes: `Events.Loaded` runs, then the ready point, then your queued `WhenReady` action. That action leaves the queue as it runs, so this is its only run in the session.

4. **You play.** `Events.HourPassed` runs once for each game hour. At a day change, `Events.DayPassed` runs first, then `HourPassed` for the new day's hour 0. Your `Update` runs every frame.

5. **You walk through a door.** The game saves first, so `Events.Saved` then `Events.AfterSave` run. Then the next level loads: `Events.SceneLoaded`, then `Events.Loaded` again, then the ready point. Your `WhenReady` action does not run again, because you queued it once in `OnLoad` and it was removed when it ran. Work you want at every door goes in your `Loaded` handler.

6. **You save the game.** `Events.Saved` runs, then `Events.AfterSave`. Nothing loads, so no `Loaded` and no ready point follow.

7. **You quit.** The framework writes the settings you changed and flushes its log. Nothing else is saved, so write anything else you need before the player quits.

A new game is the exception to step 3. It does not raise `Events.Loaded`, because a new game does not go through the game's save load path. The ready point still comes, at the first level of the new game, so a `WhenReady` action queued in `OnLoad` still runs. See Step 5 in [Loading and playing](loading-and-playing.md) for the same point in the full list.

Next: [Starting the game](starting-the-game.md).
