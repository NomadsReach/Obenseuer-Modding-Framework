---
title: The values
parent: World sky
grand_parent: Places and travel
nav_order: 3
---

# The values

`SkyOverride` is what you hand to `Set`. Every property is nullable: a property you leave `null` is not changed by your mod. `SkyState` is what you get from `Current`. It has the same names, and every value is read only.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SkyOverride</code> <span>the values your mod wants to change</span></p>
[`WorldSky.Set`](setting-and-clearing.md) takes one of these, with only the values you want to change filled in.

<div class="api-pair" markdown="1">

```csharp
public sealed class SkyOverride
{
    public bool? FogEnabled { get; set; }
    public FogMode? FogMode { get; set; }
    public Color? FogColor { get; set; }
    public float? FogDensity { get; set; }
    public float? FogStart { get; set; }
    public float? FogEnd { get; set; }
    public Color? AmbientColor { get; set; }
    public float? AmbientIntensity { get; set; }
    public float? SkyboxExposure { get; set; }
    public Color? SunColor { get; set; }
    public float? SunIntensity { get; set; }
    public float? StarsIntensity { get; set; }
    public float? CloudDensity { get; set; }
}
```

| Property | Type | What it means |
|---|---|---|
| `FogEnabled` | `bool?` | `true` turns fog on. `false` turns it off. |
| `FogMode` | `FogMode?` | How the fog thickens with distance (Unity's `FogMode`). |
| `FogColor` | `Color?` | The colour of the fog. |
| `FogDensity` | `float?` | How thick the fog is. 0 or more. |
| `FogStart` | `float?` | The distance where linear fog starts. |
| `FogEnd` | `float?` | The distance where linear fog is full. |
| `AmbientColor` | `Color?` | The colour of the light that fills shadows. |
| `AmbientIntensity` | `float?` | How strong that light is. 0 or more. |
| `SkyboxExposure` | `float?` | How bright the sky is. 0 or more. |
| `SunColor` | `Color?` | The colour of the sunlight. |
| `SunIntensity` | `float?` | How strong the sunlight is. 0 or more. |
| `StarsIntensity` | `float?` | How bright the stars are. 0 or more. |
| `CloudDensity` | `float?` | How thick the clouds are, from 0 to 1. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SkyState</code> <span>what the sky looks like now</span></p>
[`WorldSky.Current`](setting-and-clearing.md) gives you one of these, which is the sky as it looks at that moment.

<div class="api-pair" markdown="1">

```csharp
public sealed class SkyState
{
    public bool FogEnabled { get; }
    public FogMode FogMode { get; }
    public Color FogColor { get; }
    public float FogDensity { get; }
    public float FogStart { get; }
    public float FogEnd { get; }
    public Color AmbientColor { get; }
    public float AmbientIntensity { get; }
    public float? SkyboxExposure { get; }
    public Color? SunColor { get; }
    public float? SunIntensity { get; }
    public float? StarsIntensity { get; }
    public float? CloudDensity { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `FogEnabled` | `bool` | `true` when fog is on. |
| `FogMode` | `FogMode` | The fog type. |
| `FogColor` | `Color` | The fog colour. |
| `FogDensity` | `float` | The fog thickness. |
| `FogStart` | `float` | Where linear fog starts. |
| `FogEnd` | `float` | Where linear fog is full. |
| `AmbientColor` | `Color` | The colour of the light that fills shadows. |
| `AmbientIntensity` | `float` | How strong that light is. |
| `SkyboxExposure` | `float?` | `null` when the scene has no sky that carries it. |
| `SunColor` | `Color?` | `null` when the scene has no sun light. |
| `SunIntensity` | `float?` | `null` when the scene has no sun light. |
| `StarsIntensity` | `float?` | `null` when the scene has no sky that carries it. |
| `CloudDensity` | `float?` | `null` when the scene has no sky that carries it. |

</div>
</div>

## Things to know

- The fog and ambient values in `SkyState` always have a value. The others are `null` when the scene does not contain the object that holds them. A scene can use a different mix of sky systems than another, so a value you can read in one scene may be `null` in the next. <span class="tech">(the game's scenes use `SunDirection`, the Azure sky controllers, or both. A property whose object is missing is skipped, and `Current` reports it as `null`.)</span>
- `AmbientColor` sets every kind of ambient light to the same colour. <span class="tech">(flat, sky, equator and ground ambient colours all get the value.)</span>
- `SkyboxExposure` changes the sky's brightness whichever way the scene draws its sky. It sets the `_Exposure` property of the current skybox material when it has one, and the exposure of the Azure sky controller when one is in the scene.
- `SunColor` and `SunIntensity` change every sun light the sky systems drive. In the game the sun and the moon share one light, so this changes the moonlight too.

## Limits

- The Azure fog is not the same as the fog you change here. The game draws its own fog as a screen effect from the sky's shader values, and these properties change Unity's built-in fog only. So `FogEnabled` and the other fog values may look different from the Azure fog you see. <span class="tech">(Azure fog is driven by `globalFogDistance` and other shader values, not by `RenderSettings.fog`.)</span>
