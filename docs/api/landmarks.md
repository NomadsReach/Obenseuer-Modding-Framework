---
title: Landmarks
parent: Places and travel
grand_parent: API reference
has_children: true
nav_order: 7
---

# Landmarks

## What it is

The map in the game shows landmarks: places the player has discovered, each with an icon and a name. This API lets your mod read the game's landmarks, see which ones the player has, unlock one, and place one at a spot you choose. It can also create new landmarks of your own, and simple map markers, that stay on the map after the player saves and loads.

## When to use it

- Check whether the player has discovered a landmark.
- Unlock a landmark as a reward, with the game's own notification.
- Put a marker on the map for a place your mod adds, such as a camp or a quest spot.
- React when any landmark is added to the map.

## Quick example

This mod puts a red marker called Camp on the map once a save is loaded. It also writes a log line each time any landmark is added to the map.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.camp", "Camp Marker", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class CampMarker : FrameworkMod
{
    protected override void OnLoad()
    {
        Landmarks.Added += args => Log.Info("Landmark added: " + args.Title);
        Events.WhenReady(() =>
        {
            if (!Landmarks.Mark(Mod.Id, "camp", "Camp", new Vector3(10f, 0f, 10f)))
                Log.Info("The marker was not placed.");
        });
    }
}
```

`Mark` places a simple red dot on the map and gives back `true` when it is there afterwards. It gives back `false` when the map is not ready, so the example only logs a line then. `Added` is an event: the framework calls your code each time it happens. See [Using events](../guides/getting-started/using-events.md).
