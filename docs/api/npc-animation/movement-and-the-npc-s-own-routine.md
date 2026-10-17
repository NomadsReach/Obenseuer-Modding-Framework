---
title: Movement and the NPC's own routine
parent: NPC animation
grand_parent: People
nav_order: 5
---

# Movement and the NPC's own routine

The clip changes the pose and nothing else. The NPC's own schedule and behaviour keep walking it while the clip plays. To keep an NPC in place, hold it with `NpcMovement.SetActivityEnabled(modId, npcId, false)` and release it with `true` when the clip ends. See [NPC movement](../npc-movement.md). To also turn off its sight, talking and speech while the clip plays, take over the NPC with `NpcControl.Take(modId, npcId)`. See [NPC control](../npc-control.md).
