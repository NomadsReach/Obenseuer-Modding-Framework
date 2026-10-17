---
title: World objects
parent: Objects and devices
grand_parent: API reference
has_children: true
nav_order: 19
---

# World objects

This page uses `Vector3` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Status: experimental.

## What it is

World objects are the things lying around in the game's scenes: items on the floor, packages, coins, furniture and other objects the game saves. This API lets your mod find them (by save id, by place in the scene, or by distance), tell whether the game saves one, store a small piece of your own text on one, and drop new items into the world.

## When to use it

- Find the saved objects near the player or near a point.
- Find an object by its save id or by its name path in a scene.
- Remember something about one object, for example that the player already looked at it, and have it survive a save.
- Drop an item on the floor so the player can pick it up.

## Quick example

This mod, once the game is ready, logs how many saved objects are within 10 metres of the player and drops an apple at the player's position.

```csharp
using System.Collections.Generic;
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.worldlook", "World Look", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class WorldLook : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            Vector3 here = PlayerState.Position;
            List<WorldObjectInfo> near = WorldObjects.Near(here, 10f);
            Log.Info(near.Count + " saved objects within 10 metres");
            if (Spawn.Item(10620, 1, here)) Log.Info("dropped an apple");
        });
    }
}
```

The number 10620 is the apple's item id. `Spawn.Item` returns `false` instead of throwing when the game can't place the item. See [Using events](../guides/getting-started/using-events.md) for more on `WhenReady`.
