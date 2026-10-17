---
title: NPC movement
parent: People
grand_parent: API reference
has_children: true
nav_order: 9
---

# NPC movement

Status: experimental.

## What it is

Every NPC (a character in the game) follows a daily schedule. The schedule gives the NPC an activity at each time of day, such as walking to a place, standing at a spot or patrolling a route. This API lets your mod take over an NPC's movement for a while. You can walk it to a position, make it patrol or stand at a group of waypoints, or just switch its schedule off so it stays where it is. When you are done, `Clear` hands the NPC back to its normal schedule.

NPC ids on this page are the ones from the [NPCs](npcs.md) page (`NpcInfo.Id`, `Npcs.All()`). To choose an NPC and read its state, use that page.

## When to use it

- Walk an NPC to a place for a quest or a scripted scene.
- Make an NPC patrol a route or stand at a spot.
- Freeze an NPC where it is so the schedule does not pull it away.
- Stop whatever an NPC is doing right now.

## Quick example

This mod waits for a save to load. Then it walks the first living NPC it can to a spot 5 metres away, and gives the NPC back to its schedule after 20 seconds.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.npcwalk", "NPC Walk", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NpcWalk : FrameworkMod
{
    private int walkingNpc = -1;
    private float clearAt;

    protected override void OnLoad()
    {
        Events.WhenReady(StartWalk);
    }

    private void StartWalk()
    {
        foreach (NpcInfo npc in Npcs.All())
        {
            if (!npc.Alive) continue;
            Vector3 target = npc.Position + new Vector3(5f, 0f, 0f);
            if (!NpcMovement.MoveTo(Mod.Id, npc.Id, target)) continue;
            walkingNpc = npc.Id;
            clearAt = Time.time + 20f;
            Log.Info("Walking " + (npc.Name ?? "an NPC") + " to " + target);
            return;
        }
        Log.Info("No NPC could be moved.");
    }

    private void Update()
    {
        if (walkingNpc < 0 || Time.time < clearAt) return;
        NpcMovement.Clear(Mod.Id, walkingNpc);
        walkingNpc = -1;
    }
}
```

`MoveTo` returns `false` for an NPC that is not in the loaded scene, so the example tries each NPC until one works. `Mod.Id` is your mod's own id; it is how the framework knows the claim is yours. See [Using events](../guides/getting-started/using-events.md) for more on `WhenReady`.
