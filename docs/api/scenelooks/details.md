---
title: Details
parent: Scene looks
grand_parent: Places and travel
nav_order: 8
---

# Details

This page uses `Mesh` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Everything not listed in this reference is internal and may change without notice.

- Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
- Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.scenelooks.log`.
- This page changes none of the game's methods. Its feature group is `obenseuer.moddingframework.scenelooks`, the name you will see in the log.

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../../glossary.md#main-thread)).
- It is safe to call at any time, and nothing throws. If the call cannot be done, you get an empty array, `null`, a failed handle or `false`, and the first failure is written to the log.

## If something fails to set up

- This page changes none of the game's methods, so there is no change that can fail to set up, and no `IsAvailable` to check.
- The only gate is the Unity version: on any version other than 6000.0.77f1 every call refuses, as described in [The rules](the-rules.md#the-rules).

## See also

- [Mod NPCs](../modnpcs.md): `ModNpcs.SpawnWithLook` spawns an NPC and applies a look in one call.
- [NPC appearance](../npcappearance.md): mesh, material and body shape changes on an NPC that already exists.
- [NPC animation](../npc-animation.md): play a clip over an NPC's own pose.
