---
title: Main menu
parent: Framework basics
grand_parent: API reference
has_children: true
nav_order: 8
---

# Main menu

## What it is

The main menu is the first screen the player sees, with buttons such as New Game, Load Game and Quit. `MainMenuButtons` lets your mod add a button of its own to it, and switch the menu's own controls off and on while your mod shows something over the menu. The framework's own MODS button is added the same way.

## When to use it

- Add a button to the main menu that opens your mod's own screen.
- Stop the menu reacting to the keyboard or controller while your own window is open on top of it.
- Check whether the main menu is up before you try to show something over it.

## Quick example

This mod adds a button called HELLO to the main menu. Clicking it writes a line to the log.

```csharp
using ObenseuerCore;

[ModInfo("example.menubutton", "Menu Button", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class MenuButton : FrameworkMod
{
    protected override void OnLoad()
    {
        MainMenuButtons.Add(Mod.Id, "Hello", "HELLO", () => Log.Info("The HELLO button was clicked."));
    }
}
```

The first argument is your mod's id. The second is a short name for the button, which must be different from every other button's name. The third is the text on the button, and the last is your code, which runs when the player clicks it. You can call `Add` from `OnLoad`, even though the menu may not exist yet: the framework builds the button when the menu appears.
