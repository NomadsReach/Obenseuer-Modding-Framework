---
title: Mod NPCs
parent: People
grand_parent: API reference
has_children: true
nav_order: 4
---

# Mod NPCs

This page uses `Vector3` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Status: experimental. Every member on this page is experimental and may change.

## What it is

A mod NPC is a new person your mod adds to the game. The game has no way to add a person from nothing, so the framework builds a copy of one that already exists. You pick a person from the game to copy (the source), pick which 3D body to use (the body), and give the copy a new name and a place to stand. This page covers adding and removing those people, keeping them across a save and load, changing how they look, and adding extras, which are simple background figures for a crowd. Making a mod NPC walk, wander or follow a timetable is on the [mod NPC routines](modnpc-routines.md) page.

## When to use it

- Put a new shopkeeper, guard or visitor into a place in the game.
- Keep that person in the game after the player saves and loads.
- Fill a room with background figures that stand and watch.

## Quick example

This mod waits until a save is loaded. It then adds a person named Gus Greeter three metres in front of the player, built from the first person in the game's character list who is not the player. It writes the new person's number to the log.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.greeter", "Greeter", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class Greeter : FrameworkMod
{
    protected override void OnLoad()
    {
        // Adding a person touches the world, so it waits for a loaded save.
        Events.WhenReady(() =>
        {
            int sourceId = 0;
            foreach (CharacterInfo character in Characters.All())
                if (!character.IsPlayer) { sourceId = character.Id; break; }

            Vector3 spot = PlayerState.Position + Vector3.forward * 3f;
            int id = ModNpcs.Spawn(Mod.Id, "greeter", sourceId, "Gus", "Greeter", spot, 0f);
            if (id == 0)
            {
                Log.Warn("The greeter was refused. See the spawn limits.");
                return;
            }

            Log.Info("Spawned the greeter as character " + id);
        });
    }
}
```

`WhenReady` waits until a save is loaded before it runs your code. See [Using events](../guides/getting-started/using-events.md) for more on it. `Spawn` returns `0` when it refuses, and the [spawn limits](modnpcs/spawn.md#limits) list the reasons. This person is there until the scene changes, so use [SpawnSaved](modnpcs/spawnsaved.md) for a person who must come back after a load.
