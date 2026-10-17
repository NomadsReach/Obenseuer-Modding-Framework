---
title: Common tasks
parent: Main menu
grand_parent: Framework basics
nav_order: 1
---

# Common tasks

Stop the menu reacting to input while your own window is open, and give control back when it closes. Every `false` needs a matching `true`.

```csharp
MainMenuButtons.SetControlsEnabled(Mod.Id, false);
MainMenuButtons.SetControlsEnabled(Mod.Id, true);
```

Check whether the main menu is up and your buttons are on it.

```csharp
if (MainMenuButtons.IsOpen) Log.Info("The main menu is open.");
```

Find out whether your button was added. `Add` gives back `false` when the name was already taken.

```csharp
if (!MainMenuButtons.Add(Mod.Id, "Settings", "MY SETTINGS", () => Log.Info("Settings clicked.")))
    Log.Warn("The button name is already taken.");
```
