---
title: Common tasks
parent: Player
grand_parent: Player life and skills
nav_order: 1
---

# Common tasks

Each of these runs after a save has loaded, for example inside `Events.WhenReady(() => { ... })`.

Give the player half a level of Cooking. `Add` returns `false` when the game refuses, for example at the maximum level.

```csharp
Skills.Add(Skill.Cooking, 0.5f);
```

Check whether the player is drunk right now.

```csharp
bool drunk = Effects.IsActive(Effect.Drunk);
```

Hurt the player. The reason is the kind of damage, and the game's normal sounds and protection rules apply.

```csharp
PlayerState.Damage(10f, DamageReason.Fall);
```

Make the player unkillable for a while. Pass your mod's id, so your claim does not clash with another mod's (see [Owner](../../glossary.md#owner)), and release it when you are done.

```csharp
PlayerState.SetGodMode("example.playerbasics", true);
PlayerState.SetGodMode("example.playerbasics", false);
```

Check a subscription and list the level's named spawn points, then send the player to one.

```csharp
bool has = Subscriptions.IsActive("some-id");
foreach (string name in Travel.Entrypoints()) Log.Info(name);
Travel.TeleportTo("some-name");
```
