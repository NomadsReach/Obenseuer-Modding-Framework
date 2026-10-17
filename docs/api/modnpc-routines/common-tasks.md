---
title: Common tasks
parent: Mod NPC routines
grand_parent: People
nav_order: 1
---

# Common tasks

This page uses `Vector3` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Walk an NPC to a point, turn it to face a direction, and find out when it has arrived. The last argument is called once with `true` if the NPC arrived and `false` if not.

```csharp
bool started = ModNpcRoutines.WalkTo(Mod.Id, "keeper", new Vector3(0f, 0f, 0f), 90f, false, arrived => Log.Info("Arrived: " + arrived));
```

Make an NPC wander around a spot, or patrol along a list of points.

```csharp
ModNpcRoutines.Wander(Mod.Id, "keeper", new Vector3(0f, 0f, 0f), 5f);
ModNpcRoutines.Patrol(Mod.Id, "keeper", new[] { new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f) }, false);
```

Stop whatever the NPC is doing, and take its routine away (this also removes the NPC if the routine spawned it).

```csharp
ModNpcRoutines.Stop(Mod.Id, "keeper");
ModNpcRoutines.Remove(Mod.Id, "keeper");
```

Ask what the timetable says the NPC should be doing right now.

```csharp
NpcStepKind now = ModNpcRoutines.Current(Mod.Id, "keeper");
```
