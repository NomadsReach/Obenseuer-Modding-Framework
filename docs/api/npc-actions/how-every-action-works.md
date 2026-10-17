---
title: How every action works
parent: NPC actions
grand_parent: People
nav_order: 2
---

# How every action works

This is the full list of what the API gives you, one section for each kind of action. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

- **Every action returns `true` when it was done and `false` when it was not.** Nothing throws an error. An NPC id the game does not know, an NPC that is dead, or a bad value gives `false`.
- **Almost every action works only on an NPC that is standing in the scene you are in.** An NPC that is far away is not in the scene and has nothing to act on, so the action gives `false`. The one exception is `ChangeLevel`, which only works on an NPC that is not in the scene.
  - Technical: the NPC needs a loaded scene instance (`NPCInfo`) with an `NPCController` on an active object, and it must not be dead. An away NPC has no controller, so the calls do not touch the game's NPC records (`NPCDirector` data). `ChangeLevel` is the only call that sets the scene or position in that record.
- **Every action gives `false` while a save is loading or being written.** `Teleport` and `StartDialogue` also give `false` during a loading screen, the death screen, a waiting screen, a level change, an arrest, a sleep event, and while any conversation is running.
  - Technical: `Teleport` and `StartDialogue` check the framework's `SaveSlots.WorldBusy`, which covers all of those. The other live actions check only the save gate.
- **Most actions change the NPC in the scene only.** `FollowPlayer`, `StopFollowing` and `Kill` also change what the NPC's saved record says it is doing, and `Hit` can too when it finds a police or robber activity.
  - Technical: the game's controller writes the NPC's current activity into its saved state.
