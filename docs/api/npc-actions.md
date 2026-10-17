---
title: NPC actions
parent: People
grand_parent: API reference
has_children: true
nav_order: 5
---

# NPC actions

This page uses `KeyCode`, `Vector3` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

## What it is

This API makes a character you can see in the scene do something right now: jump to a new place, die, get hit, follow the player, say a line, start a conversation, look at something, or be sent to another scene. It only works on NPCs that are standing in the scene you are in. To find an NPC and read its state first, use the [NPCs page](npcs.md). Every call here takes the NPC's id from there.

## When to use it

- Move an NPC to a spot, for a cutscene or a quest.
- Make an NPC follow the player and stop again.
- Make an NPC say a line above its head, or start one of the game's conversations with it.
- Make an NPC look at the player or at an object.
- Hurt or kill an NPC from your mod.
- Send an NPC who is far away to another scene.

## Quick example

This mod waits for you to press F8. Then it finds the closest NPC in the scene, makes it look at the player for five seconds, and makes it say hello.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.npcgreet", "NPC Greeter", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NpcGreeter : FrameworkMod
{
    private ModKeybind greetKey;

    protected override void OnLoad()
    {
        greetKey = Mod.Key("Greet", "Greet the closest NPC", KeyCode.F8);
    }

    private void Update()
    {
        if (greetKey == null || UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (!greetKey.Down()) return;
        SceneNpcInfo closest = null;
        float best = float.MaxValue;
        foreach (SceneNpcInfo npc in Npcs.InScene())
        {
            if (npc.Id < 0 || !npc.Active) continue;
            float distance = Vector3.Distance(npc.Position, PlayerState.Position);
            if (distance < best)
            {
                best = distance;
                closest = npc;
            }
        }
        if (closest == null) return;
        NpcActions.LookAtPlayer(closest.Id, 5);
        if (!NpcActions.Say(closest.Id, "Hello there!")) Log.Info(closest.Name + " could not speak right now.");
    }
}
```

`Npcs.InScene()` lists the NPC objects in the scene, and `Id` is the number the actions take. Every action returns a `bool`: `true` when it was done, `false` when the game would not do it. See the [NPCs page](npcs.md) for `InScene` and the [keybinds guide](../guides/getting-started/keybinds.md) for `Mod.Key`.
