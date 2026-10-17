---
title: What you can't do (yet)
parent: NPC movement
grand_parent: People
nav_order: 4
---

# What you can't do (yet)

- **You can't pause the timetable of an NPC that is away.** There is no single switch that works for every NPC. `SetActivityEnabled` works only for an NPC in the loaded scene, because the in-scene switch needs the NPC's controller. The reasons are on the [NPC schedule API](../npc-schedules.md) page.
- **You can't walk an NPC between levels.** The game moves an NPC through a separate path and a level-change door, and the NPC is in transit while it runs. [NPC actions](../npc-actions.md) has `ChangeLevel`, which sets the level of an NPC that is not in the scene directly, and refuses an NPC that is already on a path.
- **You can't start a conversation here.** [NPC actions](../npc-actions.md) has `StartDialogue`, which starts an existing conversation with an NPC by its id. `Dialogue.Start(title, actor, conversant)` does the same for a conversant you already hold as a `Transform`.
