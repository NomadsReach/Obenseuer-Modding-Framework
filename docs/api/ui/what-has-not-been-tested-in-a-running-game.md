---
title: What has not been tested in a running game
parent: UI
grand_parent: Screen and UI
nav_order: 7
---

# What has not been tested in a running game

These parts are built and checked in code, but nobody has tried them in a running game yet. They may look or act slightly differently there.

- Text fields and key binding rows: focus, typing, Escape and Enter inside a real window, and whether the field lock is released when you click away.
- A control callback that throws an error, under a window (the window closes) and outside one (the error is only logged).
- The head, hands and body in the model preview.
- Clicks, the wheel and dragging on tab pages in a Full window.
- Lighting, framing and the look of the backdrop, including a 330 pixel tall preview in a Right panel.
- What happens when the game cannot tell whether the player's controls are on (keys, cursor and camera).
- Switching back to the first or third person view when the window closes, while the game changes the view.
- The preview pausing on a hidden tab, clicks on slider rows and toggle labels, and a hotkey that opens and closes a window with `Find`.
