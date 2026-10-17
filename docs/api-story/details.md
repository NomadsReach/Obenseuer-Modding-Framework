---
title: Details
parent: Quests, dialogue and law
grand_parent: Story
nav_order: 6
---

# Details

Target: Obenseuer 0.5.08 (build 25969064), Unity Mono.
Namespace: `ObenseuerCore`. Each API writes to its own log in `BepInEx\logs`, named after its feature group.

Everything not listed here is internal. Items marked **experimental** may change.

The framework changes some of the game's methods when it starts, so it can tell your mod about tasks, conversations and crimes. This page calls each group of those changes a feature group. Each one has a name that shows up in the log. The three on this page are `obenseuer.moddingframework.quests`, `obenseuer.moddingframework.dialogue` and `obenseuer.moddingframework.law`. Logs go to `BepInEx\logs\<name>.log`.

## When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).

## What to check for and what comes back

- **Check `IsAvailable` first when it matters.** Each of `Quests`, `Dialogue` and `Law` has one. See [Rules for all three APIs](rules-for-all-three-apis.md#rules-for-all-three-apis) for what it means and what you get when it is `false`.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the changes for one of these APIs cannot be made, that API's `IsAvailable` becomes `false` and nothing else is affected.** The other two story APIs and the rest of the framework keep working.

## What you can't do (yet)

- **Writing quests and conversations from scratch is not on this page.** See [Quest authoring](../api/quest-authoring.md) and [Dialogue authoring](../api/dialogue-authoring.md).
- **You can't change the fine multiplier or how fast the prison drains the player.** The ideas were `SetFineMultiplier` and `SetPrisonDrainPerHour`, but they would write game values whose lifetime is not understood, so they are not in this version.
