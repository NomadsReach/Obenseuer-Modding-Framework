---
title: Loading
parent: Framework mods
grand_parent: Framework basics
nav_order: 4
---

# Loading

- **The framework loads every framework mod in one go, after all BepInEx plugins have started.** Do your starting work in `OnLoad`.
  - Technical: BepInEx plugins have all run `Awake` by then.
- **`OnLoad` runs once.** `Loader.Has(yourOwnId)` is `false` while your mod's own `Awake` and `OnLoad` are running, and becomes `true` when `OnLoad` finishes.
- **If `OnLoad` throws an error, your mod is switched off.** The error goes to your mod's log and to `ObenseuerCore.log`, your mod's component is destroyed straight away, so its `Update` cannot run on a half-started mod, and every mod that depends on yours is skipped. Other mods and the framework keep running. Things your mod did before the error are not undone, such as event subscriptions, saved-data registrations and game patches, so register them last in `OnLoad`.
- **Unity runs your class's own `Awake` before `OnLoad`.** An error in `Awake` is logged by Unity, and the framework does not stop it from spreading.
- **A framework mod stays loaded until the game closes.** The game cannot unload a mod while it runs.
  - Technical: Mono cannot unload an assembly.
