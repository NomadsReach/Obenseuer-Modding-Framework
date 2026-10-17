---
title: NPC control
parent: People
grand_parent: API reference
has_children: true
nav_order: 8
---

# NPC control

Status: experimental.

## What it is

Normally an NPC (a character in the game) looks around, talks, comments on things, walks about and fades out when you are far away, all by itself. This API lets your mod take an NPC for a while and switch some of that off, for example so the NPC stands still and ignores the player while your mod runs a scene. Taking an NPC means your mod holds a claim on it. Each part you switch off is a flag, and `Release` puts everything back.

NPC ids on this page are the ones from the [NPCs](npcs.md) page (`NpcInfo.Id`, `Npcs.All()`). For an NPC your own mod added, the id is `ModNpcs.CharacterIdOf(modId, npcId)` (see [Mod NPCs](modnpcs.md)).

## When to use it

- Make an NPC stand still, stop seeing the player and stop talking while your mod plays a scene.
- Stop the game's own comments from an NPC so they do not talk over yours.
- Keep an NPC from fading out of the world when the player walks away.
- Hand the NPC back to the game afterwards.

## Quick example

This mod waits for a save to load. Then it takes the first living NPC it can in the scene, stops that NPC from seeing the player or talking for 10 seconds, and gives it back.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.npchold", "NPC Hold", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NpcHold : FrameworkMod
{
    private int heldNpc = -1;
    private float releaseAt;

    protected override void OnLoad()
    {
        Events.WhenReady(HoldFirstNpc);
    }

    private void HoldFirstNpc()
    {
        foreach (NpcInfo npc in Npcs.All())
        {
            if (!npc.Alive) continue;
            if (!NpcControl.Take(Mod.Id, npc.Id, NpcControlFlags.Sight | NpcControlFlags.Talk)) continue;
            heldNpc = npc.Id;
            releaseAt = Time.time + 10f;
            Log.Info("Holding " + (npc.Name ?? "an NPC") + " for 10 seconds.");
            return;
        }
        Log.Info("No NPC could be taken.");
    }

    private void Update()
    {
        if (heldNpc < 0 || Time.time < releaseAt) return;
        NpcControl.Release(Mod.Id, heldNpc);
        heldNpc = -1;
    }
}
```

`Take` returns `false` for an NPC that is not in the loaded scene, so the example tries each NPC until one works. `Mod.Id` is your mod's own id; it is how the framework knows the claim is yours. See [Using events](../guides/getting-started/using-events.md) for more on `WhenReady`.
