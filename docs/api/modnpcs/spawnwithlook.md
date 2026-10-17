---
title: SpawnWithLook
parent: Mod NPCs
grand_parent: People
nav_order: 4
---

# SpawnWithLook

`SpawnWithLook` adds a person like `Spawn` and gives them a look in the same call. The look comes from [Scene looks](../scenelooks.md), which reads a character's look out of the installed game files while the game runs, so your mod does not need to ship any art. It needs framework 0.11.0 or later.

## Things to know

- The arguments, the body rule and the reasons for a refusal are those of `Spawn`. The version without `bodyCharacterId` passes `0`.
- A refusal returns `0` with no reason, as `Spawn` does. Read the reason in `BepInEx\logs\obenseuer.moddingframework.modnpcs.log`.
- If the look can't be applied, the person is taken out again. The call then returns `0`, writes the reason to the log once, and leaves nothing in the scene. A saved record from `SpawnSaved` under the same id is left as it was.

## Limits

- A look that is not ready (`SceneLook.Ready` is `false`) fails the same way, so check `Ready` first when the look may not be installed.
- The default body is a different kind of skeleton, so some looks are refused. A look from a character such as Mr. Duck is built for a different skeleton and is very likely to need a conversion step. A look the framework can't put on the default body is refused. <span class="tech">(the default template is a Blender-style humanoid rig, and a `Bip001` body such as Mr. Duck's takes the retarget path)</span>
- The look is not saved. A person who must come back after a load uses `SpawnSaved`, and your mod applies the look again from `Events.WhenReady` with `SceneLooks.Apply`.
