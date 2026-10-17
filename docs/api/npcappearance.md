---
title: NPC appearance
parent: People
grand_parent: API reference
has_children: true
nav_order: 7
---

# NPC appearance

Status: experimental. Every member on this page is experimental.

## What it is

This API changes how a character who is standing in the scene looks. You can switch a part of the body or clothing on or off, swap the materials (the surface looks) of a part, set the body and face sliders of a character built from sliders, or set the blend shapes of a character with a baked look. Every change is tied to your mod id. `Restore` and `Clear` put the character back the way it was.

## When to use it

- Hide a character's hat or glasses, or give a part a different material.
- Change a character's height or face with the same sliders the player's look uses.
- Bend a character's face or body with blend shapes.
- Undo all your changes when your mod is done.

## Quick example

This mod waits until a save is loaded. Then it logs the body parts and the kind of body of the first character it finds in the scene.

```csharp
using ObenseuerCore;

[ModInfo("example.lookwatch", "Look Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class LookWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (SceneNpcInfo npc in Npcs.InScene())
            {
                if (npc.Id < 0 || !npc.Present) continue;
                Log.Info(npc.Name + " has a " + NpcAppearance.ShapeKind(npc.Id) + " body.");
                foreach (NpcMeshInfo mesh in NpcAppearance.Meshes(npc.Id))
                    Log.Info("  " + mesh.Mesh + (mesh.Active ? " (on)" : " (off)"));
                return;
            }
        });
    }
}
```

`Npcs.InScene()` lists the character objects in the scene, and `Id` is the character's number. See the [NPC page](npcs/loaded-scenes.md#npcs-in-the-loaded-scenes). `Events.WhenReady` runs your code once a save is loaded, or at once if one already is. See [Using events](../guides/getting-started/using-events.md).
