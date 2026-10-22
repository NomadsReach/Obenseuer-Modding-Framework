---
title: Which hook should I use
parent: When things run
grand_parent: Getting started
nav_order: 6
---

# Which hook should I use

This page matches a job you want to do to the call that does it, in one table.

| I want to... | Use... |
|---|---|
| Run setup once, at startup | [`FrameworkMod.OnLoad`](../../../api/framework-mods/loading.md) |
| Add a setting or a keybind | [`Config.Bind`](../config.md), [`Keybinds.Register`](../keybinds.md) |
| Keep data per save | [`SaveData.Register` or `SaveData.Bind`](../save-data.md) |
| Add a new game option | [`NewGameOptions.RegisterToggle` and the other register calls](../../../api/new-game-options.md) |
| Run code once a save is ready | [`Events.WhenReady`](../using-events.md) |
| Read saved data after a load | [`Events.Loaded` or `Events.WhenReady`](../../../api/savedata-game.md) |
| React to a door or a level change | [`Events.SceneLoaded`](../../../events.md) |
| Create quests | [`Quests.Ready`](../../../api-story/quests-experimental.md) |
| React to the clock | [`Events.HourPassed`, `Events.DayPassed`](../../../events.md) |
| React to a save being written | [`Events.Saved`, `Events.AfterSave`](../../../events.md) |
| Run something every frame | `Update` on your mod |
| Add a button to the main menu | [`MainMenuButtons.Add`](../../../api/mainmenu/the-mods-button.md) |
| Run code for a new game only | [`Events.NewGameStarted`](../../../api/new-game-options.md) |
| Change a game method | [`CorePatcher`](../patching.md) |

Next: [When things run](../startup-order.md) for the links to the rest of the guides.
