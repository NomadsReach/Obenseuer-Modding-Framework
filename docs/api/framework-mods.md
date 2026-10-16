---
title: Framework mods API
parent: API reference
nav_order: 100
---

# Framework mods API

Experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later). Needs framework 0.11.0 or later.
Namespace: `ObenseuerCore`.

A framework mod is one DLL that runs on BepInEx 5 and BepInEx 6. The DLL does not reference BepInEx. The framework finds it in `BepInEx\plugins\` when the game starts, and loads it, so one build serves both loaders. Use this form for a new mod. A [BepInEx plugin](../guides/getting-started/packaging.md#bepinex-plugins-advanced) is still needed for a BepInEx API, such as `KeyboardShortcut`.

## A complete mod

```csharp
using ObenseuerCore;

[ModInfo("example.myfirstmod", "My First Mod", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class MyFirstMod : FrameworkMod
{
    protected override void OnLoad()
    {
        Log.Info("loaded");
        ModSetting<float> speed = Config.Bind("General", "Speed", 1f, "Walk speed factor.", 0.5f, 2f);
        Events.WhenReady(() => Hud.ShowHint(Mod.Id, "My First Mod", new[] { "Hello." }));
    }
}
```

## Attributes and base class

```csharp
public sealed class ModInfoAttribute : Attribute            // class, once per class
public ModInfoAttribute(string id, string name, string version);
public string Id { get; }
public string Name { get; }
public string Version { get; }

public sealed class ModDependencyAttribute : Attribute      // class, repeatable
public ModDependencyAttribute(string id);
public ModDependencyAttribute(string id, string minVersion);
public string Id { get; }
public string MinVersion { get; }                           // null when there is no minimum

public abstract class FrameworkMod : MonoBehaviour
{
    public ModInfoAttribute Info { get; }                   // the class's ModInfo
    public CoreMod Mod { get; }                             // save data, keybinds and patch helpers under the mod id
    public CoreLog Log { get; }                             // BepInEx\logs\<id>.log
    public ModConfig Config { get; }                        // BepInEx\config\<id>.cfg
    protected abstract void OnLoad();                       // called once by the framework
}
```

`Mod.Key(...)` returns a `ModKeybind`, as it does for BepInEx plugins. A framework mod gets its `CoreMod` from `Mod`, not from `CoreMod.For`.

## Settings

```csharp
public sealed class ModConfig
{
    public ModSetting<T> Bind<T>(string section, string key, T defaultValue, string description);
    public ModSetting<T> Bind<T>(string section, string key, T defaultValue, string description, T min, T max) where T : IComparable;
}

public sealed class ModSetting<T>
{
    public T Value { get; set; }
    public T Default { get; }
    public event Action Changed;
}
```

- The file is created on the first `Bind`. Binding the same section and key again returns the saved value.
- With `min` and `max`, an `int` or `float` setting is a slider in the Mods menu. A `bool` is a toggle, an enum is a cycle and a `string` is a text field.
- `Changed` runs when the value changes, from the Mods menu or from code. A handler that throws is logged once, and the other handlers still run.

## Loading

- The framework loads all framework mods in one pass, after every BepInEx plugin has run `Awake`. Start your work in `OnLoad`.
- `Loader.Has(ownId)` is false during the mod's own `Awake` and `OnLoad`. It becomes true when `OnLoad` returns.
- `OnLoad` runs once. If it throws, the error goes to the mod's log and to `ObenseuerCore.log`, the mod's component is disabled and destroyed at once, so its `Update` cannot run on a half-initialised mod, and the mods that depend on it are skipped. The event subscriptions, saved-data registrations and patches the mod made before it threw are not undone, so do the registering last. Other mods and the framework keep running.
- Unity runs your class's own `Awake` before `OnLoad`. An exception there is logged by Unity, not isolated by the framework.
- A framework mod is never unloaded. Mono cannot unload an assembly.

## Identity and version

- The id uses letters, digits, `.`, `_` or `-`, and is 1 to 64 characters. It names the log, the config file, the save data and the owner of the patches.
- The version has 2 to 4 numeric parts, such as `0.1` or `1.2.3`. Missing parts count as 0, so `0.11`, `0.11.0` and `0.11.0.0` are the same version. Text, a sign or a space is not allowed, so `-beta`, `+1.0` and ` 1.0 ` are refused. The minimum in a dependency follows the same rule. Before the comparison, a BepInEx plugin's version is cut at its first `-` or `+`, so `1.2.0-beta.1` and `1.2.0+abc` count as `1.2.0`: a prerelease is treated as equal to its release, not lower. Framework mod versions and dependency minimums do not get this cut, so `1.2.0-beta` is refused for them.
- The name must not be empty.
- The ids `ObenseuerCore`, `nomad.obenseuer.core`, `ObenseuerCore.ModKeybinds` and `obenseuer.moddingframework` belong to the framework: a framework mod whose id is one of them or starts with one of them, in any letter case, is skipped with a warning. An id that is a Windows device name, such as `con`, `nul` or `aux`, is skipped too.
- Ids are compared without regard to letter case, against other framework mods and against BepInEx plugin ids.
- Two framework mods with the same id: the first file by path (ordinal order) loads, and the other is skipped with a warning that names both files.
- A framework mod whose id is already used by an installed BepInEx plugin is skipped with a warning.

## Dependencies

- `[ModDependency(id)]` or `[ModDependency(id, minVersion)]` names a framework mod or a BepInEx plugin that must load first. The minimum is compared with the other mod's version.
- A missing dependency, or one older than `minVersion`, skips the mod with a warning. The mods that depend on it are skipped too.
- Dependencies load first. Other mods load in id order.
- A BepInEx plugin cannot depend on a framework mod, because framework mods load after all plugins.
- For a soft check, call `Loader.Has(id)` inside `Events.WhenReady`. In `Start` it can still be false: the framework loads framework mods after every plugin's `Awake`, so a plugin without a `BepInDependency` on the framework can reach `Start` first.

```csharp
public static class Loader
{
    public static bool Has(string id);
}
```

`Loader.Has` is true when a BepInEx plugin with that id is running, or a framework mod with that id is loaded. It works from a framework mod and from a BepInEx plugin. It means running, so it is false for a framework mod whose `OnLoad` threw. The framework's own checks on saved data (items, animals, NPCs and landmarks) ask whether a mod is installed instead. That also counts a framework mod that failed in `OnLoad`, so its saved data is kept. A framework mod that is present on disk but skipped, because a dependency is missing, too old or in a cycle, counts as installed too, so its saved data survives until the dependency is back.

## Mods menu

Settings from `Config` appear in the Mods menu, classic and web, under the mod's name. ConfigurationManager (F1) lists BepInEx plugins only.

An older framework, 0.10.0 or earlier, does not load framework mods, and does not report them.

## Loader-specific members

These members use BepInEx types. A framework mod cannot call them, because it has no BepInEx reference, and the compiler stops with CS0012. Only BepInEx plugins can call them.

| Member | BepInEx type | Framework mods use |
|---|---|---|
| `CoreMod.For(BaseUnityPlugin)` | `BaseUnityPlugin` | `FrameworkMod.Mod` |
| `Loader.Find(string)` | `PluginInfo` | `Loader.Has(string)` |
| `Loader.All()` | `PluginInfo` | none |
| `ModUi.KeyBinding(...)` | `KeyboardShortcut` | none |
| `ModKeyBinding.Set(KeyboardShortcut)` | `KeyboardShortcut` | none |
| `Core` base type | `BaseUnityPlugin` | its static members work |
| `WebUiPlugin` | `BaseUnityPlugin` | none, mods never use it |
