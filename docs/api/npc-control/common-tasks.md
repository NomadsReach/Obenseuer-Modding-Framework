---
title: Common tasks
parent: NPC control
grand_parent: People
nav_order: 1
---

# Common tasks

Hold an NPC still and stop it from fading out while your mod plays a scene, then hand it back:

```csharp
void StartScene(string modId, int npcId)
{
    NpcControlFlags flags = NpcControlFlags.Sight | NpcControlFlags.Talk | NpcControlFlags.Fade | NpcControlFlags.Movement;
    if (!NpcControl.Take(modId, npcId, flags)) return;
}

void EndScene(string modId, int npcId)
{
    NpcControl.Release(modId, npcId);
}
```

Say a line while the NPC is held. `Say` is on the [NPC actions](../npc-actions.md) page. It needs the NPC's body to be switched on, so check the result and try again later if it returned `false`:

```csharp
void Speak(int npcId)
{
    if (!NpcActions.Say(npcId, "Wait here.")) return;
}
```

Find out who holds an NPC, and give back everything your mod holds:

```csharp
string holder = NpcControl.TakenBy(5);
if (holder != null) Log.Info("NPC 5 is held by " + holder);
int released = NpcControl.ReleaseAll("example.npchold");
Log.Info("Released " + released + " NPCs.");
```
