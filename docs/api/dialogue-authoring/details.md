---
title: Details
parent: Dialogue authoring
grand_parent: Story
nav_order: 8
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Source: `src/Story/DialogueAuthoring.cs`. Logs go to `BepInEx\logs\obenseuer.moddingframework.dialogueauthoring.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

This API builds on `Dialogue` (`src/Story/Dialogue.cs`), so it needs `Dialogue.IsAvailable` to be `true`. The framework changes some of the game's methods when it starts so `Dialogue` works. This page calls that set of changes a feature group. Its feature group is `obenseuer.moddingframework.dialogue`, the name you will see in the log for responses, branches and narration. Writing a conversation itself changes no game method.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only works safely on its main thread (see the [glossary](../../glossary.md#main-thread)).
- Writes are refused while a save is loading or being written. The calls return `null` or `false`, or a refusal reason, instead of changing anything.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the dialogue system could not be set up, `Dialogue.IsAvailable` is `false`. `Narrate`, `AddResponse` and `AddBranch` then refuse their calls, and `DialogueAuthoring.Add` gives `NotReady`.
