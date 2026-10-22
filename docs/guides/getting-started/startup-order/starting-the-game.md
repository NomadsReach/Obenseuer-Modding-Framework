---
title: Starting the game
parent: When things run
grand_parent: Getting started
nav_order: 3
---

# Starting the game

This page is steps 1 to 4 of the order: the preloader, every plugin's `Awake`, the framework's `Start` and your `OnLoad`, and the main menu.

**Step 1. BepInEx preloader patchers run first, before the game loads.** The framework ships one optional patcher, the publicizer, and it is off by default. It runs only when you set `Publicize = true` in its config file. Nothing of your mod runs at this point.

**Step 2. Every plugin's `Awake` runs.** The framework's own `Awake` does all of its feature setup here. It reads its settings, sets up the save gate and then the events, then applies its game patches and sets up every area: items and recipes, furniture, NPCs, shops, quests, dialogue and the rest. The WebUi plugin runs after the framework, because it declares a dependency on it. A plugin with no dependency on the framework can run before it, so `Loader.Has` can be `false` in that plugin's `Awake`.

Safe here: `Config.Bind`, `SaveData.Register`, `SaveData.Bind`, `Events` subscriptions, `Keybinds.Register`, `ConsoleCommands.Register`, and your own `Awake` work.
Not here yet: nothing of the framework is guaranteed if you run before its `Awake`, and no game scene has loaded.

**Step 3. The framework's `Start` loads every framework mod.** It scans the plugins folder, sorts the mods by `[ModDependency]` (a mod loads after the mods it depends on, and mods with no ordering between them load in alphabetical order by id), then adds each mod's component and calls its `OnLoad`. So `OnLoad` runs after every plugin's `Awake`. This is the step the rest of these pages are written around.

Safe in `OnLoad`: `Events` subscriptions, `SaveData.Register` and `SaveData.Bind`, `Config.Bind`, keybinds, console commands, item, recipe, furniture and NPC definitions, `MainMenuButtons.Add`, and new game options. New game options must be registered here, because this is the only moment guaranteed to run before a save loads or a new game starts.
Not here yet: any scene content or NPC, and no save is loaded, so `SaveData.Folder` is `null`.
If `OnLoad` throws, the framework destroys the component it added, so your `Update` never runs, and every mod that depends on yours is skipped. The other mods still load. Event subscriptions and save data registrations you already made are not undone.

**Step 4. The main menu.** The framework adds its own MODS button during its `Awake`, so the button is there from the first main menu. Pressing it opens the Mods window, which covers the menu and switches the menu's controls off. When the `Main Menu` scene loads, the framework forgets the session: it clears the loaded save folder and calls the load callback of every registered save entry with no data, so per-save mod data goes back to its defaults.

Safe here: the Mods menu, settings and keybinds.
Not here yet: no save is loaded and no game scene content exists.

Next: [Loading and playing](loading-and-playing.md).
