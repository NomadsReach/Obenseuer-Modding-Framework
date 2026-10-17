---
title: Attributes and base class
parent: Framework mods
grand_parent: Framework basics
nav_order: 2
---

# Attributes and base class

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

An attribute is a label you put above your class in square brackets. `ModInfo` gives the mod its id, name and version, and `ModDependency` names a mod yours needs. Your class inherits from `FrameworkMod`, which gives you a log, settings and the other tools a mod needs.

```csharp
public sealed class ModInfoAttribute : Attribute            // goes on your class, once
{
    public ModInfoAttribute(string id, string name, string version);
    public string Id { get; }        // the mod's id, such as example.myfirstmod
    public string Name { get; }      // the name shown in the Mods menu
    public string Version { get; }   // the mod's version, such as 0.1.0
}

public sealed class ModDependencyAttribute : Attribute      // goes on your class, as many times as you need
{
    public ModDependencyAttribute(string id);                    // the mod must be there
    public ModDependencyAttribute(string id, string minVersion); // the mod must be there and at least this version
    public string Id { get; }            // the id of the mod you need
    public string MinVersion { get; }    // the oldest version you accept; null when any version will do
}

public abstract class FrameworkMod : MonoBehaviour
{
    public ModInfoAttribute Info { get; }   // the ModInfo you wrote on your class
    public CoreMod Mod { get; }             // your mod's handle for save data, keybinds and game patches
    public CoreLog Log { get; }             // writes to BepInEx\logs\<your id>.log
    public ModConfig Config { get; }        // your mod's settings, stored in BepInEx\config\<your id>.cfg
    protected abstract void OnLoad();       // the framework calls this once; start your work here
}
```

- **`Mod` is the handle you use for your mod's saved data, keybinds and game patches.** `Mod.Key(...)` gives a keybind, the same as for a BepInEx plugin. A framework mod gets its handle from `Mod`. It does not use `CoreMod.For`, which needs a BepInEx plugin.
  - Technical: `CoreMod.For(BaseUnityPlugin)` takes a BepInEx type, which a framework mod cannot reference.
