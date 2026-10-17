---
title: Glossary
nav_order: 10
---

# Glossary

## Framework

The Obenseuer Modding Framework, the DLL that mods use, in a BepInEx 6 and a BepInEx 5 build. The DLL name also shows in some code names, such as `CoreLog` and `CorePatcher`, and in the log file `ObenseuerCore.log`.

## BepInEx

The mod loader for Unity games. It loads plugins from `BepInEx\plugins\`. The framework and your mods run on BepInEx 5.4.23 or later (Mono) or BepInEx 6 Unity.Mono. See [Install](install.md).

## Harmony

A library that changes methods while the game runs, called patching. BepInEx ships it. The framework uses it inside `CorePatcher`. Mods should patch through `CorePatcher`, not with Harmony directly. See [Patching](guides/getting-started/patching.md).

## Plugin

A BepInEx mod. It is a class that derives from `BaseUnityPlugin` and has a `[BepInPlugin]` attribute. Each plugin has its own folder under `BepInEx\plugins\`. The framework also has [framework mods](api/framework-mods.md), a simpler form for new mods: one DLL with no BepInEx reference, which runs on BepInEx 5 and 6.

## GUID

The unique id of a plugin. It is the first value in `[BepInPlugin]`. The framework's GUID is `obenseuer.moddingframework`. Pick your own GUID, and do not use one that starts with `obenseuer.moddingframework` or `nomad.obenseuer.core`. Both prefixes are reserved for the framework. A plugin GUID is not the id the framework gives an object in a scene; see [Object id](#object-id).

## Publicizer

A tool that makes some private game members public at build time, so the framework can use them. The framework's build runs it. Mods do not need to run it.

## Mod id

The id your mod uses for its config file, its log file, its save data and its keybinds, such as `example.hello`. It is usually the same as your plugin's GUID.

## WebUi

The optional framework module that shows mod pages written in HTML and CSS, in a WebView2 window. It is a second DLL, `ObenseuerCore.WebUi.dll`. See [Install](install.md#optional-webui).

## Blueprint

A crafting blueprint for a piece of mod furniture, with its inputs, craft time and skill. The player gets it from a pickup, a blueprint magazine or the mail. See [Furniture](api/furniture/blueprints-and-flatpacks.md#blueprints-and-flatpacks).

## Flatpack

The packed form of a piece of mod furniture, given as the game's Furniture Flatpack item. See [Furniture](api/furniture/blueprints-and-flatpacks.md#blueprints-and-flatpacks).

## Owner

The mod id a mod passes to a call that holds a change for it, such as a speed factor or a camera hold. Each owner keeps its own claim, so two mods do not overwrite each other. An owner is not a [feature group](#feature-group). See [Using the framework alongside other mods](guides/multiple-mods.md).

## Player shop

A furniture manager that the player runs as a shop, with its own prices, till and sales. See [Player shops](api/economy/player-shops.md#player-shops).

## OC

Open Sewer Coins, the game's everyday currency (`MoneyCurrency.OC`). It is a whole number. See [Money](api/economy/money-cash.md#money-cash).

## RM

Real Money, the game's second currency (`MoneyCurrency.RM`). It is counted in tenths. See [Money](api/economy/money-cash.md#money-cash).

## Main thread

The one thread the game and Unity use to run the game. Your mod's normal code, such as `OnLoad` and your event handlers, runs on it. A background thread you start yourself is not the main thread, and it must not call the game or the framework.

## Feature group

A named set of the framework's changes to the game for one feature, such as `obenseuer.moddingframework.npcs`. If one change in the group fails to install, the framework writes a "patch failed on" error line, removes all of that group's changes, turns the group off, and writes a second error line, "core patch failed, all patches from ...". Both lines go to `BepInEx\logs\<feature group>.log`, and the rest of the framework keeps working. A feature group is not an [Owner](#owner).

## Interceptor

A handler your mod adds to run before the game acts, which can stop the game from acting. `Survival.Consumables.Intercept` runs before an item is used, and `Dialogue.Intercept` runs before a conversation starts. The handler gets an args object, and setting `args.Cancel = true` skips the action. See [Survival](api-core/survival.md#survival) and [Story](api-story.md).

## Modal view

A WebUi page opened with `WebUi.Open(..., modal)` set to `true`. While it is showing, the page takes over the game's controls and the cursor is shown, so the player cannot move or look around. A view that is not modal leaves the controls alone. See [WebUi](api/webui.md).

## WebView2

The Microsoft Edge component that draws a WebUi page. The framework talks to it through a small native library, the WebUi shim `ObxWebView.dll`, which sits next to `ObenseuerCore.WebUi.dll`. If the shim is missing or cannot start, `WebUi.IsAvailable` is `false`. See [WebUi](api/webui.md).

## Relay

A hidden switch-like object in a scene. When a relay fires, it passes the signal on to other things, such as opening a door or moving a quest to its next step. See [Scene logic](api/scenelogic.md).

## Level changer

A door, use point or invisible area that takes the player to another level. See [Levels](api/levels.md).

## Entrypoint

A named spawn point in a level. The game uses entrypoints when the player changes level and for teleports inside a level. See [Travel](api/player/travel.md#travel) and [Levels](api/levels.md).

## Claim

What your mod holds on an NPC when it takes the NPC over, for example so the NPC stands still while your mod runs a scene. Each mod has its own claim, and the NPC goes back to normal once no mod holds it any more. See [NPC control](api/npc-control.md) and [NPC movement](api/npc-movement.md).

## Object id

The text the framework uses to name one object in the world, also called a scene id. It is the object's own `GUID` field if it has one, then its save id if it can be saved, and otherwise its path in the scene. A path id changes when objects next to it in the scene are added, removed or moved. Not the same as a plugin [GUID](#guid).

## Savable object

A game object the game's own save system can store, which has a `GUID`. `SaveData.Capture` reads its saved state as text, and `Apply` writes it back. See [Game save data](api/savedata-game.md).

## Building area

A part of a building that holds furniture the player places, with its own id. `Building.AllAreaIds()` lists them. See [Housing](api/housing.md).

## Witness

A game component that lets a person or a camera see the player. `Surveillance.IsPlayerWitnessed` is `true` when any enabled witness can see the player right now. See [Surveillance](api/surveillance.md).

## Mod NPC

A new person your mod adds to the game. The framework builds it as a copy of a person the game already has, with its own name and place. See [Mod NPCs](api/modnpcs.md).

## Lobby

A group of players connected to each other through Steam, which mods can host or join. Every player in it needs the same mods. See [Networking](api/net.md).

## Peer

Another player in your lobby that you can send messages to. `Net.Peers` lists them as SteamID64 numbers. It does not include you. See [Networking](api/net.md).

## SteamID64

The number that names one Steam account. The framework uses it as the id of a player in a lobby, for example in `Lobby.Members` and `Net.Peers`. See [Networking](api/net.md).

## Lua

The small scripting language the game's dialogue system uses. A dialogue line can have a Lua condition, which decides whether the line is offered, and a Lua script, which runs when the line is reached. See [Dialogue authoring](api/dialogue-authoring.md).

## Template

An existing game item, recipe or piece of furniture that a new mod item is copied from. The copy starts with the template's look and behaviour, and your mod then changes what it needs. Furniture names its template with `TemplatePath`. See [Registry](api/registry.md) and [Furniture](api/furniture.md).
