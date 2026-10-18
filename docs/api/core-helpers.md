---
title: Framework helpers
parent: Framework basics
grand_parent: API reference
has_children: true
nav_order: 4
---

# Framework helpers

This page uses `KeyCode` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Status: experimental.

## What it is

These are three small shortcuts that remove the setup code almost every mod repeats. None of them adds a new feature. Each one calls an existing part of the framework for you.

- `CoreMod` gives your mod one handle that does saving, keybinds, logging and patching under your mod id, so you do not type the id each time.
- The `CorePatcher` short helpers (`Prefix`, `Postfix`, `Finalizer`) change a game method when you only need to run your own code before it, after it, or when it throws an error.
- `Events.WhenReady` runs your code once a save is loaded, even if the save was loaded before your code asked.

Saving a value with `SaveData.Bind` is on the [game save data page](savedata-game.md).

## When to use it

- Register a save slot and a keybind for your mod in two short lines, without repeating your mod id.
- Run your own code before or after a game method, in one line.
- Run something once, as soon as a save is ready, without worrying whether it is already ready.

## Quick example

This mod uses a `CoreMod` handle to register a keybind and a save slot, and to run a method after each new day starts. The handle comes from `Mod`, which every framework mod has. A framework mod is the simple kind of mod described on the [framework mods page](framework-mods.md).

```csharp
using ObenseuerCore;
using UnityEngine;

public sealed class MyState
{
    public int Days;
}

[ModInfo("example.helpers", "Helpers", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class Helpers : FrameworkMod
{
    private MyState state = new MyState();

    protected override void OnLoad()
    {
        ModKeybind open = Mod.Key("open", "Open menu", KeyCode.F7);
        Mod.Save<MyState>("state", () => state, s => state = s ?? new MyState());
        bool ok = Mod.Postfix(typeof(TimeOfDayAzure), "IncreaseDayCounter", typeof(Helpers), "AfterDay");
        Log.Info("keybind " + (open != null) + ", day patch " + ok);
    }

    private static void AfterDay()
    {
    }
}
```

`Mod.Key` makes a keybind the player can change in the options. `Mod.Save` stores your `MyState` in the player's save, and gives it back when the save loads. `Mod.Postfix` makes `AfterDay` run right after the game's `IncreaseDayCounter` method, and gives `true` when that worked.
