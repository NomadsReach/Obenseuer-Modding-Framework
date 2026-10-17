---
title: The MODS button
parent: Main menu
grand_parent: Framework basics
nav_order: 6
---

# The MODS button

The framework's own MODS button is added with this same API, so it behaves the way your button does.

- **The MODS button opens the Mods window, which covers the whole main menu.** While it is open the menu's controls are switched off. When the window closes, they come back and MODS is selected again.
  - Technical: MODS is `Add("Mods", "MODS", ...)`. Its object is `Mod:Mods`. Its click calls `SetControlsEnabled("obenseuer.moddingframework.ui", false)` and opens the window, which is the Full dock. When the window closes, the framework calls `SetControlsEnabled(true)`.
- **Clicking MODS does nothing if the framework's input is not ready.** One line goes to the log the first time.
  - Technical: the line is `MODS not opened: input is locked or the input patches are not ready`, written while `ModMenuInput.Ready` is `false`.
- **Another mod can take the name `Mods` first.** If another plugin registers the name before the framework, the MODS button is not added, and the framework writes a line to the log.
  - Technical: the line is `MODS main menu button not added: id Mods is already taken`, written to the ui log.
