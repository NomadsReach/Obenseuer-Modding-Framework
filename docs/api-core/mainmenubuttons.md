---
title: MainMenuButtons
parent: Framework core classes
grand_parent: Framework basics
nav_order: 14
---

# MainMenuButtons

`MainMenuButtons` lets your mod add a button to the main menu, next to the framework's MODS button, and turn the main menu's own controls off and on.

- **`MainMenuButtons.Add(string id, string label, Action onClick)`** adds a button to the row under the main menu grid, next to MODS. Buttons from other mods in that row are kept clear of yours.
- **`MainMenuButtons.SetControlsEnabled(bool enabled)`** turns the menu's own controls off and on.
- **`MainMenuButtons.IsOpen`** is `true` while the main menu that the framework added its buttons to is open.

The full details are on the [Main menu](../api/mainmenu.md) page.
