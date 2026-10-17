---
title: Settings
parent: Framework mods
grand_parent: Framework basics
nav_order: 3
---

# Settings

A setting is a value the player can change in the Mods menu and that is saved between sessions. You make one with `Config.Bind`.

```csharp
public sealed class ModConfig
{
    // Creates a setting, or reads the saved value of one that already exists.
    // section groups settings in the file; key names this setting; description is shown to the player.
    public ModSetting<T> Bind<T>(string section, string key, T defaultValue, string description);

    // The same, with a smallest and largest value. An int or float setting then shows as a slider.
    public ModSetting<T> Bind<T>(string section, string key, T defaultValue, string description, T min, T max) where T : IComparable;
}

public sealed class ModSetting<T>
{
    public T Value { get; set; }        // the current value; setting it saves it
    public T Default { get; }           // the value you gave as the default
    public event Action Changed;        // runs when the value changes
}
```

- **The settings file is created the first time you call `Bind`.** If you bind the same section and key again, you get the value that was saved, not the default.
- **The Mods menu picks the right control for the type.** A number with a smallest and largest value is a slider. A `bool` is a toggle, an enum is a button that cycles through its values, and a `string` is a text field.
- **`Changed` runs when the value changes, whether the player changed it in the Mods menu or your code did.** If one of your handlers throws an error, the framework logs it once, and the other handlers still run.
