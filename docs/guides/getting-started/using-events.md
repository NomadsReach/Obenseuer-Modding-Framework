---
title: Using events
parent: Getting started
nav_order: 5
---

# Using events

An event is a signal the framework sends when something happens in the game, such as a new hour starting or a save loading. Your mod subscribes to the event with `+=` and gives it a method to run. You do this once when your mod loads. The full list is on the [events page](../../events.md).

This page continues the project from [Your first mod](first-mod.md). Replace the code in `Plugin.cs` with the code below, then run `dotnet build -c Release -p:DeployToGame=true` with the game closed. `GameTime` reads the game's clock and calendar.

```csharp
using ObenseuerCore;

[ModInfo("example.hello", "Example Hello", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class ExampleHello : FrameworkMod
{
    protected override void OnLoad()
    {
        // Runs OnHour at the start of every game hour. The method is below.
        Events.HourPassed += OnHour;
        // A short handler written in place. It gets the new day number.
        Events.DayPassed += day => Log.Info("day " + day + ", " + GameTime.DayOfWeek);
        // Gets the name of each scene (game level) that loads.
        Events.SceneLoaded += scene => Log.Info("scene " + scene);
        // Runs after a save has loaded.
        Events.AfterSave += folder => Log.Info("saved to " + folder);
        Events.Loaded += () => Log.Info("loaded on day " + GameTime.Day);
    }

    private void OnHour(int hour)
    {
        Log.Info("hour " + hour + ", minute " + GameTime.Minute + ", paused " + GameTime.IsPaused);
    }
}
```

Start the game, load a save and play for a few game hours. Then open `BepInEx\logs\example.hello.log` and look for the `hour`, `scene` and `loaded` lines.

- **`Events.HourPassed(int hour)` runs once for each game hour.**
- **`Events.DayPassed(int day)` runs when the game day changes.** It gets the new day number.
- **`Events.SceneLoaded(string scene)` gives the name of the scene that loaded.** A scene is one level of the game that Unity loads.
- **`Events.Saved` and `Events.Loaded` run after a save and after a load.** `Loaded` also runs on every door change, so for work that should happen once per loaded save, use `Events.WhenReady`. It runs your action once a save is loaded, or at once if one already is.
- **`Events.AfterSave(string folder)` runs in the same saving step, after `Events.Saved` and after every `Saved` handler has run.** The argument is the folder the game just wrote the save to, for example `...\Saves\Esko_Virtanen\MySave`. A door change saves to the `Autosave` or `Autosave2` slot first, so the folder names that slot. It gives `null` when the game has no character and save name yet. It is not `SaveData.Folder`, which stays on the loaded save until the next load (see [Save data](save-data.md) and [Game save data entries](../../api/savedata-game.md#save-folder-and-peek)).
  - Technical: during a real load the framework skips writing its own mod files while this event still fires, so treat the folder as the game's and not as proof that the framework wrote anything.
- **Handlers run in the order they were added.** If one throws an error, the first error from that handler goes to `BepInEx\logs\ObenseuerCore.log`, and the other handlers still run.
- **`GameTime` reads `Day`, `DayOfWeek`, `Hour`, `Minute` and `IsPaused`.** On the main menu, and while a load runs, `Day`, `Hour` and `Minute` are 0.
- **`GameTime.IsPaused` is true whenever the game clock is stopped.** The pause menu and the saving screen both stop it. To ask only about the pause menu, use `UiState.IsPaused`.
  - Technical: `GameTime.IsPaused` is `Time.timeScale == 0`.
- **Call the framework from your mod's normal code, not from a background thread you started.** The game raises events on Unity's main thread, so your handlers already run in the right place. See [Main thread](../../glossary.md#main-thread).

## Where to go next

Next: [Save data](save-data.md).
