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

This mod uses a config setting, a keybind, an event and save data. Save it as `ExampleCounter.cs` in a project made as in [Your first mod](guides/getting-started/first-mod.md). Change the project's `AssemblyName` to `ExampleCounter`, then run `dotnet build -c Release -p:DeployToGame=true` to copy the DLL to `BepInEx\plugins\ExampleCounter\`. Without `-p:DeployToGame=true`, the DLL is in `bin\Release\net472\ExampleCounter.dll`.

```csharp
using System;
using BepInEx;
using BepInEx.Unity.Mono;
using BepInEx.Configuration;
using ObenseuerCore;
using UnityEngine;

[Serializable]
public class CounterState
{
    public int hoursSeen;
    public int presses;
}

[BepInPlugin("example.counter", "Example Counter", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
public class ExampleCounter : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.counter");
    private CounterState state = new CounterState();
    private ConfigEntry<bool> countHours;
    private ConfigEntry<int> logEvery;
    private ModKeybind pressKey;

    private void Awake()
    {
        countHours = Config.Bind("General", "CountHours", true, "Count game hours while on.");
        logEvery = Config.Bind("General", "LogEveryHours", 1, new ConfigDescription("Log every N-th counted hour.", new AcceptableValueRange<int>(1, 24)));
        pressKey = Keybinds.Register("example.counter", "Press", "Count a press", KeyCode.F7);
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

On BepInEx 5, write `"0.11.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](guides/getting-started/packaging.md#supporting-bepinex-5-too).

After it loads, change `LogEveryHours` in the Mods menu, press F7 in the game, and check `BepInEx\logs\example.counter.log`. The player can rebind the key on the Mods menu page "Mod Keybinds".

The counter counts each game hour once, including across level changes, because a load does not repeat the hour it lands on.
