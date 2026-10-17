---
title: Loading
parent: Framework mods
grand_parent: Framework basics
nav_order: 4
---

# Loading

These notes cover when your mod starts, and what happens when it fails or stops.

## Things to know

- The framework loads every framework mod in one go, after all BepInEx plugins have started. Do your starting work in `OnLoad`. <span class="tech">(BepInEx plugins have all run `Awake` by then.)</span>
- `OnLoad` runs once. `Loader.Has(yourOwnId)` is `false` while your mod's own `Awake` and `OnLoad` are running, and becomes `true` when `OnLoad` finishes.
- Unity runs your class's own `Awake` before `OnLoad`. A framework mod stays loaded until the game closes. <span class="tech">(Mono cannot unload an assembly.)</span>

## Limits

- If `OnLoad` throws an error, your mod is switched off. The error goes to your mod's log and to `ObenseuerCore.log`, and your mod's component is destroyed straight away, so its `Update` cannot run on a half-started mod. Every mod that depends on yours is skipped, but other mods and the framework keep running.
- Things your mod did before the error are not undone, such as event subscriptions, saved-data registrations and game patches. Register them last in `OnLoad`.
- An error in your class's `Awake` is logged by Unity, and the framework does not stop it from spreading.
