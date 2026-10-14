---
title: Config
parent: Getting started
nav_order: 3
---

# Config

Settings are read with `Config.Bind`. The player can change them in the Mods menu.

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using BepInEx.Configuration;
using ObenseuerCore;

[BepInPlugin("example.hello", "Example Hello", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.10.0")]
public class ExampleHello : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("example.hello");
    private ConfigEntry<bool> greetOn;
    private ConfigEntry<int> greetEvery;
    private ConfigEntry<string> greeting;

    private void Awake()
    {
        greetOn = Config.Bind("General", "Enabled", true, "Turn the greeting on or off.");
        greetEvery = Config.Bind("General", "GreetEveryHours", 6, new ConfigDescription("Hours between greetings.", new AcceptableValueRange<int>(1, 24)));
        greeting = Config.Bind("General", "Greeting", "Hello", "Text to show.");
        Log.Info("on " + greetOn.Value + ", every " + greetEvery.Value + " hours, text " + greeting.Value);
    }
}
```

On BepInEx 5, write `"0.10.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](packaging.md#supporting-bepinex-5-too).

- The value is saved in `BepInEx\config\<mod id>.cfg`.
- Read `.Value` each time you need it, not once in `Awake`. The Mods menu can change it while the game runs.
- You do not register anything for the Mods menu. Every config entry shows up there, grouped by plugin and section.
- The control depends on the type. A `bool` is a toggle. An `int` or `float` with an `AcceptableValueRange` is a slider. A `string` is a text field. An enum is a stepper.
- Do not name an entry `enabled`. It hides `Behaviour.enabled`, and the compiler warns. This page uses `greetOn`.
- The debug switches, including the framework's master trace switch, are listed on the [Debug page](../../api/debug.md).

Next: [Keybinds](keybinds.md).
