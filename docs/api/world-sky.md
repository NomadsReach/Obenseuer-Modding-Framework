---
title: World sky
parent: Places and travel
grand_parent: API reference
has_children: true
nav_order: 19
---

# World sky

## What it is

The sky and the light around the player: fog, the ambient light, how bright the sky is, the sun, the stars and the clouds. This API lets your mod read how the sky looks right now, and change those looks on top of what the game does. Each mod keeps its own set of values, so two mods do not overwrite each other. It does not change the time of day or the weather: see [World](world.md) for those.

## When to use it

- Make a heavy fog when the player enters a special area.
- Darken or brighten the scene for a story moment.
- Turn the stars up, or the clouds down.
- Read the current fog or sun values, for example to match your own lights to them.

## Quick example

This mod turns on a grey fog as soon as the game is ready, and removes it again after 10 seconds.

```csharp
using System.Collections;
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.fog", "Fog", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class FogMod : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() => StartCoroutine(FogForTenSeconds()));
    }

    private IEnumerator FogForTenSeconds()
    {
        SkyOverride fog = new SkyOverride();
        fog.FogEnabled = true;
        fog.FogMode = FogMode.ExponentialSquared;
        fog.FogColor = new Color(0.5f, 0.55f, 0.6f);
        fog.FogDensity = 0.02f;
        WorldSky.Set(Mod.Id, fog);
        yield return new WaitForSeconds(10f);
        WorldSky.Clear(Mod.Id);
    }
}
```

`SkyOverride` holds the values you want to change, and every one you leave out stays as the game has it. `Mod.Id` is the id you gave in `[ModInfo]`, so the framework knows the values are yours. See [Using events](../guides/getting-started/using-events.md) for more on `WhenReady`.
