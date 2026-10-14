---
title: Using events
parent: Getting started
nav_order: 5
---

# Using events

Subscribe to events in `Awake`. The full list is on the [events page](../../events.md).

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

[BepInPlugin("example.hello", "Example Hello", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.10.0")]
public class ExampleHello : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.hello");

    private void Awake()
    {
        Events.HourPassed += OnHour;
        Events.DayPassed += day => Log.Info("day " + day + ", " + GameTime.DayOfWeek);
        Events.SceneLoaded += scene => Log.Info("scene " + scene);
        Events.Loaded += () => Log.Info("loaded on day " + GameTime.Day);
    }

    private void OnHour(int hour)
    {
        Log.Info("hour " + hour + ", minute " + GameTime.Minute + ", paused " + GameTime.IsPaused);
    }
}
```

On BepInEx 5, write `"0.10.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](packaging.md#supporting-bepinex-5-too).

- `Events.HourPassed(int hour)` runs once for each game hour.
- `Events.DayPassed(int day)` runs when the game day changes.
- `Events.SceneLoaded(string scene)` gives the name of the scene that loaded.
- `Events.Saved` and `Events.Loaded` run after a save and after a load. `Loaded` also runs on every door change, so for work that should happen once per loaded save, use `Events.WhenReady`.
- Handlers run in the order they were added. The first throw from each handler is logged to `ObenseuerCore.log`, and the other handlers still run.
- `GameTime` reads `Day`, `DayOfWeek`, `Hour`, `Minute` and `IsPaused`. On the main menu, and while a load runs, `Day`, `Hour` and `Minute` are 0.
- `GameTime.IsPaused` means the game clock is stopped. It is not the pause menu. For the pause menu, use `UiState.IsPaused`.
- The game raises events on Unity's main thread. Call the framework from there too.

Next: [Save data](save-data.md).
