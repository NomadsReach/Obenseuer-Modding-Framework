---
title: Save data
parent: Getting started
nav_order: 6
---

# Save data

Save data is information your mod remembers for each save game, such as how many days the player has played with your mod. The framework writes it next to the save, and reads it back when the player loads that save. You register it once, in `OnLoad`, which the framework runs when your mod loads. A BepInEx plugin registers in `Awake` instead.

This page continues the project from [Your first mod](first-mod.md). Replace the code in `Plugin.cs` with the code below, then run `dotnet build -c Release -p:DeployToGame=true` with the game closed. `HelloState` is a plain class that holds what you want to keep. `SaveData.Register` takes your mod id and two methods: one that hands over the data to save, and one that receives the data after a load.

```csharp
using System;
using ObenseuerCore;

// The data to keep. It needs a constructor with no parameters (this class has the default one).
[Serializable]
public class HelloState
{
    // How many game days this save has seen with the mod.
    public int daysSeen;
}

[ModInfo("example.hello", "Example Hello", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class ExampleHello : FrameworkMod
{
    private HelloState state = new HelloState();

    protected override void OnLoad()
    {
        // First method: gives the framework the data to save. Second: receives the loaded data.
        SaveData.Register<HelloState>(Mod.Id, () => state, s => state = s ?? new HelloState());
        Events.DayPassed += day =>
        {
            // Do nothing while a save or load is running.
            if (SaveData.Busy) return;
            state.daysSeen++;
            Log.Info("days seen " + state.daysSeen);
        };
    }
}
```

Start the game, load a save and let a game day pass. Save, quit, load the same save and let another day pass. The log line `days seen` continues from the old number instead of starting at 1.

- **Your data is kept per save, in one file.** It is `<mod id>.json` in the `mods` folder of that save, so each save game has its own numbers.
- **Save a plain class.** It needs a constructor with no parameters. Public fields and `[SerializeField]` fields are saved. Properties are not.
- **Common types work.** Numbers, text, lists, arrays, dictionaries, `Vector2`, `Vector3`, `Vector4`, `Quaternion` and `Color` are saved. A field that holds different subclasses (a polymorphic type) does not work, and neither does an array as the top-level type.
- **The framework asks for your data when the player saves, and gives it back after a load.** On a save it calls the first method you passed, which returns your data. When a load finishes, it calls the second method with the data it read.
- **A new save has no data yet.** The loaded value is `null` when the file is missing or cannot be read, so write `s ?? new HelloState()` to start empty.
- **Do not change game state while `SaveData.Busy` is `true`.** It is `true` during a load and in the frame a save starts. Wait for it to be `false` before you touch anything.
- **Register in `OnLoad`.** A framework mod's `OnLoad` runs when the framework starts the mod. If a save is already open by then, the framework loads your data at once. A BepInEx plugin does the same call in `Awake`.
- **Register once.** A second `Register` with the same id throws `InvalidOperationException`.
- **For several separate pieces of data, or data with no null to check, use `SaveData.Bind`.** It returns a slot whose `Value` is never `null`. The [Pay the player](../cookbook/pay-the-player.md) recipe uses it.

## Where to go next

Next: [Patching](patching.md).
