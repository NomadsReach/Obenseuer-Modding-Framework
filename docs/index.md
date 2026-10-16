---
title: Home
nav_order: 1
permalink: /
---

# Obenseuer Modding Framework

The Obenseuer Modding Framework is a base for mods of the game Obenseuer. It runs as a BepInEx 5 or BepInEx 6 plugin. It gives mods a shared settings menu, save data, game events, game helpers and safe patching. The framework ships as one zip with a BepInEx 6 build (`ObenseuerCore.dll`) and a BepInEx 5 build (`ObenseuerCore.BepInEx5.dll`).

[Install](install.md){: .btn .btn-primary }
[Make your first mod](guides/getting-started/first-mod.md){: .btn }
[Everything the framework covers](features.md){: .btn }

{: .warning }
This is a beta. Extensive testing has not been fully done yet. See [Status](status.md).

## What is new in 0.11.1-beta

Version 0.11.0-beta adds `FrameworkMod`, a base class that lets one DLL run on both BepInEx 5 and BepInEx 6, with settings that show in the Mods menu. It also adds dialogue branches, HUD bars, screen vignette and blur, NPC control and animation, and more furniture options. Version 0.11.1-beta supports game build 0.5.08 and fixes bugs. The [changelog](https://github.com/NomadsReach/Obenseuer-Modding-Framework/blob/main/CHANGELOG.md) lists every change. The additions of 0.10.0-beta, such as player shops, mod NPC routines and the BepInEx 5 build, are on the [status page](status.md#what-is-new-in-0100-beta), and [features](features.md) shows where each part fits.
