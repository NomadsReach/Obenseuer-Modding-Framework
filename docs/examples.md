---
title: Examples
nav_order: 8
---

# Examples

These mods are in the repository. Each one is a working plugin you can build and copy.

- [HelloCore](https://github.com/NomadsReach/Obenseuer-Modding-Framework/tree/main/Examples/HelloCore): save data, events and logging.
- [SlowHunger](https://github.com/NomadsReach/Obenseuer-Modding-Framework/tree/main/Examples/SlowHunger): survival decay and threshold events.
- [WebUiHello](https://github.com/NomadsReach/Obenseuer-Modding-Framework/tree/main/Examples/WebUiHello): an HTML view with light and dark themes. Needs the WebUi module.

## ExampleCounter

This mod uses a config setting, a keybind, an event and save data. This example is a [framework mod](api/framework-mods.md). Save it as `ExampleCounter.cs` in a project made as in [Your first mod](guides/getting-started/first-mod.md). Change the project's `AssemblyName` to `ExampleCounter`, then run `dotnet build -c Release -p:DeployToGame=true` to copy the DLL to `BepInEx\plugins\ExampleCounter\`. Without `-p:DeployToGame=true`, the DLL is in `bin\Release\net472\ExampleCounter.dll`.

```csharp
using System;
using ObenseuerCore;
using UnityEngine;

[Serializable]
public class CounterState
{
    public int hoursSeen;
    public int presses;
}

[ModInfo("example.counter", "Example Counter", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class ExampleCounter : FrameworkMod
{
    private CounterState state = new CounterState();
    private ModSetting<bool> countHours;
    private ModSetting<int> logEvery;
    private ModKeybind pressKey;

    protected override void OnLoad()
    {
        countHours = Config.Bind("General", "CountHours", true, "Count game hours while on.");
        logEvery = Config.Bind("General", "LogEveryHours", 1, "Log every N-th counted hour.", 1, 24);
        pressKey = Mod.Key("Press", "Count a press", KeyCode.F7);
        SaveData.Register<CounterState>("example.counter", () => state, s => state = s ?? new CounterState());
        Events.HourPassed += OnHour;
        Events.Loaded += () => Log.Info("loaded, hours seen " + state.hoursSeen + ", presses " + state.presses);
    }

    private void Update()
    {
        if (pressKey == null || SaveData.Busy || UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (!pressKey.Down()) return;
        state.presses++;
        Log.Info("press " + state.presses);
    }

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
