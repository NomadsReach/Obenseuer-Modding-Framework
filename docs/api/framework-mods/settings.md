---
title: Settings
parent: Framework mods
grand_parent: Framework basics
nav_order: 3
---

# Settings

A setting is a value the player can change in the Mods menu and that is saved between sessions. You make one with `Config.Bind`.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModConfig</code> <span>your mod's settings</span></p>

Your mod gets one from its `Config` property. Each `Bind` call makes one setting and returns it as a `ModSetting<T>`.

<div class="api-pair" markdown="1">

```csharp
public sealed class ModConfig
{
    public ModSetting<T> Bind<T>(string section, string key, T defaultValue, string description);
    public ModSetting<T> Bind<T>(string section, string key, T defaultValue, string description, T min, T max) where T : IComparable;
}
```

| Method | Returns | What it does |
|---|---|---|
| `Bind<T>(section, key, defaultValue, description)` | `ModSetting<T>` | Creates a setting, or reads the saved value of one that already exists. `section` groups it in the file, `key` names it, and `description` is shown to the player. |
| `Bind<T>(string section, string key, T defaultValue, string description, T min, T max) where T : IComparable` | `ModSetting<T>` | The same, with a smallest and largest value. An int or float setting then shows as a slider. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModSetting&lt;T&gt;</code> <span>one setting</span></p>

`Bind` gives you this. Read `Value` to use the setting, and set `Value` to change it and save it.

<div class="api-pair" markdown="1">

```csharp
public sealed class ModSetting<T>
{
    public T Value { get; set; }
    public T Default { get; }
    public event Action Changed;
}
```

| Property | Type | What it means |
|---|---|---|
| `Value` | `T` | The current value. Setting it saves it. |
| `Default` | `T` | The value you gave as the default. |

| Event | Gives you | When it fires |
|---|---|---|
| `Changed` | Nothing | When the value changes. |

</div>
</div>

## Things to know

- The settings file is created the first time you call `Bind`. If you bind the same section and key again, you get the value that was saved, not the default.
- The Mods menu picks the right control for the type. A number with a smallest and largest value is a slider. A `bool` is a toggle, an enum is a button that cycles through its values, and a `string` is a text field.
- `Changed` runs when the value changes, whether the player changed it in the Mods menu or your code did. If one of your handlers throws an error, the framework logs it once, and the other handlers still run.
