---
title: Common tasks
parent: NPC animation
grand_parent: People
nav_order: 1
---

# Common tasks

Keep an NPC in place while a clip plays, and let it go afterwards. See [NPC movement](../npc-movement.md).

```csharp
NpcMovement.SetActivityEnabled(Mod.Id, npcId, false);
NpcAnimation.Play(Mod.Id, npcId, clip, true, 0.3f);

NpcAnimation.Stop(Mod.Id, npcId, 0.3f);
NpcMovement.SetActivityEnabled(Mod.Id, npcId, true);
```

Play a clip once, with no fade.

```csharp
NpcAnimation.Play(Mod.Id, npcId, clip, false, 0f);
```

Play a clip on a figure your mod owns, such as an extra.

```csharp
Animator animator = extraObject.GetComponentInChildren<Animator>();
if (animator != null) NpcAnimation.PlayOn(Mod.Id, animator, clip, true);
```

Stop every clip your mod has started.

```csharp
int stopped = NpcAnimation.StopAll(Mod.Id);
```

In these snippets, `npcId` is an NPC id, `clip` is your `AnimationClip`, and `extraObject` is the object of an extra from [Mod NPCs](../modnpcs.md).
