---
title: Common tasks
parent: NPC actions
grand_parent: People
nav_order: 1
---

# Common tasks

Move an NPC to a spot. The NPC may walk back toward what it was doing, because its routine keeps running.

```csharp
bool moved = NpcActions.Teleport(npcId, new Vector3(10f, 0f, 5f));
```

Make an NPC follow the player, then let it go. Use `false` for the last argument of `FollowPlayer`, so the NPC's own routine can take it back.

```csharp
if (NpcActions.FollowPlayer(npcId, false, false)) Log.Info("The NPC is following you.");
NpcActions.StopFollowing(npcId);
```

Start a conversation the game already has, by its title.

```csharp
bool started = NpcActions.StartDialogue(npcId, conversationTitle);
```

Send an NPC who is not in the scene to another scene. The scene name must be one the game has.

```csharp
bool sent = NpcActions.ChangeLevel(npcId, sceneName);
```

Hit an NPC the way a melee hit does, or kill it.

```csharp
NpcActions.Hit(npcId);
NpcActions.Kill(npcId);
```

In these snippets, `npcId` is the number from `NpcInfo.Id`, `conversationTitle` is the title of a conversation in the game, and `sceneName` is the name of a scene.
