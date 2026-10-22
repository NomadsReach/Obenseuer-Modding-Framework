---
title: Saving and quitting
parent: When things run
grand_parent: Getting started
nav_order: 5
---

# Saving and quitting

This page is the last two steps of the order: saving a game, and quitting.

**Step 9. Saving.** A normal save raises `Events.Saved`, and the framework writes each registered entry into the save folder's `mods` folder in the same step, before your own `Saved` handlers run. `Events.AfterSave` then gives you the folder. `SaveData.Register` writes `<modId>.json`; `SaveData.Bind` writes `<modId>.<key>.json`. A save the game makes while a load from the menu or the pause menu is still running writes nothing.

**Step 10. Quit.** The framework writes any settings you changed and have not written yet, and flushes its log. Nothing else is saved at quit, so write anything else you need before the player quits.

Next: [Which hook should I use](which-hook-should-i-use.md).
