---
title: Details
parent: Quest authoring
grand_parent: Story
nav_order: 7
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.questauthoring.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes one of the game's methods when it starts, so a task made here is matched by its id. This page calls that a feature group: a named set of the framework's changes for one feature. Its feature group is `obenseuer.moddingframework.questauthoring`, the name you will see in the log.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../../glossary.md#main-thread)).

## What to check for

- `QuestAuthoring.IsAvailable` tells you whether you can make and change tasks. Every call that changes a task returns `false` (or `null` for `Create`) while it is `false`. It also needs `Quests.IsAvailable` to be `true`.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework turns off the [feature group](../../glossary.md#feature-group) that depends on it, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the change that matches tasks by id cannot be made, quest authoring is switched off. `QuestAuthoring.IsAvailable` becomes `false`. The `Quests` page keeps working for the game's own tasks.
