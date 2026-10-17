---
title: WorldWeather
parent: World
grand_parent: Places and travel
nav_order: 2
---

# WorldWeather

This is the full list of what the API gives you, one section for each part. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

`WorldWeather` reads the weather and changes it. The game has a list of weather profiles, called the global weather. Each profile is a named weather with an index counted from `0`. `NameOf` and `IndexOf` turn one into the other.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WorldWeather</code> <span>read and set the global weather</span></p>
Use these to read the weather now, `Set` to change it, and `WeatherChanged` to hear when it changes.

<div class="api-pair" markdown="1">

```csharp
public static partial class WorldWeather
{
    public static event Action<WeatherChangedArgs> WeatherChanged;

    public static bool IsAvailable { get; }

    public static int Count { get; }
    public static int CurrentIndex { get; }
    public static string CurrentName { get; }
    public static float Temperature { get; }
    public static float RainIntensity { get; }
    public static bool IsRaining { get; }
    public static float SnowIntensity { get; }
    public static bool IsSnowing { get; }
    public static float WindSpeed { get; }
    public static bool IsThunderActive { get; }

    public static string NameOf(int index);
    public static int IndexOf(string name);
    public static bool Set(int index, float transitionSeconds = 0f);
    public static bool SetThunder(bool active);
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the weather change event could not be set up. |
| `Count` | `int` | How many global weather profiles there are. |
| `CurrentIndex` | `int` | The index of the current weather. `-1` when there is no weather system or the default is active. |
| `CurrentName` | `string` | The name of the current weather. `null` when not ready. |
| `Temperature` | `float` | The temperature the game uses. `0` when not ready. |
| `RainIntensity` | `float` | How hard it rains right now: the average of light, medium and heavy rain. |
| `IsRaining` | `bool` | `true` when it is raining. |
| `SnowIntensity` | `float` | How hard it snows right now. |
| `IsSnowing` | `bool` | `true` when it is snowing. |
| `WindSpeed` | `float` | How strong the wind is right now. |
| `IsThunderActive` | `bool` | `true` when thunder is on. |

| Method | Returns | What it does |
|---|---|---|
| `NameOf(index)` | `string` | The name of a weather. `null` when the index is out of range. |
| `IndexOf(name)` | `int` | The index of a weather by name. `-1` when not found. |
| `Set(index, transitionSeconds)` | `bool` | Changes the global weather. Returns `true` when it worked. |
| `SetThunder(active)` | `bool` | Turns thunder on or off. Returns `true` also when it already was that way. |

| Event | Gives you | When it fires |
|---|---|---|
| `WeatherChanged` | `WeatherChangedArgs` | The weather the game is heading for has changed. `args.Index` is its index, `-1` when it is the default. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WeatherChangedArgs</code> <span>what WeatherChanged tells you</span></p>
`WeatherChanged` gives you this when the weather changes. `Index` is the index of the weather it changed to.

<div class="api-pair" markdown="1">

```csharp
public sealed class WeatherChangedArgs
{
    public int Index { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Index` | `int` | The index of the weather the game is heading for. `-1` for the default. |

</div>
</div>

## Things to know

- The weather values give plain defaults when the game has no weather system. That is on the main menu or while a save loads. `Count` is `0`, `CurrentIndex` is `-1`, `CurrentName` is `null`, and the numbers are `0`.
- `Set(index, transitionSeconds)` changes the global weather, and the sky blends over `transitionSeconds`. The game's own timer for the next random change is not reset, so the weather can change again later. <span class="tech">(It calls the game's `WeatherManager.SetWeather(index, true, transitionSeconds)`.)</span>
- `WeatherChanged` fires after the game has changed the weather it is heading for. That includes changes made by the game's random timer, by your `Set`, by a save loading, and by any other game call. It does not fire for each frame of the blend. <span class="tech">(It is raised after `WeatherManager.ChangeWeather`.)</span>
- `SetThunder` turns thunder on or off. No event is raised. <span class="tech">(It calls `WeatherManager.StartThunder` and `StopThunder`.)</span>
- Forced thunder can stay in a save. A save made while thunder is forced can keep it. It lasts until the next `Set` of the weather, which sets thunder again from the profile's heavy rain and the game's thunder chance. <span class="tech">(The game's field carries `[fsProperty]`.)</span>
- `IsAvailable` tells you whether `WeatherChanged` works. It is `false` when the framework could not set up the event, or when its feature group was turned off. Reads are not affected.

## Limits

- `Set` throws an `ArgumentException` for a bad number. That is a negative index, or a transition that is negative, `NaN` or infinite. It does this even when no weather system is ready. Once a weather system is ready, it also throws for an index outside `0..Count-1`.
- `Set` returns `false` for other problems. That is when no weather system is ready, when the profile is missing, and while a save is loading or being written.
- If the game stumbles during `Set`, you may still get `true`. When the game call throws but the weather you asked for is now active, `Set` raises `WeatherChanged` itself and returns `true`. Otherwise it returns `false` and nothing is raised. The failure is logged. <span class="tech">(The game retargets the weather before the exception can happen, and the framework's change to `ChangeWeather` does not run after a throw.)</span>
- Thunder alone does not always show lightning. Lightning flashes only while the sky is not overridden and no weather change is blending. `SetThunder(true)` alone shows no lightning in those states.
- Fog is not available. The game has no fog setting to read. <span class="tech">(`AzureFogScattering` is a visual component only.)</span>

## Weather zones

A weather zone is an area in a scene with its own local weather. While the player is in or near a zone, the game blends the zone's weather over the global weather. The distance over which the zone fades in is its blend distance. A scene can have no zones at all. Then `Zones()` is an empty list.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WorldWeather</code> <span>weather zones</span></p>
Use `SetZone` to give one weather zone its own weather, and `ClearZones` to remove your mod's zones.

<div class="api-pair" markdown="1">

```csharp
public static partial class WorldWeather
{
    public const float MaxZoneBlendDistance = 10000f;

    public static List<WeatherZoneInfo> Zones();
    public static bool SetZone(string modId, string zoneId, int weatherIndex, float blendDistance);
    public static bool ClearZones(string modId);
}
```

| Property | Type | What it means |
|---|---|---|
| `MaxZoneBlendDistance` | `float` | The largest blend distance you can set, in metres. |

| Method | Returns | What it does |
|---|---|---|
| `Zones()` | `List<WeatherZoneInfo>` | The weather zones the game has in this scene. |
| `SetZone(modId, zoneId, weatherIndex, blendDistance)` | `bool` | Makes a zone use one of the global weathers. |
| `ClearZones(modId)` | `bool` | Takes back every zone change your mod made. Returns `true` when it had made one. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WeatherZoneInfo</code> <span>a snapshot of one weather zone</span></p>
`Zones()` gives you one of these for each weather zone. `Overridden` says whether a mod has changed the zone with `SetZone`. <span class="tech">(It reads the game's `AzureWeatherZone`.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class WeatherZoneInfo
{
    public string Id { get; }
    public bool Active { get; }
    public float BlendDistance { get; }
    public string ProfileName { get; }
    public int WeatherIndex { get; }
    public bool Overridden { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The zone's id in the scene. Use it with `SetZone`. |
| `Active` | `bool` | `true` when the zone is switched on and can take effect. |
| `BlendDistance` | `float` | The distance in metres over which the zone fades in. |
| `ProfileName` | `string` | The name of the weather the zone applies now. `null` when none. |
| `WeatherIndex` | `int` | The index of the global weather with that same profile. `-1` when it is not one. |
| `Overridden` | `bool` | `true` while a mod's `SetZone` is in force. |

</div>
</div>

### Things to know

- `Zones()` lists the zones the game has registered, in the game's order. The game's own sky-override zone is left out, because the game rewrites it whenever the player enters or leaves one of the places that use it. Zones register about 0.1 seconds after a scene starts, so wait a moment after the scene load before you list them. <span class="tech">(The zones are `AzureWeatherZone` components. `Active` is true when the zone's collider is enabled and its object is active. The left-out zone is the one used by scenes with an overridden sky and by the shop controller.)</span>
- `SetZone` makes a zone use one of the global weathers. `weatherIndex` is the same index as `Set` and `NameOf`. `blendDistance` is from 0 to `MaxZoneBlendDistance` metres. It does not change the global weather: `Set` and `WeatherChanged` are not affected, and only the player's sky and effects near the zone use the change.
- The newest `SetZone` call wins. Each mod keeps one change for each zone, and for a zone the newest call among all mods applies. When a second mod sets a zone that another mod already set, one warning names both mods.
- `ClearZones(modId)` takes back that mod's changes. The change set before it applies again. When none is left, the zone goes back to the weather and blend distance it had before the first change.
- `SetZone` and `ClearZones` work at any time. They work inside `Events.SceneLoaded` and while a save loads or is being written, because the changes are not saved and the game does not save a zone. <span class="tech">(`AzureWeatherZone` is not a savable component.)</span>
- A zone change never edits the game's weather files. The framework only points the zone at an existing weather. The global weather and other zones are not affected. <span class="tech">(A zone's profile and blend distance are plain data on the component. The framework swaps the zone's reference to an existing profile and never changes a profile asset.)</span>

### Limits

- `SetZone` returns `false` when the input is not allowed. That is an unknown or empty zone id, a weather index that does not exist, a blend distance out of range or not a number, and no weather system. An invalid mod id throws an `ArgumentException`.
- Zone changes last for the session only. They are not saved. Every single-scene load puts the zones back and clears all changes, so set them again from your `Events.SceneLoaded` handler. An additive load (a neighbouring scene loaded alongside) does not clear them.
