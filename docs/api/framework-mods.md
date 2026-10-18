---
title: Framework mods
parent: Framework basics
grand_parent: API reference
has_children: true
nav_order: 5
---

# Framework mods

Status: experimental. Needs framework 0.11.0 or later.

## What it is

A framework mod is the simplest kind of mod for the game. It is one DLL with one class. The framework finds the DLL in `BepInEx\plugins\` when the game starts and loads it for you. The same DLL runs on both BepInEx 5 and BepInEx 6, the two mod loaders the framework supports, so you do not need one build for each. Your mod does not reference BepInEx at all. This page covers the pieces that make up a framework mod. They are the class you write, the labels that give it a name and a version, its settings, how it loads, and how it depends on other mods.

## When to use it

- Start a new mod. This is the form to use unless you have a reason not to.
- Add settings to a mod that show up in the Mods menu in the game.
- Make your mod wait for another mod, or only run when it is there.

Use a [BepInEx plugin](../guides/getting-started/packaging.md#bepinex-plugins-advanced) instead when you need something from BepInEx itself, such as `KeyboardShortcut`. A table of those cases is at the end of the reference.

## Quick example

This is a complete mod. It writes a line to its log, adds a speed setting to the Mods menu, and shows a hint on screen once a save is loaded.

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

`[ModInfo]` gives the mod its id, its name and its version. `[ModDependency]` says the framework must be at least version 0.11.0. `OnLoad` is where you start your work. `WhenReady` waits until a save is loaded before it runs your code. See [Using events](../guides/getting-started/using-events.md) for more on it.
