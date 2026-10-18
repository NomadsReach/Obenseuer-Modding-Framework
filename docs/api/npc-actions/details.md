---
title: Details
parent: NPC actions
grand_parent: People
nav_order: 9
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.npcactions.log`.

Everything not listed in this reference is internal and may change without notice.

## If something fails to set up

The framework changes some of the game's methods when it starts, so it can tell your mod when something happens. This part of the API changes none of them and has no events, so there is nothing that can fail to set up. Its name in the log is `obenseuer.moddingframework.npcactions`.

## When you can call it

- Call everything from your mod's normal code (OnLoad, event handlers, Update), not from a background thread you started. The game only runs safely on the main thread (see the [glossary](../../glossary.md#main-thread)).
- An error from the game is caught and you get `false`. Only the first error of each action is logged. Later errors of that action are not logged again.

## Limits

- `Say` and `Comment` follow the game's own rules for speech. Because of the game's priorities, conditions and conversation state, they give `true` when the line was asked for and not when it was shown.
- A `Teleport` is not final. It moves the character, but the NPC's schedule may pull it back to its target on the next tick.
