---
title: The values
parent: World sky
grand_parent: Places and travel
nav_order: 3
---

# The values

`SkyOverride` is what you hand to `Set`. Every property is nullable: a property you leave `null` is not changed by your mod. `SkyState` is what you get from `Current`. It has the same names, and every value is read only.

```csharp
public sealed class SkyOverride    // the values your mod wants to change; null means leave it alone
{
    public bool? FogEnabled { get; set; }         // true turns fog on, false turns it off
    public FogMode? FogMode { get; set; }         // how the fog thickens with distance (Unity's FogMode)
    public Color? FogColor { get; set; }          // the colour of the fog
    public float? FogDensity { get; set; }        // how thick the fog is; 0 or more
    public float? FogStart { get; set; }          // distance where linear fog starts
    public float? FogEnd { get; set; }            // distance where linear fog is full
    public Color? AmbientColor { get; set; }      // the colour of the light that fills shadows
    public float? AmbientIntensity { get; set; }  // how strong that light is; 0 or more
    public float? SkyboxExposure { get; set; }    // how bright the sky is; 0 or more
    public Color? SunColor { get; set; }          // the colour of the sunlight
    public float? SunIntensity { get; set; }      // how strong the sunlight is; 0 or more
    public float? StarsIntensity { get; set; }    // how bright the stars are; 0 or more
    public float? CloudDensity { get; set; }      // how thick the clouds are, 0 to 1
}

public sealed class SkyState    // what the sky looks like now
{
    public bool FogEnabled { get; }           // true when fog is on
    public FogMode FogMode { get; }           // the fog type
    public Color FogColor { get; }            // the fog colour
    public float FogDensity { get; }          // the fog thickness
    public float FogStart { get; }            // where linear fog starts
    public float FogEnd { get; }              // where linear fog is full
    public Color AmbientColor { get; }        // the colour of the light that fills shadows
    public float AmbientIntensity { get; }    // how strong that light is
    public float? SkyboxExposure { get; }     // null when the scene has no sky that carries it
    public Color? SunColor { get; }           // null when the scene has no sun light
    public float? SunIntensity { get; }       // null when the scene has no sun light
    public float? StarsIntensity { get; }     // null when the scene has no sky that carries it
    public float? CloudDensity { get; }       // null when the scene has no sky that carries it
}
```

- **The fog and ambient values in `SkyState` always have a value.** The others are `null` when the scene does not contain the object that holds them. A scene can use a different mix of sky systems than another, so a value you can read in one scene may be `null` in the next.
  - Technical: the game's scenes use `SunDirection`, the Azure sky controllers, or both. A property whose object is missing is skipped, and `Current` reports it as `null`.
- **`AmbientColor` sets every kind of ambient light to the same colour.**
  - Technical: flat, sky, equator and ground ambient colours all get the value.
- **`SkyboxExposure` changes the sky's brightness whichever way the scene draws its sky.** It sets the `_Exposure` property of the current skybox material when it has one, and the exposure of the Azure sky controller when one is in the scene.
- **`SunColor` and `SunIntensity` change every sun light the sky systems drive.** In the game the sun and the moon share one light, so this changes the moonlight too.
- **The Azure fog is not the same as the fog you change here.** The game draws its own fog as a screen effect from the sky's shader values, and these properties change Unity's built-in fog only. So `FogEnabled` and the other fog values may look different from the Azure fog you see.
  - Technical: Azure fog is driven by `globalFogDistance` and other shader values, not by `RenderSettings.fog`.
