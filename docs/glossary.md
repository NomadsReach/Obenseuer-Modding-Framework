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

A BepInEx mod. It is a class that derives from `BaseUnityPlugin` and has a `[BepInPlugin]` attribute. Each plugin has its own folder under `BepInEx\plugins\`.

## GUID

The unique id of a plugin. It is the first value in `[BepInPlugin]`. The framework's GUID is `obenseuer.moddingframework`. Pick your own GUID, and do not use one that starts with `obenseuer.moddingframework` or `nomad.obenseuer.core`. Both prefixes are reserved for the framework.

## Publicizer

A tool that makes some private game members public at build time, so the framework can use them. The framework's build runs it. Mods do not need to run it.

## Mod id

The id your mod uses for its config file, its log file, its save data and its keybinds, such as `example.hello`. It is usually the same as your plugin's GUID.

## WebUi

The optional framework module that shows mod pages written in HTML and CSS, in a WebView2 window. It is a second DLL, `ObenseuerCore.WebUi.dll`. See [Install](install.md#optional-webui).

## Blueprint

A crafting blueprint for a piece of mod furniture, with its inputs, craft time and skill. The player gets it from a pickup, a blueprint magazine or the mail. See [Furniture](api/furniture.md#blueprints-and-flatpacks).

## Flatpack

The packed form of a piece of mod furniture, given as the game's Furniture Flatpack item. See [Furniture](api/furniture.md#blueprints-and-flatpacks).

## Owner

The mod id a mod passes to a call that holds a change for it, such as a speed factor or a camera hold. Each owner keeps its own claim, so two mods do not overwrite each other. See [Using the framework alongside other mods](guides/multiple-mods.md).

## Player shop

A furniture manager that the player runs as a shop, with its own prices, till and sales. See [Player shops](api/economy.md#player-shops).
