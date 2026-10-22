---
title: Once or again and again
parent: When things run
grand_parent: Getting started
nav_order: 1
---

# Once or again and again

This page sorts the framework's calls into the ones that happen once and the ones that happen again and again.

Some calls happen once when the game starts. The rest happen while you play, and most of them happen many times in one session.

| Once per game launch | Each time something happens |
|---|---|
| `Awake` on every plugin, the framework's included | `Events.SceneLoaded`, for every scene the game loads, the main menu included |
| `Awake` on your own mod class, which Unity runs before `OnLoad` | `Events.Loaded`, after a save load and after every door or level change |
| `OnLoad`, once, after every plugin's `Awake` | The action you hand to `Events.WhenReady`, once for each call, at the next ready point |
| | `Events.Saved` and `Events.AfterSave`, each time a save is written |
| | `Events.HourPassed`, once per game hour |
| | `Events.DayPassed`, at each day change |
| | `Update` and `LateUpdate` on your mod, every frame |

`OnLoad` is your setup step. It runs before any save is loaded, so it is where you subscribe to events, register saved data, bind settings and keybinds, and register new game options. It does not run when a save loads, and it does not run when a door opens. The handlers you attach inside `OnLoad` are the things that run later, and each one runs as often as its event fires.

`Events.WhenReady` is a method, not an event. You hand it an action, and the framework runs that action at the next moment a save is ready. A queued action is taken off the queue before it runs, so an action queued once in `OnLoad` runs once, even though later saves and doors have ready points of their own. For work at every load and every door, subscribe to `Events.Loaded` and queue a fresh action there each time. If a save is already loaded and the framework is not busy, `Events.WhenReady` runs your action at once.

The order of all of this is set out step by step in [Starting the game](starting-the-game.md) and the pages after it.

Next: [One play session](one-play-session.md).
