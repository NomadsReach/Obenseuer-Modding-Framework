---
title: SetControlsEnabled
parent: Main menu
grand_parent: Framework basics
nav_order: 4
---

# SetControlsEnabled

Use this when your mod opens its own window over the main menu. While your window is open you do not want the menu's buttons to react to the keyboard or controller. Each `false` call is a hold on the controls, and the controls only come back when every hold has been released.

- **The version with `modId` counts holds for each mod, so one mod's `true` does not release another mod's `false`.** The version without `modId` counts under one key shared by every caller that does not give a mod id.
- **Every `false` needs a matching `true`.** Two `false` calls need two `true` calls before the controls come back. A `true` with no `false` before it does nothing, and the count never goes below zero.
- **The first `false` switches the menu's controls off, and the last `true` switches them back on and selects the button the player pressed last.** If the player had pressed a button, it is selected again, so they land where they were.
  - Technical: when the count goes from 0 to 1, the framework saves the main menu navigation handler's `ControlsEnabled` (or `true` when no handler exists). While the count is above 0, it sets `ControlsEnabled` to `false` each frame, because the game turns its own controls back on when the handler is enabled again. When the count reaches 0, it restores the saved value and selects the button pressed last.
- **A hold does not carry over to the next time the main menu loads.** Holds are cleared when the main menu loads again, so a `true` that comes after that does nothing.
  - Technical: the count resets to 0 when the main menu scene loads again.
