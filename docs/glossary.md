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

## Types that come from Unity

Some calls take or return a type the framework did not invent. They belong to Unity, the engine Obenseuer is built on, so they behave the same way they do in any Unity game. You do not need to learn Unity to use the framework, but here is what each one is when you meet it in a signature.

## KeyCode

A single key or mouse button on the keyboard, written as a name: `KeyCode.F`, `KeyCode.Escape`, `KeyCode.Mouse0`, `KeyCode.None` for nothing. The framework uses it for keybinds. Full list: [UnityEngine.KeyCode](https://docs.unity3d.com/ScriptReference/KeyCode.html).

## Vector3

A point or a direction in the world, as three numbers: x across, y up, z forward. `new Vector3(10f, 2f, 5f)` is a place, `Vector3.up` is a direction, `Vector3.zero` is the origin. Distances are in metres. Full reference: [UnityEngine.Vector3](https://docs.unity3d.com/ScriptReference/Vector3.html).

## Quaternion

A rotation. You rarely build one by hand: use `Quaternion.identity` for no rotation, or `Quaternion.Euler(0f, 90f, 0f)` to turn 90 degrees around the up axis. Full reference: [UnityEngine.Quaternion](https://docs.unity3d.com/ScriptReference/Quaternion.html).

## GameObject

One thing in the scene: an item, a door, an NPC, a light. Everything the player can see is a `GameObject`, and each one carries components that give it behaviour. Full reference: [UnityEngine.GameObject](https://docs.unity3d.com/ScriptReference/GameObject.html).

## Transform

Where a `GameObject` is: its position, rotation and scale, and its parent and children. `go.transform.position` reads a world position. Full reference: [UnityEngine.Transform](https://docs.unity3d.com/ScriptReference/Transform.html).

## Component

A piece of behaviour attached to a `GameObject`, such as a collider, a light or one of the game's own scripts. You get one with `GetComponent<T>()`. Full reference: [UnityEngine.Component](https://docs.unity3d.com/ScriptReference/Component.html).

## Color

A colour as four numbers from 0 to 1: red, green, blue and alpha, where alpha is how solid it is. `Color.white`, or `new Color(1f, 0f, 0f, 1f)` for solid red. Full reference: [UnityEngine.Color](https://docs.unity3d.com/ScriptReference/Color.html).

## Sprite and Texture2D

A `Texture2D` is an image in memory. A `Sprite` is an image prepared for drawing in the interface. The framework loads both from a file for you where it offers that. References: [Texture2D](https://docs.unity3d.com/ScriptReference/Texture2D.html) and [Sprite](https://docs.unity3d.com/ScriptReference/Sprite.html).

## AudioClip

A sound the game can play. Full reference: [UnityEngine.AudioClip](https://docs.unity3d.com/ScriptReference/AudioClip.html).

## Mesh

The shape of a model: its points and the triangles between them. `ModelHandle.CreateMesh()` gives you one from a loaded model file. Full reference: [UnityEngine.Mesh](https://docs.unity3d.com/ScriptReference/Mesh.html).

## Scene

One loaded area of the game, such as an apartment interior or the sewer. Scenes have names, and the framework uses those names in its scene events and in world placement. Full reference: [UnityEngine.SceneManagement.Scene](https://docs.unity3d.com/ScriptReference/SceneManagement.Scene.html).

## Action and Action&lt;T&gt;

Not Unity but plain C#: a method you hand to something else so it can call it back. `Action` takes nothing, `Action<int>` takes one number, `Action<MoneyChangedArgs>` takes one argument object. Every event on the [events page](events.md) says which one it hands your handler.
