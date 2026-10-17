---
title: Examples
nav_order: 8
---

# Examples

These mods are in the repository. Each one is a working BepInEx plugin that you can build and copy. They are the advanced form, so read them after the [Getting started](guides/getting-started.md) pages. The first-time form is a framework mod, as in the ExampleCounter below.

- [HelloCore](https://github.com/NomadsReach/Obenseuer-Modding-Framework/tree/main/Examples/HelloCore): save data, events and logging.
- [SlowHunger](https://github.com/NomadsReach/Obenseuer-Modding-Framework/tree/main/Examples/SlowHunger): survival decay and threshold events.
- [WebUiHello](https://github.com/NomadsReach/Obenseuer-Modding-Framework/tree/main/Examples/WebUiHello): an HTML view with light and dark themes. Needs the WebUi module.

## ExampleCounter

This mod counts game hours and key presses, and remembers both in the save. It uses a config setting, a keybind, an event and save data, the same pieces as the [Getting started](guides/getting-started.md) pages. It is a [framework mod](api/framework-mods.md). To try it:

1. Make a project as in [Your first mod](guides/getting-started/first-mod.md).
2. Rename `MyFirstMod.csproj` to `ExampleCounter.csproj`, so the DLL is named `ExampleCounter.dll`.
3. Replace the code in `Plugin.cs` with the code below.
4. With the game closed, run `dotnet build -c Release -p:DeployToGame=true`. It copies the DLL to `BepInEx\plugins\ExampleCounter\`. Without `-p:DeployToGame=true`, the DLL is in `bin\Release\net472\ExampleCounter.dll`.

```csharp
using System;
using ObenseuerCore;
using UnityEngine;

[Serializable]
public class CounterState
{
    // Game hours counted in this save.
    public int hoursSeen;
    // Key presses counted in this save.
    public int presses;
}

[ModInfo("example.counter", "Example Counter", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class ExampleCounter : FrameworkMod
{
    // The saved numbers.
    private CounterState state = new CounterState();
    // Setting: count hours at all.
    private ModSetting<bool> countHours;
    // Setting: write a log line every N-th counted hour.
    private ModSetting<int> logEvery;
    // The F7 key.
    private ModKeybind pressKey;

    protected override void OnLoad()
    {
        // Two settings, shown in the Mods menu. LogEveryHours is a slider from 1 to 24.
        countHours = Config.Bind("General", "CountHours", true, "Count game hours while on.");
        logEvery = Config.Bind("General", "LogEveryHours", 1, "Log every N-th counted hour.", 1, 24);
        // Register the F7 key.
        pressKey = Mod.Key("Press", "Count a press", KeyCode.F7);
        // Save and load the numbers with the save game.
        SaveData.Register<CounterState>("example.counter", () => state, s => state = s ?? new CounterState());
        // Count every game hour, and log what was saved each time a save loads.
        Events.HourPassed += OnHour;
        Events.Loaded += () => Log.Info("loaded, hours seen " + state.hoursSeen + ", presses " + state.presses);
    }

    // Unity calls this once per frame.
    private void Update()
    {
        // Skip while a save runs, a menu or the pause screen is open, or the game has switched its keys off.
        if (pressKey == null || SaveData.Busy || UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (!pressKey.Down()) return;
        state.presses++;
        Log.Info("press " + state.presses);
    }

    // Runs at the start of each game hour.
    private void OnHour(int hour)
    {
        if (!countHours.Value || SaveData.Busy) return;
        state.hoursSeen++;
        if (state.hoursSeen % logEvery.Value == 0) Log.Info("hour " + hour + ", hours seen " + state.hoursSeen);
    }
}
```

After it loads, change `LogEveryHours` in the Mods menu, press F7 in the game, and check `BepInEx\logs\example.counter.log`. The player can rebind the key on the Mods menu page "Mod Keybinds".

The counter counts each game hour once, including across level changes, because a load does not repeat the hour it lands on.

## Where to go next

To add more to a mod, see the [Cookbook](guides/cookbook/index.md). To read about every area of the framework, see the [API reference](API.md).
