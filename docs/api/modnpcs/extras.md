---
title: Extras
parent: Mod NPCs
grand_parent: People
nav_order: 8
---

# Extras

An extra is a light figure for a background crowd. It stands and watches. It has no behaviour of an NPC: it does not talk, does not make barks (the short remarks NPCs call out) and has no save record.

## Things to know

- `SpawnExtra` builds an extra from a source and a body, as `Spawn` does. It returns the figure's object, or `null` when it can't.
- `DespawnExtra` removes one extra and returns `true` when the extra existed. `ClearExtras` removes all of your extras and returns how many it removed. `ExtraObject` gives you the figure's object, or `null` when there is none.
- `DespawnExtra` and `ClearExtras` don't check whether the game is loading or saving, because an extra has no record to change.
- An extra stands exactly where you put it. It is dropped onto the ground below the spot, the same way as for `Spawn`.
- An extra stays until `DespawnExtra`, `ClearExtras` or a scene load that replaces the scene, which removes every extra. It is never saved. <span class="tech">(a Single scene load removes every extra; the framework checks the scene name this way for its own saved NPCs)</span>
- To add it again after a load, call `SpawnExtra` from `Events.WhenReady`, and check the scene name inside the action.
- An extra has no scripts that make it act like a person. It has no NPC component, no behaviour, no dialogue, no barks, no position record and no save id. Only the scripts that keep it dressed stay. <span class="tech">(every `MonoBehaviour` on the copy is removed except `Appearance` and `CharacterAppearance`)</span>
- An extra keeps its visible parts and stays where it is placed. The body's animator, renderers, colliders and rigidbodies stay. <span class="tech">(the `Animator`, renderers, colliders, rigidbodies and transforms of the body prefab stay; each rigidbody is made kinematic, each `NavMeshAgent` is disabled, each `Animator` has animation events turned off because the removed scripts were their receivers, and each `Animator` has root motion turned off)</span>
- An extra is not in the NPC lists. The copy is not in `Npcs`, `NpcMovement`, `NpcActions`, `NpcAppearance`, `NpcControl` or `ModNpcs.List`, because those read the NPC list. But `Characters.All` and the game's own character list do show it, as they show a mod NPC from `Spawn`. <span class="tech">(its `Character` is put in the game's character database while the extra lives, so no mod NPC can be given its character number)</span>
- An extra has no look of its own. It keeps none between spawns. Its look is the source's look on the body, with the same limit as for a person: if the body has none of the source's parts, the copy shows the body's own look.
- An extra stands still, and you can make it play an idle animation. Get its animator with `ExtraObject(modId, extraId).GetComponentInChildren<Animator>()` and use `NpcAnimation.PlayOn` and `NpcAnimation.StopOn`. See [NPC animation](../npc-animation.md).
- `NpcAnimation.Play` takes an NPC number and does not reach an extra. `NpcAnimation.StopAll` stops an extra's clip too.

## Limits

- `SpawnExtra` returns `null` for the same reasons as `Spawn`: a save or load in progress, no scene with people in it, an unknown source, a body with no model, no ground under the spot, or the same `modId` and `extraId` already added.
- One mod can have up to 64 extras, and all mods together up to 256. Going over either limit returns `null`.
- A bad `extraId`, bad numbers for `position` or `yaw`, or a bad `modId` throws `ArgumentException`. The `extraId` must be 1 to 64 characters.
- The framework does not move an extra aside for other people or extras, so a tight crowd can overlap.
- Don't call `SpawnExtra` straight from `Events.SceneLoaded` or `Events.Loaded`. While the level is changing, the game can still be busy loading, and `SpawnExtra` returns `null` then.
- The figure can't be pushed or walk off, and it does not play the game's built-in animation events.
