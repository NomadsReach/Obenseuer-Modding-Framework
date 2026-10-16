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
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
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

On BepInEx 5, write `"0.11.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](packaging.md#supporting-bepinex-5-too).

- The value is saved in `BepInEx\config\<mod id>.cfg`.
- Read `.Value` each time you need it, not once in `Awake`. The Mods menu can change it while the game runs.
- You do not register anything for the Mods menu. Every config entry shows up there, grouped by plugin and section.
- The control depends on the type. A `bool` is a toggle. An `int` or `float` with an `AcceptableValueRange` is a slider. A `string` is a text field. An enum is a stepper.
- Do not name an entry `enabled`. It hides `Behaviour.enabled`, and the compiler warns. This page uses `greetOn`.
- The debug switches, including the framework's master trace switch, are listed on the [Debug page](../../api/debug.md).

## Changing a default in a new version

BepInEx keeps the value a player already saved, so a new default in your code never reaches a player who has the old file. To change a default, upgrade the saved value, but only when it still holds the old default, so a value the player chose stays. Bind a `ConfigVersion` entry, add one `if` line per version, then write the current version. This example changes `GreetEveryHours` from 6 to 4 in version 2. Add the new members to the plugin class above.

```csharp
private const int CurrentConfigVersion = 2;
private ConfigEntry<int> configVersion;
```

Replace the `GreetEveryHours` bind with the first line below, so a fresh install gets the new default, then add the rest after the existing binds and before the `Log.Info` line:

```csharp
greetEvery = Config.Bind("General", "GreetEveryHours", 4, new ConfigDescription("Hours between greetings.", new AcceptableValueRange<int>(1, 24)));
configVersion = Config.Bind("General", "ConfigVersion", 0, new ConfigDescription("Internal. Do not edit.", null, new { Browsable = false }));
if (configVersion.Value < 2 && greetEvery.Value == 6) greetEvery.Value = 4;
configVersion.Value = CurrentConfigVersion;
```

- Each `if` compares the saved version, which is written only at the end, so a player who skips versions gets every upgrade after their saved one. For version 3, add `if (configVersion.Value < 3 && ...)` on its own line, before the write.
- The Mods menu hides an entry whose tags include an object with a `Browsable` property set to false. The `new { Browsable = false }` tag does that, so players do not see `ConfigVersion`. A `ConfigurationManagerAttributes` tag with `Browsable = false` also hides the entry in ConfigurationManager, while the anonymous form hides it in the framework's Mods menu.

Next: [Keybinds](keybinds.md).
