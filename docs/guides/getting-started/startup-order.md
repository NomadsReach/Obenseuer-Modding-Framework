---
title: When things run
parent: Getting started
nav_order: 10
has_children: true
---

# When things run

This set of pages is the order of the framework's calls, from the game starting to the game closing. Each step says what is safe to call there, and what does not exist yet.

The order in five lines:

1. BepInEx preloader patchers run first, before the game loads, and then every plugin's `Awake` runs.
2. The framework's `Start` finds your mod and calls `OnLoad`, which is where you subscribe to events and register your own things.
3. The main menu loads, and the framework forgets the session.
4. A new game, a save load, a door or level change and every frame each bring their own calls.
5. Saving writes your registered data, and quit writes only the settings you changed and the log.

Read them in this order:

- [Once or again and again](startup-order/once-or-again-and-again.md): which calls happen once, which happen again and again, and why `OnLoad` only sets up hooks.
- [One play session](startup-order/one-play-session.md): one small mod followed through a whole session, step by step.
- [Starting the game](startup-order/starting-the-game.md): steps 1 to 4, the preloader, plugin `Awake`, the framework's `Start` and `OnLoad`, and the main menu.
- [Loading and playing](startup-order/loading-and-playing.md): steps 5 to 8, a new game, loading a save, a door or level change, and every frame.
- [Saving and quitting](startup-order/saving-and-quitting.md): steps 9 and 10, saving and quit.
- [Which hook should I use](startup-order/which-hook-should-i-use.md): a table that matches a job to the call to use.

## Where to go next

Back to [Using events](using-events.md) for the clock, scene and save events, or to [Save data](save-data.md) for per-save data.
