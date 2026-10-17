---
title: Home
nav_order: 1
permalink: /
---

# Obenseuer Modding Framework

The Obenseuer Modding Framework is a free add-on that makes it easier to mod the game Obenseuer. It is a [BepInEx](glossary.md#bepinex) plugin. BepInEx is the tool that loads mods into the game. The framework works on both BepInEx 5 and BepInEx 6.

It gives every mod the same building blocks, so each mod author does not have to build them again: a shared Mods menu with settings, per-save data, game events (a day passes, the player's money changes), helpers for the game's items, NPCs, shops, doors and more, and safe changes to the game's code.

[Install](install.md){: .btn .btn-primary }
[Make your first mod](guides/getting-started/first-mod.md){: .btn }
[Everything the framework covers](features.md){: .btn }

## Who it is for

- **Players** who want to run mods that need the framework. Follow [Install](install.md). It takes a few minutes and you do not need to know how to code.
- **People who want to make a mod**, even if you have never made one. Follow [Install](install.md), then [Your first mod](guides/getting-started/first-mod.md). It builds a small mod that writes one line to the log.
- **Experienced modders** who want to look something up. Open the [API reference](API.md), which lists every page with one line on what you can do with it. [Events](events.md) lists everything you can react to. The [glossary](glossary.md) explains the terms.

## Where to start

1. [Install](install.md) the framework. Check that `BepInEx\logs\ObenseuerCore.log` exists once you have started the game.
2. Make [your first mod](guides/getting-started/first-mod.md).
3. Browse the [guides](guides/index.md) and the [cookbook](guides/cookbook/index.md) for step-by-step tasks, such as giving an item or showing an HTML panel.
4. Stuck? Read the [FAQ](faq.md).

{: .warning }
This is a beta. Extensive testing has not been done yet. The framework is built for Obenseuer 0.5.08. See [Status](status.md) for what is tested.

## What is new in 0.11.1-beta

Version 0.11.0-beta added `FrameworkMod`, a base class that lets one DLL run on both BepInEx 5 and BepInEx 6, with settings that show in the Mods menu. It also added dialogue branches, HUD bars, screen vignette and blur, NPC control and animation, and more furniture options. Version 0.11.1-beta supports game build 0.5.08 and fixes bugs. The [changelog](https://github.com/NomadsReach/Obenseuer-Modding-Framework/blob/main/CHANGELOG.md) lists every change. The earlier 0.10.0-beta additions, such as player shops, mod NPC routines and the BepInEx 5 build, are on the [status page](status.md#what-is-new-in-0100-beta).
