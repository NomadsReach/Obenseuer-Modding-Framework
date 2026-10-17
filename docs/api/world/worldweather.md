---
title: WorldWeather
parent: World
grand_parent: Places and travel
nav_order: 2
---

# WorldWeather

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`WorldWeather` reads the weather and changes it. The game has a list of weather profiles, called the global weather. Each profile is a named weather with an index counted from `0`. `NameOf` and `IndexOf` turn one into the other.

```csharp
public static partial class WorldWeather
{
    public static event Action<WeatherChangedArgs> WeatherChanged;  // the weather the game is heading for has changed; args.Index is its index, -1 when it is the default

    public static bool IsAvailable { get; }          // false when the weather change event could not be set up

    public static int Count { get; }                 // how many global weather profiles there are
    public static int CurrentIndex { get; }          // the index of the current weather; -1 when there is no weather system or the default is active
    public static string CurrentName { get; }        // the name of the current weather; null when not ready
    public static float Temperature { get; }         // the temperature the game uses; 0 when not ready
    public static float RainIntensity { get; }       // how hard it rains right now: the average of light, medium and heavy rain
    public static bool IsRaining { get; }            // true when it is raining
    public static float SnowIntensity { get; }       // how hard it snows right now
    public static bool IsSnowing { get; }            // true when it is snowing
    public static float WindSpeed { get; }           // how strong the wind is right now
    public static bool IsThunderActive { get; }      // true when thunder is on

    public static string NameOf(int index);          // the name of a weather; null when the index is out of range
    public static int IndexOf(string name);          // the index of a weather by name; -1 when not found
    public static bool Set(int index, float transitionSeconds = 0f);   // changes the global weather; true when it worked
    public static bool SetThunder(bool active);      // turns thunder on or off; true also when it already was that way
}

public sealed class WeatherChangedArgs    // what WeatherChanged tells you
{
    public int Index { get; }    // the index of the weather the game is heading for; -1 for the default
}
```

- **The weather values give plain defaults when the game has no weather system.** That is on the main menu or while a save loads. `Count` is `0`, `CurrentIndex` is `-1`, `CurrentName` is `null`, and the numbers are `0`.
- **`Set(index, transitionSeconds)` changes the global weather, and the sky blends over `transitionSeconds`.** The game's own timer for the next random change is not reset, so the weather can change again later.
  - Technical: it calls the game's `WeatherManager.SetWeather(index, true, transitionSeconds)`.
- **`Set` throws an `ArgumentException` for a bad number.** That is a negative index, or a transition that is negative, `NaN` or infinite. It does this even when no weather system is ready. Once a weather system is ready, it also throws for an index outside `0..Count-1`.
- **`Set` returns `false` for other problems.** That is when no weather system is ready, when the profile is missing, and while a save is loading or being written.
- **If the game stumbles during `Set`, you may still get `true`.** When the game call throws but the weather you asked for is now active, `Set` raises `WeatherChanged` itself and returns `true`. Otherwise it returns `false` and nothing is raised. The failure is logged.
  - Technical: the game retargets the weather before the exception can happen, and the framework's change to `ChangeWeather` does not run after a throw.
- **`WeatherChanged` fires after the game has changed the weather it is heading for.** That includes changes made by the game's random timer, by your `Set`, by a save loading, and by any other game call. It does not fire for each frame of the blend.
  - Technical: it is raised after `WeatherManager.ChangeWeather`.
- **`SetThunder` turns thunder on or off.** It returns `false` when no weather system is ready, or while a save is loading or being written. No event is raised.
  - Technical: it calls `WeatherManager.StartThunder` and `StopThunder`.
- **Forced thunder can stay in a save.** A save made while thunder is forced can keep it. It lasts until the next `Set` of the weather, which sets thunder again from the profile's heavy rain and the game's thunder chance.
  - Technical: the game's field carries `[fsProperty]`.
- **Thunder alone does not always show lightning.** Lightning flashes only while the sky is not overridden and no weather change is blending. `SetThunder(true)` alone shows no lightning in those states.
- **Fog is not available.** The game has no fog setting to read.
  - Technical: `AzureFogScattering` is a visual component only.
- **`IsAvailable` tells you whether `WeatherChanged` works.** It is `false` when the framework could not set up the event, or when its feature group was turned off. Reads are not affected.

### Weather zones

A weather zone is an area in a scene with its own local weather. While the player is in or near a zone, the game blends the zone's weather over the global weather. The distance over which the zone fades in is its blend distance. A scene can have no zones at all. Then `Zones()` is an empty list.

```csharp
public static partial class WorldWeather
{
    public const float MaxZoneBlendDistance = 10000f;   // the largest blend distance you can set, in metres

    public static List<WeatherZoneInfo> Zones();        // the weather zones the game has in this scene
    public static bool SetZone(string modId, string zoneId, int weatherIndex, float blendDistance);   // makes a zone use one of the global weathers
    public static bool ClearZones(string modId);        // takes back every zone change your mod made; true when it had made one
}

public sealed class WeatherZoneInfo    // a snapshot of one weather zone
{
    public string Id { get; }                 // the zone's id in the scene; use it with SetZone
    public bool Active { get; }               // true when the zone is switched on and can take effect
    public float BlendDistance { get; }       // the distance in metres over which the zone fades in
    public string ProfileName { get; }        // the name of the weather the zone applies now; null when none
    public int WeatherIndex { get; }          // the index of the global weather with that same profile; -1 when it is not one
    public bool Overridden { get; }           // true while a mod's SetZone is in force
}
```

- **`Zones()` lists the zones the game has registered, in the game's order.** The game's own sky-override zone is left out, because the game rewrites it whenever the player enters or leaves one of the places that use it. Zones register about 0.1 seconds after a scene starts, so wait a moment after the scene load before you list them.
  - Technical: the zones are `AzureWeatherZone` components. `Active` is true when the zone's collider is enabled and its object is active. The left-out zone is the one used by scenes with an overridden sky and by the shop controller.
- **`SetZone` makes a zone use one of the global weathers.** `weatherIndex` is the same index as `Set` and `NameOf`. `blendDistance` is from 0 to `MaxZoneBlendDistance` metres. It does not change the global weather: `Set` and `WeatherChanged` are not affected, and only the player's sky and effects near the zone use the change.
- **`SetZone` returns `false` when the input is not allowed.** That is an unknown or empty zone id, a weather index that does not exist, a blend distance out of range or not a number, and no weather system. An invalid mod id throws an `ArgumentException`.
- **`SetZone` and `ClearZones` work at any time.** They work inside `Events.SceneLoaded` and while a save loads or is being written, because the changes are not saved and the game does not save a zone.
  - Technical: `AzureWeatherZone` is not a savable component.
- **The newest `SetZone` call wins.** Each mod keeps one change for each zone, and for a zone the newest call among all mods applies. When a second mod sets a zone that another mod already set, one warning names both mods.
- **`ClearZones(modId)` takes back that mod's changes.** The change set before it applies again. When none is left, the zone goes back to the weather and blend distance it had before the first change. It returns `true` when the mod had a change.
- **Zone changes last for the session only.** They are not saved. Every single-scene load puts the zones back and clears all changes, so set them again from your `Events.SceneLoaded` handler. An additive load (a neighbouring scene loaded alongside) does not clear them.
- **A zone change never edits the game's weather files.** The framework only points the zone at an existing weather. The global weather and other zones are not affected.
  - Technical: a zone's profile and blend distance are plain data on the component. The framework swaps the zone's reference to an existing profile and never changes a profile asset.
