---
title: What each flag changes
parent: NPC control
grand_parent: People
nav_order: 3
---

# What each flag changes

- **`Sight` stops the NPC from noticing anything.** It does not see the player, does not notice other NPCs or targets, does not witness crimes, and does not turn its head toward the player. An NPC with no field of view is not affected.
  - Technical: the NPC's field of view component (`NPC_FOV`) is stopped from detecting anything while the flag is held.
- **`Talk` stops the player from starting a conversation with the NPC by pressing the use key.** The game shows its "Can't talk" message instead. Conversations the game forces still start, such as the NPC's own scripted reactions, the police and robber acts, card game prompts and the black screen dialogue.
- **`Barks` stops the game's own short comments from the NPC.** A bark is a quick line an NPC says by itself, such as when it is hit or when it sees a crime. The game's own comments about hits, guitar playing, crimes and random things all stop. While `Barks` is held, `NpcActions.Comment` ([NPC actions](../npc-actions.md)) refuses the NPC. `NpcActions.Say` is not stopped, because it does not go through the comment system.
  - Technical: it stops the NPC's `BarkStarter` (including its character bark starter) and every `DialogueSystemTrigger` on the NPC that has a bark conversation set. Barks that the game sends straight to the Dialogue System, without a bark starter, are not stopped. An example is the behaviour tree's bark task.
- **`Fade` stops the game from hiding the NPC when it is far away or out of view.** Normally the game hides an NPC that is far from the player, and when the camera looks away, to save work. With `Fade` held, the NPC stays shown. A far-away NPC that was already hidden is shown again on the game's next update, not at once.
  - Technical: it raises the `hideDistance` of the NPC's own `FadeGameObject` to one million metres. It also stops camera culling from hiding the NPC: a culled NPC is shown when you take it, and while the flag is held the game's culling callback does nothing for it. The framework reads the private `culled` flag through reflection. If the game does not have a `culled` bool field, the flag does nothing and the framework logs that once.
- **When you release `Fade`, the NPC may stay shown a little while.** The framework puts the distance back, but it does not write the "out of view" flag back, so the game's next change decides. A hidden-by-camera NPC that is out of view can stay shown until then.
- **`Movement` stops the NPC from walking and from starting new activities.** The NPC stays where it is. The game's schedule does not start or change what the NPC is doing while the flag is held. This is the same hold as `SetActivityEnabled` on the [NPC movement](../npc-movement.md) page. When you release, the NPC can move again.
  - Technical: it sets `richAI.canMove` to `false` and holds the NPC's activity, under the feature group `obenseuer.moddingframework.npccontrol`. The framework sets `canMove` to `false` again on every frame while the flag is held. On release, `canMove` comes back as it was when you took the NPC, and `true` if the game turned movement on during the take. A mod NPC that is in a routine order is not timed out while it is held.
- **`Movement` waits if the game has switched the NPC's activity off.** The hold is not placed while the game itself has the NPC's activity disabled, so releasing never turns on an activity the game turned off. The hold is placed on a later frame, once the game turns the activity back on. A hold is also not placed while the NPC's body is switched off, for example when the game has hidden it for being far away. The framework tries again each frame until it can.
  - Technical: the game flag is `activityDisabled`, and the controller is not live while it is hidden by the fade.
- **The framework only holds the NPC's activity for `Movement`.** Releasing drops only the hold that `Movement` placed. Another mod's hold on the same NPC is left alone.
