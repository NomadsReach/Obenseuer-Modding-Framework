---
title: What you can't do (yet)
parent: NPC actions
grand_parent: People
nav_order: 8
---

# What you can't do (yet)

- **You can't damage or heal an NPC.** The game has no damage or health that NPCs use, so a health change would do nothing and would only look like it worked. `Hit` is the closest thing, and it does not change health.
  - Technical: `NPCController` has no damage, health or `IDamageReceiver` member. The only health value is the public field `NPCStats.Health`, and no game code reads or writes it. Its `Activities_Health` triggers have no callers.
- **You can't listen for when an NPC says a line.** There is no event for it. It would need a change to the game's bark code that would run on every bark of every NPC. The speaker would also be unknown for lines said by world logic, which have no NPC.
  - Technical: a `Barked` event would need a Harmony postfix on `SubtitleBarkUI.Bark`, with its own install and turn-off path. A world `logic_bark` speaker has no NPC, so its `NpcId` would be 0.
- **You can't use the extra look calls the game has.** The look calls here are the four listed above only. Looking at a named target, and turning off the look at the player, are not offered.
  - Technical: the game's `LookAtNamedTarget` and `DisableLookAtPlayer` are not offered by the framework.
- **You can't move an NPC that is not in the scene by a step at a time.** Those NPCs move along the game's own routes between scenes and have nothing to teleport. `ChangeLevel` sets the scene directly and does not walk the NPC.
