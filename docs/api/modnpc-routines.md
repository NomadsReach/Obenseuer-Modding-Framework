---
title: Mod NPC routines
parent: People
grand_parent: API reference
has_children: true
nav_order: 3
---

# Mod NPC routines

Status: experimental.

## What it is

A mod NPC is a character that your mod spawns into the game (see [Mod NPCs](modnpcs.md)). On its own it just stands where you put it. This API gives a mod NPC something to do. There are two parts:

- Orders are one-off commands: walk to a point, wander around a spot, patrol along some points, sit on a chair, sleep in a bed, or stop.
- Routines are a weekly timetable. Each step says which days and hours it covers, which scene the NPC is in, and what it does there. While a step is in effect, the framework puts the NPC in that scene, and when the clock moves on, the NPC leaves.

These calls work only on NPCs that your mod spawned. They do not work on the game's own NPCs. To read what a game NPC is doing, see [NPC schedules](npc-schedules.md).

## When to use it

- Make a shopkeeper who stands at the counter from 9 to 17 and goes home at night.
- Send an NPC to a point when something happens, and know when it has arrived.
- Have an NPC wander around a yard, patrol a corridor, sit on a bench or sleep in a bed.
- React when an NPC's timetable moves from one activity to the next.

## Quick example

This mod gives a new NPC a timetable: wander near the player's position every day from 8:00 to 18:00. It writes a log line whenever the NPC changes from one step to the next. The routine spawns the NPC for you, so there is no separate spawn call. `Set` returns `false` when it refuses, so the mod writes a warning and stops.

```csharp
using System;
using ObenseuerCore;
using UnityEngine;
using UnityEngine.SceneManagement;

[ModInfo("example.keeper", "Keeper", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class Keeper : FrameworkMod
{
    protected override void OnLoad()
    {
        // A subscription is a registration, so it belongs in OnLoad.
        ModNpcRoutines.StepChanged += args => Log.Info(args.NpcId + " went from " + args.From + " to " + args.To);

        // The routine needs a loaded scene, so it waits for a save.
        Events.WhenReady(() =>
        {
            int sourceId = 0;
            foreach (CharacterInfo character in Characters.All())
                if (!character.IsPlayer) { sourceId = character.Id; break; }

            string scene = SceneManager.GetActiveScene().name;
            NpcRoutine routine = new NpcRoutine()
                .Wander(NpcDays.Every, TimeSpan.FromHours(8), TimeSpan.FromHours(18), scene, PlayerState.Position, 5f);
            if (!ModNpcRoutines.Set(Mod.Id, "keeper", sourceId, "Sam", "Keeper", routine))
            {
                Log.Warn("The routine was refused. See the routines page for why.");
                return;
            }

            Log.Info("The keeper has a routine.");
        });
    }
}
```

`Mod.Id` is your mod's id. `"keeper"` is the id you choose for this NPC, and every later call uses the same pair of ids. `StepChanged` is an event: the framework calls your code each time it happens. `WhenReady` runs your code once a save is loaded (see [Using events](../guides/getting-started/using-events.md)). The [setting and removing](modnpc-routines/routines.md#setting-and-removing) notes list the reasons `Set` gives a plain `false`.
