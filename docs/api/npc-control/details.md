---
title: Details
parent: NPC control
grand_parent: People
nav_order: 8
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.npccontrol.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts, so it can check your claims whenever the game looks, talks or hides an NPC. This page calls that a feature group: a named set of the framework's changes for one feature. Its feature group is `obenseuer.moddingframework.npccontrol`, the name you will see in the log.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- All the changes are made together, or none are. If any of them can't be made, the feature group is turned off, `NpcControl.IsAvailable` is `false` and `Take` returns `false`. `Release` and the reads keep working. <span class="tech">(`NpcControl.Init` subscribes to the scene event, and the framework calls `NpcControl.Tick` once per frame. It also changes five game methods: `FadeGameObject.Cull`, `NPC_FOV.CheckIfTargetIsDetected`, `InteractableTalk.CanTalk`, `BarkStarter.TryBark` and `DialogueSystemTrigger.Fire`. The five install together or not at all.)</span>
- If the game has no `culled` flag on its fade component, only `Fade` stops working. The framework logs it once, and the other flags keep working. <span class="tech">(`FadeGameObject.culled` is missing or is not a bool.)</span>
