---
title: Scene looks
parent: Places and travel
grand_parent: API reference
has_children: true
nav_order: 11
---

# Scene looks

Status: experimental. Every member on this page is experimental and may change.

## What it is

Some characters in the game, such as Mr. Duck in the scene `Interior Tenement Speakeasy`, have a look that the game keeps inside one of its built-in scenes: the body model, its materials, its skeleton and its textures. A "scene look" is that look, read straight out of the game's own files while the game runs. This API lets your mod put a game character's look on a mod NPC, or on an NPC that is already in the scene, without your mod shipping any art.

Nothing is extracted, converted or written to disk, and the scene the look lives in is never loaded. This is the first part of the framework that reads the game's installed Unity asset files. It is written for Unity 6000.0.77f1 only, and every call refuses on any other Unity version.

## When to use it

- Give a mod NPC the look of a character that already exists in the game.
- Change the look of an NPC that is already in the scene.
- List the characters and bodies in one of the game's scenes, to find the look you want.
- Take a look off a body again, and free what it used.

## Quick example

This mod lists every character look in the scene the player is in, and writes each one to the log. It runs once a save is loaded.

```csharp
using ObenseuerCore;
using UnityEngine.SceneManagement;

[ModInfo("example.scenelooklist", "Scene Look List", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class SceneLookList : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            string scene = SceneManager.GetActiveScene().name;
            foreach (SceneLookInfo look in SceneLooks.List(scene))
                Log.Info(look.Name + ": " + look.Renderers + " meshes, " + look.Bones + " bones, " + look.RigKind);
        });
    }
}
```

`List` gives you one entry for each character in the scene, as plain information. Nothing is loaded yet. See [Using events](../guides/getting-started/using-events.md) for `Events.WhenReady`.
