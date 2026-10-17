---
title: Common tasks
parent: NPC movement
grand_parent: People
nav_order: 1
---

# Common tasks

Freeze an NPC where it is, then let the schedule go on again:

```csharp
NpcMovement.SetActivityEnabled("example.npcwalk", 5, false);
NpcMovement.SetActivityEnabled("example.npcwalk", 5, true);
```

Stop whatever the NPC is doing right now. The schedule takes over again at its next check:

```csharp
NpcMovement.ForceStopActivity("example.npcwalk", 5);
```

Make an NPC patrol a group of waypoints, if your mod knows the group's id. A patrol and a stand both refuse while any mod holds the NPC, so release the hold first:

```csharp
if (!NpcMovement.Patrol("example.npcwalk", 5, "my-node-group-guid")) Log.Warn("The patrol did not start.");
```
