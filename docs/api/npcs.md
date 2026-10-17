---
title: NPCs
parent: People
grand_parent: API reference
has_children: true
nav_order: 11
---

# NPCs

Status: experimental.

## What it is

This API lists NPCs and their spawns, despawns and deaths, and reads [relationships](relationships.md) and the [phone book](phones.md). It also finds and hurts [cats, chicks, chickens, pigs and rats](animals.md). The osrat is [on the weapons page](weapons/osrat-signal-detector.md#osrat-signal-detector), not an animal.

## When to use it

- Log a named NPC's death.
- Count NPCs or list the living in a scene.

## Quick example

Logs NPC deaths, and counts NPCs once a save loads.

```csharp
using ObenseuerCore;

[ModInfo("example.npcwatch", "NPC Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NpcWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Npcs.Died += npc => Log.Info((npc.DisplayName ?? npc.Name ?? "An NPC") + " died in " + npc.CurrentScene);
        Events.WhenReady(() =>
        {
            int alive = 0;
            foreach (NpcInfo npc in Npcs.All())
                if (npc.Alive) alive++;
            Log.Info("The game knows " + Npcs.All().Count + " NPCs, " + alive + " alive.");
        });
    }
}
```
