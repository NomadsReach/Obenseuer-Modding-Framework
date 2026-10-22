---
title: Common tasks
parent: Framework mods
grand_parent: Framework basics
nav_order: 1
---

# Common tasks

Add settings. The second setting is a slider in the Mods menu, because it has a smallest and a largest value. `Changed` runs your code when the player moves it.

```csharp
using ObenseuerCore;

[ModInfo("example.tasks", "Tasks", "1.2.3")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ExampleSettings : FrameworkMod
{
    protected override void OnLoad()
    {
        // Both settings appear in the Mods menu under this mod's name.
        ModSetting<bool> loud = Config.Bind("General", "Loud", false, "Log more.");
        ModSetting<int> count = Config.Bind("General", "Count", 3, "How many.", 1, 10);
        // Runs when the player moves the switch.
        loud.Changed += () => Log.Info("Loud is now " + loud.Value);
        Log.Info("Count is " + count.Value + " (default " + count.Default + ")");
    }
}
```

Depend on other mods. A mod with no version after its id only has to be there. The mods you name here load before yours.

```csharp
using ObenseuerCore;

[ModInfo("example.tasks", "Tasks", "1.2.3")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
// A dependency with no version only has to be installed.
[ModDependency("example.otherlibrary")]
[ModDependency("example.anotherlibrary", "2.0")]
public sealed class Tasks : FrameworkMod
{
    protected override void OnLoad()
    {
        Log.Info("loaded; the libraries above load first");
    }
}
```

Use a mod only when it is there, without making it a must-have.

```csharp
using ObenseuerCore;

[ModInfo("example.tasks", "Tasks", "1.2.3")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ExampleOptional : FrameworkMod
{
    protected override void OnLoad()
    {
        // Runs once a save has loaded.
        Events.WhenReady(() =>
        {
            if (Loader.Has("example.optionalfriend")) Log.Info("The optional mod is running.");
        });
    }
}
```
