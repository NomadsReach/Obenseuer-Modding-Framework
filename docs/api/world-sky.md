---
title: WorldSky API
parent: API reference
nav_order: 101.3
---

# WorldSky API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.sky`.
Files: `src/Api/WorldSky.cs`, `src/Api/WorldSkyPatches.cs`, `src/Api/SkyApply.cs`, `src/Api/SkyChannel.cs`.

This area overrides fog, ambient light, skybox exposure, sun light, stars and cloud density, one set of values per mod. It also reads the current sky state. It does not change the time of day or the weather (see `docs/api/world.md`).

## If a hook cannot be installed

The framework installs one prefix on `AzureSkyRenderController.UpdateShaderUniforms()` and a prefix and a finalizer on each game writer (`UnderwaterEffect.EnableEffect` and `DisableEffect`, `SunDirection.Update`, `LightsController.Update` and `LightsController.StartChangeAmbientColor`), and clears overrides on a save load and on a Single scene load. It subscribes to `Application.onBeforeRender` only while an override is set or a value is still held. If the hook cannot be installed the owner is removed and `WorldSky.IsAvailable` becomes `false`.

## API

```csharp
public static bool IsAvailable { get; }
public static SkyState Current { get; }
public static bool Set(string modId, SkyOverride values);
public static bool Clear(string modId);
```

`SkyOverride` has these nullable properties. A property left `null` is not overridden by that mod.

```csharp
bool? FogEnabled; FogMode? FogMode; Color? FogColor; float? FogDensity; float? FogStart; float? FogEnd;
Color? AmbientColor; float? AmbientIntensity;
float? SkyboxExposure;
Color? SunColor; float? SunIntensity;
float? StarsIntensity; float? CloudDensity;
```

`SkyState` has the same names as read-only values. The fog and ambient values are always set. `SkyboxExposure`, `SunColor`, `SunIntensity`, `StarsIntensity` and `CloudDensity` are `null` when the scene has no object that carries them.

- `Set` returns `false` when `IsAvailable` is `false`. It throws `ArgumentException` for an invalid mod id, a `null` `values`, a value that is not finite, or a negative density, intensity or exposure. `CloudDensity` is clamped to 0 to 1. The values are copied, so changing the `SkyOverride` afterwards has no effect.
- A mod that calls `Set` again replaces its own earlier values. When another mod already overrides one of the same properties, the framework logs one warning naming both mods, and the newer call applies.
- `Clear` returns `true` only when that mod had an override. It throws `ArgumentException` for an invalid mod id.
- `Current` returns what the sky shows now, including any override. It returns `null` when reading fails.

## Example

```csharp
SkyOverride fog = new SkyOverride();
fog.FogEnabled = true;
fog.FogMode = FogMode.ExponentialSquared;
fog.FogColor = new Color(0.5f, 0.55f, 0.6f);
fog.FogDensity = 0.02f;
WorldSky.Set("com.example.mymod", fog);
SkyState now = WorldSky.Current;
WorldSky.Clear("com.example.mymod");
```

## Behaviour

- Several mods may override at once. For each property the newest mod that set it wins. When that mod clears, the value from the mod before it applies, and when no mod has the property the game's own value returns.
- The game rewrites these values itself: every frame for ambient light (`AzureEnvironmentController`), on a timer for the sun light, skybox and fog colour (`SunDirection`), and when the player goes under water for fog. The framework writes the override again after the game's writers, just before the frame is drawn (`Application.onBeforeRender`) and just before the sky shader values are pushed. Stars, clouds and exposure of the Azure sky are read in that push, so they use the prefix.
- The framework remembers the last value the game wrote for each property. Around each game writer it puts the remembered value back, lets the writer run, and reads the result as the new remembered value, so a game write that equals the override is not lost. On `Clear` it writes the remembered value back, so the sky returns to what the game would show. If a writer the framework does not wrap has written a newer value since the framework last wrote, the framework leaves it alone.
- `AmbientColor` sets the flat, sky, equator and ground ambient colours to the same colour.
- `SkyboxExposure` sets the `_Exposure` property of the current skybox material when it has one, and the exposure of the Azure sky controller when one is in the scene. Whichever the scene has is used.
- `SunColor` and `SunIntensity` set every directional sun light the sky systems drive. Sun and moon share one light in the game.
- Overrides drop on a save load and on a Single scene load. Set them again from `Events.SceneLoaded` or `Events.Loaded`. Fog and ambient values belong to the scene, so on a scene change they are not written back. A skybox material is written back.
- The scene is searched for sky objects at most once every 120 frames, and only while an override is active or a value is held. A sun light that is no longer found, a replaced Azure sky controller and a replaced skybox material get their remembered values written back.
- `Set` and `Clear` are not gated on `SaveData.Busy`, so the re-apply from `Events.SceneLoaded` above works. An override is session state and is never saved.

## Not exposed

- The Azure weather profile properties by name (fog distance, scattering, sky colours, sun direction). The profile is an asset whose property names and order are not in the code.
- Sun direction. The Azure light direction is computed from the time of day each physics step, and `SunDirection` computes its own angle, so a mod value would be overwritten. Change the time of day with `WorldClock`.
- `DisableSky` and `EnableSky` (`TimeOfDayAzure`). They swap the whole weather profile, and a profile with fewer entries than the override object throws in the game.

## Ceilings and risks

- Which sky systems the game scenes use (`SunDirection`, the Azure controllers, or both). Properties whose object is missing are skipped, and `Current` reports them as `null`.
- The Azure fog is a screen effect driven by its own sky shader values (`globalFogDistance` and others), not by `RenderSettings.fog`. `FogEnabled` and the other fog values change the engine fog only.
- A writer the framework does not wrap (a mod, or a game script not covered here) that writes exactly the value the framework had written cannot be told apart from no write, and the earlier remembered value is kept.

## Save loads

Nothing is written to the save.
