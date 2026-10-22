---
title: Common tasks
parent: Mod NPC routines
grand_parent: People
nav_order: 1
---

# Common tasks

This page uses `Vector3` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Every block on this page is a small mod of its own, and every call that touches an NPC runs inside `Events.WhenReady`. An order returns `false` when it can't start, so each example checks it and writes a warning. The [order limits](orders.md#limits) list the reasons.

Walk an NPC to a point, turn it to face a direction, and find out when it has arrived. The last argument is called once with `true` if the NPC arrived and `false` if not. The example adds the NPC first, because an order only works on a person your mod spawned.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.walk", "Walk", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class WalkKeeper : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            int sourceId = 0;
            foreach (CharacterInfo character in Characters.All())
                if (!character.IsPlayer) { sourceId = character.Id; break; }

            Vector3 spot = PlayerState.Position + Vector3.forward * 3f;
            int id = ModNpcs.Spawn(Mod.Id, "keeper", sourceId, "Sam", "Keeper", spot, 0f);
            if (id == 0)
            {
                Log.Warn("The keeper was refused, so there is nothing to walk.");
                return;
            }

            bool started = ModNpcRoutines.WalkTo(Mod.Id, "keeper", spot + Vector3.forward * 10f, 90f, false,
                arrived => Log.Info("Arrived: " + arrived));
            if (!started)
                Log.Warn("The walk did not start. See the order limits.");
        });
    }
}
```

Make an NPC wander around a spot, or patrol along a list of points. A new order replaces the old one, so the patrol below takes over from the wander.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.patrol", "Patrol", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class PatrolKeeper : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            int sourceId = 0;
            foreach (CharacterInfo character in Characters.All())
                if (!character.IsPlayer) { sourceId = character.Id; break; }

            Vector3 spot = PlayerState.Position + Vector3.forward * 3f;
            int id = ModNpcs.Spawn(Mod.Id, "keeper", sourceId, "Sam", "Keeper", spot, 0f);
            if (id == 0)
            {
                Log.Warn("The keeper was refused, so there is nothing to move.");
                return;
            }

            if (!ModNpcRoutines.Wander(Mod.Id, "keeper", spot, 5f))
                Log.Warn("The wander did not start. See the order limits.");

            Vector3[] points = { spot, spot + Vector3.forward * 10f, spot + Vector3.right * 10f };
            if (!ModNpcRoutines.Patrol(Mod.Id, "keeper", points, false))
                Log.Warn("The patrol did not start. See the order limits.");
        });
    }
}
```

Stop whatever the NPC is doing, and take its routine away (this also removes the NPC if the routine spawned it).

```csharp
using ObenseuerCore;

[ModInfo("example.retire", "Retire", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class RetireKeeper : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            // Stop ends the order the NPC is running now.
            if (!ModNpcRoutines.Stop(Mod.Id, "keeper"))
                Log.Warn("The keeper had no order to stop.");

            // Remove forgets the routine and takes its NPC away.
            if (!ModNpcRoutines.Remove(Mod.Id, "keeper"))
                Log.Warn("The keeper had no routine to remove.");
        });
    }
}
```

`Stop` reaches any NPC your mod spawned. `Remove` needs a routine, so it returns `false` for a person you added with `ModNpcs.Spawn`. Both return `false` when there was nothing to do.

Ask what the timetable says the NPC should be doing right now. `Current` gives `Away` when the id has no routine, or when the clock puts the NPC in no scene.

```csharp
using ObenseuerCore;

[ModInfo("example.check", "Check", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class CheckKeeper : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            NpcStepKind now = ModNpcRoutines.Current(Mod.Id, "keeper");
            if (now == NpcStepKind.Away)
                Log.Warn("The keeper is in no scene right now.");
            else
                Log.Info("The keeper is in step " + now + ".");
        });
    }
}
```
