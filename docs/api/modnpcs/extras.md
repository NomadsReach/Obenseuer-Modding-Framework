---
title: Extras
parent: Mod NPCs
grand_parent: People
nav_order: 8
---

# Extras

An extra is a light figure for a background crowd. It stands and watches. It has no behaviour of an NPC: it does not talk, does not make barks (the short remarks NPCs call out) and has no save record.

- **`SpawnExtra` builds an extra from a source and a body, as `Spawn` does.** It returns the figure's object, or `null` when it can't. The reasons are the same as for `Spawn`: a save or load in progress, no scene with people in it, an unknown source, a body with no model, no ground under the spot, or the same `modId` and `extraId` already added. One mod can have up to 64 extras, and all mods together up to 256. Going over either limit returns `null`.
- **A bad `extraId`, bad numbers for `position` or `yaw`, or a bad `modId` throws `ArgumentException`.** The `extraId` must be 1 to 64 characters.
- **`DespawnExtra` removes one extra, `ClearExtras` removes all of your extras, and `ExtraObject` gives you the figure's object.** `DespawnExtra` returns `true` when the extra existed. `ClearExtras` returns how many it removed. `ExtraObject` returns `null` when there is none. `DespawnExtra` and `ClearExtras` don't check whether the game is loading or saving, because an extra has no record to change.
- **An extra stands exactly where you put it.** It is dropped onto the ground below the spot, the same way as for `Spawn`. The framework does not move it aside for other people or extras, so a tight crowd can overlap.
- **Extras last only for the session.** An extra stays until `DespawnExtra`, `ClearExtras` or a scene load that replaces the scene, which removes every extra. It is never saved. To add it again after a load, call `SpawnExtra` from `Events.WhenReady`, and check the scene name inside the action. Don't call it straight from `Events.SceneLoaded` or `Events.Loaded`: while the level is changing, the game can still be busy loading, and `SpawnExtra` returns `null` then.
  - Technical: a Single scene load removes every extra. The framework checks the scene name this way for its own saved NPCs.
- **An extra has no scripts that make it act like a person.** It has no NPC component, no behaviour, no dialogue, no barks, no position record and no save id. Only the scripts that keep it dressed stay.
  - Technical: every `MonoBehaviour` on the copy is removed except `Appearance` and `CharacterAppearance`.
- **An extra keeps its visible parts and stays where it is placed.** The body's animator, renderers, colliders and rigidbodies stay. The figure can't be pushed or walk off, and it does not play the game's built-in animation events.
  - Technical: the `Animator`, renderers, colliders, rigidbodies and transforms of the body prefab stay. Each rigidbody is made kinematic, each `NavMeshAgent` is disabled, each `Animator` has animation events turned off because the removed scripts were their receivers, and each `Animator` has root motion turned off.
- **An extra is not in the NPC lists.** The copy is not in `Npcs`, `NpcMovement`, `NpcActions`, `NpcAppearance`, `NpcControl` or `ModNpcs.List`, because those read the NPC list. But `Characters.All` and the game's own character list do show it, as they show a mod NPC from `Spawn`.
  - Technical: its `Character` is put in the game's character database while the extra lives, so no mod NPC can be given its character number.
- **An extra has no look of its own.** It keeps none between spawns. Its look is the source's look on the body, with the same limit as for a person: if the body has none of the source's parts, the copy shows the body's own look.
- **An extra stands still, and you can make it play an idle animation.** Get its animator with `ExtraObject(modId, extraId).GetComponentInChildren<Animator>()` and use `NpcAnimation.PlayOn` and `NpcAnimation.StopOn`. See [NPC animation](../npc-animation.md). `NpcAnimation.Play` takes an NPC number and does not reach an extra. `NpcAnimation.StopAll` stops an extra's clip too.
