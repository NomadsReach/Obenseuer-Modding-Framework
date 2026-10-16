---
title: Obenseuer Modding Framework world API
parent: API reference
nav_order: 91
---

# Obenseuer Modding Framework world API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace `ObenseuerCore`. Listed here are only the public members. Everything else is internal.

Threading: main thread only. Reads return a neutral default when the game system is not ready (no save loaded, main menu, loading). Writes return `bool` success. The only exception is `ArgumentException` for invalid arguments.

`WorldWeather.IsAvailable` is false when the weather hook was not installed or its owner (`obenseuer.moddingframework.world.weather`) was disabled. `WorldPower.IsAvailable` is false when the blackout hook (`obenseuer.moddingframework.world.blackout`) or the generator hook (`obenseuer.moddingframework.world.generator`) is missing or disabled. A missing hook stops only its own event, and the other event keeps firing while `IsAvailable` is false. Reads are not affected.

## WorldWeather

```csharp
public static partial class WorldWeather
{
    public static event Action<WeatherChangedArgs> WeatherChanged;  // args.Index of the weather now targeted, -1 when default

    public static int Count { get; }                 // number of global weather profiles
    public static int CurrentIndex { get; }          // -1 when no weather system or the default profile is active
    public static string CurrentName { get; }        // null when not ready
    public static float Temperature { get; }         // game WeatherManager.Temperature(), 0 when not ready
    public static float RainIntensity { get; }       // average of light, medium and heavy rain, live
    public static bool IsRaining { get; }
    public static float SnowIntensity { get; }       // live
    public static bool IsSnowing { get; }
    public static float WindSpeed { get; }           // live
    public static bool IsThunderActive { get; }

    public static string NameOf(int index);          // null when out of range
    public static int IndexOf(string name);          // -1 when not found
    public static bool Set(int index, float transitionSeconds = 0f);
    public static bool SetThunder(bool active);      // true also when the state already matches

    public const float MaxZoneBlendDistance = 10000f;
    public static List<WeatherZoneInfo> Zones();
    public static bool SetZone(string modId, string zoneId, int weatherIndex, float blendDistance);
    public static bool ClearZones(string modId);     // removes every zone override this mod set
}

public sealed class WeatherZoneInfo
{
    public string Id { get; }                 // scene id of the zone, for SetZone
    public bool Active { get; }               // true when the zone's collider is enabled and its object is active
    public float BlendDistance { get; }       // metres over which the zone fades in
    public string ProfileName { get; }        // name of the weather profile the zone applies now, null when none
    public int WeatherIndex { get; }          // index of the global weather with that same profile, -1 when it is not one
    public bool Overridden { get; }           // true while a mod's SetZone is in force
}
```

- `Set` changes the global weather through the game's `WeatherManager.SetWeather(index, true, transitionSeconds)`. The sky blends over `transitionSeconds`. The game's own timer for the next random change is not reset, so the weather can change again later.
- `Set` throws `ArgumentException` for a negative index and for a transition that is negative, NaN or infinite, even when no weather system is ready. It returns `false` when no weather system is ready. Once one is ready, it throws `ArgumentException` for an index outside `0..Count-1`. It returns `false` when the profile is missing, and while a save loads or saves (`SaveData.Busy`). When the game call throws, the failure is logged. If the requested weather is now active, `Set` raises `WeatherChanged` itself and returns `true`, because the game retargets the weather before the exception can happen and its `ChangeWeather` hook does not run after a throw. Otherwise it returns `false` and nothing is raised.
- `WeatherChanged` fires after the game has retargeted the weather. This includes weather changes made by the game's random timer, by `Set`, by save loads, and by any other `SetWeather` call. It does not fire for each frame of the blend.
- `SetThunder` turns the game's thunder flag on or off (`WeatherManager.StartThunder` and `StopThunder`). It returns `false` when no weather system is ready, or while a save loads or saves. No event is raised.
- The thunder flag is not transient. The game's field carries `[fsProperty]`, so a save made while it is forced can keep it. It lasts until the next `SetWeather`, which sets thunder again from the profile's heavy rain and the game's thunder chance.
- Lightning flashes only while the sky is not overridden and no weather transition is in progress. `SetThunder(true)` alone does not show lightning in those states.
- Local weather zones are `AzureWeatherZone` components placed in a scene. While the player is in or near a zone, the game blends the zone's weather profile over the global weather (`BlendDistance` is the fade distance). `Zones()` lists the zones the game has registered, in the game's order. A scene can have none, and then the list is empty. The game's own sky override zone (used by scenes with an overridden sky and by the shop controller) is left out, because the game rewrites it whenever it enters or leaves one of those places.
- `SetZone` makes a zone apply one of the global weather profiles (`weatherIndex`, the same index as `Set` and `NameOf`) with the given blend distance, 0 to `MaxZoneBlendDistance` metres. It is not the global weather: `Set` and `WeatherChanged` are unchanged, and only the player's sky and effects near the zone use the override. It returns `false` for an unknown or empty zone id, a weather index that does not exist, a blend distance out of range or not a number, an invalid mod id and no weather system. `SetZone` and `ClearZones` work inside `Events.SceneLoaded` and while a save loads or saves, because the overrides are not saved and the game does not save a zone (`AzureWeatherZone` is not a savable component).
- Newest call wins. Each mod keeps one override per zone, and for a zone the newest call among all mods applies. When a second mod sets a zone that another mod already set, one warning names both mods. `ClearZones(modId)` removes that mod's overrides, and the override set before it applies again; with none left the zone returns to the profile and blend distance it had before the first override. `ClearZones` returns `true` when this mod had an override.
- Session only. The overrides are not saved. Every Single scene load puts the zones back and clears all overrides, so set them again from your `Events.SceneLoaded` handler. An additive load does not. Zones register about 0.1 s after the scene starts, so wait a moment after the scene load before you list them.
- A zone's profile and blend distance are plain data on the component, and the framework only swaps the zone's reference to an existing profile. It never changes a profile asset, so the global weather and other zones are not affected.
- Fog is not exposed. The game code has no fog state (`AzureFogScattering` is a visual component only).

## WorldPower

```csharp
public static class WorldPower
{
    public static event Action BlackoutStarted;
    public static event Action<bool> GeneratorToggled;  // new On state of the generator

    public static bool IsBlackoutRunning { get; }       // any BlackoutTrigger sequence in progress
    public static bool AnyGeneratorRunning { get; }     // any generator with PoweredAndActive

    public static bool ScheduleWakeUp(float fadeOutSpeed, float wakeUpDelay, WakeUpAnimation wakeUpAnimation, bool autoWakeUp);
    public static bool WakeUp();
}

public enum WakeUpAnimation
{
    None,
    OnGround,
    OnGround2
}
```

- The game has no global power outage state. Power is per device (`PowerSource`). `IsBlackoutRunning` covers the scripted blackout sequence (fade, theft, teleport) only.
- `BlackoutStarted` fires when a `BlackoutTrigger.StartBlackout` call starts a new sequence. Calls made while one is already running do not fire it.
- `GeneratorToggled` fires when the generator's On switch actually changes through `CraftingBase.Toggle`. It does not fire for a generator running out of fuel, because the game turns that off through a different path.
- `ScheduleWakeUp` only stores the wake-up, via `BlackoutController.SetScheduleWakeUp`. It takes effect on the next level load, in `BlackoutController.OnLoadingGame`, which is when the player wakes. It does not change the level. The caller must do that, as `BlackoutTrigger` does just before `SaveController.ChangeLevel`.
- `WakeUp` calls the game's `BlackoutController.TriggerWakeUp`. That uses the manual values from `SetPendingFinish`, not the ones from `ScheduleWakeUp`. A blackout with `autoWakeUp` false sets them. With none set, it runs with animation `None` and delay `0`, and it re-enables the controls. It returns `false` when `BlackCanvas.instance` is null, because `FinishWakeUp` reads `BlackCanvas.instance.gameObject`.
- Both writes return `false` during a save or load, and when `BlackoutController.instance` is null. `WakeUp` also returns `false` when `BlackCanvas.instance` is null.
- Not exposed: `LightsController` (light culling only, not a power state).

```csharp
WorldPower.ScheduleWakeUp(1f, 2f, WakeUpAnimation.OnGround, autoWakeUp: true);
```

## WorldWater

```csharp
public static class WorldWater
{
    public static bool IsAvailable { get; }           // a water source exists in the current scene
    public static float Amount { get; }               // LiquidAmount of the global storage (game units), 0 when none
    public static bool IsInfinite { get; }            // the global storage has no capacity limit
    public static bool IsPlayerSwimming { get; }     // body-based, WaterController.PlayerIsSwimming
    public static bool IsPlayerUnderwater { get; }   // camera-based, the game's UnderwaterEffect flag
    public static float BreathTicks { get; }         // WaterController.timeInWater: trigger-stay ticks, not seconds
    public const float MaxBreathTicks = 2000f;       // WaterController.maxBreathHoldingTime, copied at compile time
    public static event Action PlayerSubmerged;
    public static event Action PlayerSurfaced;

    public static List<WaterVolumeInfo> Volumes();
    public static bool SetVolume(string id, float density, float drag, float drownDamage, int swimHygiene);
    public static bool ResetBreath();
}

public sealed class WaterVolumeInfo
{
    public string Id { get; }            // the save GUID, or the scene path
    public float Density { get; }        // Water.density, the buoyancy force per unit of submersion
    public float Drag { get; }           // Water.drag, applied as linearDamping while submerged
    public float DrownDamage { get; }    // Water.drowndamage, the health damage per drown tick
    public int SwimHygiene { get; }      // Water.setPlayerHygiene, -1 means no effect
}
```

- The water supply is the `LiquidStorage` returned by `WaterSourceController.globalStorage`.
- No supply event. The game has none, and the amount changes from many unrelated paths.
- Writes to the supply are not exposed. Other tanks are in [Tanks](tanks.md), and its writers refuse this one.
- `Volumes` lists every `Water` component in the active scene. The values are the game's public fields and are read on each call.
- `SetVolume` writes the four public fields of one volume and returns `true` when it did. It returns `false` for an unknown or empty id, a negative or non-finite density, drag or drown damage, a `swimHygiene` below `-1`, or while `SaveData.Busy` is set. The game reads these fields on every trigger stay, so a change applies to the next frame, not only to the next entry.
- `SwimHygiene` is applied by the game while the player is swimming (submersion above 0.7), not when the player enters the volume.
- `maxDepth` (the box collider height, set at `Awake`) and `fogColor` are not exposed.
- `IsPlayerUnderwater` is the camera's flag. `UnderwaterEffect.EnableEffect` sets it and `DisableEffect` clears it. The game calls them from a `Water` trigger on the MainCamera, so it is the authoritative state. `IsPlayerSwimming` comes from the player's body triggers, so the two can differ.
- `PlayerSubmerged` fires when the flag goes from false to true, and `PlayerSurfaced` when it goes from true to false, after the game's own method has run. Neither fires while a save loads. `LoadingScreen` calls `DisableEffect` on every load and scene change, so `PlayerSurfaced` is suppressed there. A subscriber can therefore see `PlayerSubmerged` with no matching `PlayerSurfaced` across a load or a scene change. Resync from `IsPlayerUnderwater` on `Events.SceneLoaded`. A camera destroyed while submerged never reaches `DisableEffect`, so `PlayerSurfaced` may not fire. Nothing is synthesized.
- Leaving one of two overlapping `Water` volumes calls `DisableEffect` even when the camera is still inside the other, and the next `OnTriggerStay` re-enables it. That can raise `PlayerSurfaced` and then `PlayerSubmerged` in the same frame. This is the game's behaviour.
- `BreathTicks` counts `OnTriggerStay` calls on the PlayerHead collider, not seconds. It is set to 0 when the head leaves a volume, and it is not a save field. Drowning damage starts at 2000 (`Water`, a literal, not this constant) and repeats every 0.5 s while the head stays in, because the counter keeps rising. `MaxBreathTicks` is the game's value for this build and does not follow the game if that changes.
- `ResetBreath` sets `BreathTicks` to 0. It returns `false` while a save loads or saves, or when `WaterController.instance` is null. It does not clear the drown cooldown (`Water.dmgcooldown`, `Water`), so a drown hit already inside its 0.5 s window still lands, through the game's own `health.Damage` call.
- The events are hooked on `UnderwaterEffect.EnableEffect` and `DisableEffect`, under the owner `obenseuer.moddingframework.worldwater.events`. If a hook fails, only the two events stop. The reads, `ResetBreath` and `SetVolume` keep working.

## WorldRadiation

```csharp
public static class WorldRadiation
{
    public static float Dose { get; }                  // accumulated radiation dose of the player
    public static float DoseDamageThreshold { get; }   // dose above which damage starts
    public static float BackgroundLevel { get; }       // background radiation for the current area
    public static bool IsWasteland { get; }
    public static float DoseRate { get; }              // mSv/h at the player's head, last game sample, 0 when unknown
    public static bool DosimeterEquipped { get; }      // the dosimeter hand item is drawn, in either hand
    public static bool? DosimeterOn();                 // null when no dosimeter is held
    public static float? DosimeterCharge();            // ammo / maxAmmo, null when no dosimeter is held
}
```

- `Dose` is the value the game keeps in `PlayerStats.radiation`. It is not a rate.
- `DoseRate` is the value the game's own `RadiationController.MeasureRadiation` call returned for the player's head. That call runs about once per in-game minute, through `PlayerStats.DeltaSeconds` and `PlayerStatsRadiation.UpdateStats`. A passive postfix (`src/Api/WorldRadiationPatches.cs`) copies the result and does not call `MeasureRadiation` itself, so the rate can lag a real change by up to a minute. It is 0 until the first sample, after `Events.Loaded`, while `PlayerLocator` or `RadiationController` is missing, and while stats progress is disabled, because the game stops sampling then. It excludes `TriggerRadiation` volumes and the dosimeter's plus or minus 10% roll.
- `DosimeterOn()` and `DosimeterCharge()` look at the held dosimeter, right hand first and then left, drawn or not, the same order as `Weapons.FlashlightOn`. `DosimeterOn` is true only when the device's own switch is set and it has ammo (`ammo > 0`), because the display only runs then. The switch is the private `Dosimeter.on`, read by reflection, so an empty battery reads false even while the switch is still set. `DosimeterCharge` is `ammo / maxAmmo` and is `null` when the maximum is 0 or less. The switch lives only in the instance, so a freshly spawned dosimeter reads false until it is turned on.
- No event. `MeasureRadiation` is not exposed as a call. For a non-player transform it adds receiver bookkeeping, so a mod that calls it would change game state.
- Radiation is a world system (background, point sources, triggers).
- `DosimeterEquipped` is true when the held dosimeter is drawn (`Dosimeter.equipped`) on the right or left hand item. It does not mean the device is on or has ammo.
- No `DosimeterReading`. The dosimeter re-rolls its value by 98% to 102% on each update and keeps it only in a local and in its text. The unrolled value is what `Dose`, or `BackgroundLevel` when `Dose` is 0, already returns, so a reading would copy them.

## WorldLocation

```csharp
public static class WorldLocation
{
    public static string CurrentScene { get; }        // active Unity scene name
    public static List<string> KnownScenes();              // every scene name in LevelNames.bytes
    public static List<string> VisitedScenes();            // scenes visited in this save
}
```

- For a scene change event, use the framework's existing `Events.SceneLoaded`. Not duplicated here.
- `VisitedScenes()` returns the scenes visited in the save being played, without duplicates and without case: the game's own visited list (when `SaveSceneManager` exists), the active scene, the level files in the save folder (`*.tnmt`, except `Globals` and `Info`), and the scenes entered this session. The game does not write its own list into a save, so the save-folder files are what keep a visit after a load. It returns an empty list when there is no save, never `null`. The session part starts when the framework starts. It is cleared when a save is loaded from the load menu and when `Main Menu` or `Open Sewer Intro` loads. Door changes keep it. `Main Menu` and `Open Sewer Intro` are left out, the two scenes the game itself treats as non-gameplay. After either of them loads, the save-folder files are ignored until the next load. Saving to a new slot does not update the folder until the next load, so the active scene and the session set cover that.
- Indoors is not exposed. The game does not track whether the player is inside. `InsideTrigger` is for build-mode placement only.

## WorldClock

```csharp
public static class WorldClock
{
    public static int Year { get; }
    public static int Month { get; }
    public static int CalendarDay { get; }           // day of the month. GameTime.Day is the game day counter, a different value.
    public static bool IsAvailable { get; }          // true when the game clock exists
    public static WorldTime Now { get; }             // every field below in one read, all zero when not available

    public static bool SetTimeOfDay(int hour, int minute);  // hour 0..23, minute 0..59
    public static bool SetDate(int year, int month, int day);  // year 1..9999, month 1..12, day 3..days in that month
}
```

- `WorldTime` is a read-only struct: `Year`, `Month`, `CalendarDay` (day of the month), `Day` (the game day counter, `GameTime.Day`), `Weekday` (`GameTime.DayOfWeek`), `Hour` and `Minute` (`GameTime.Hour` and `Minute`). `Now` fills it from the same calls, so it equals reading them one by one. When `IsAvailable` is false, `Now` is all zero (`Weekday` is `Sunday`).
- `WorldClock` has no minute event. `TimeControl.MinutePassed` fires once per game minute and `Events.HourPassed` once per game hour.
- Use `GameTime` for reads of the time of day and the day counter. `WorldClock` adds the missing setters and the calendar reads.
- `SetTimeOfDay` calls the game's `TimeOfDayAzure.SetTimeline`, which takes hours from 0 to 24. The minute is placed in the middle of its minute so it reads back as that minute. It returns `false` while a save loads or saves (`SaveData.Busy`), and when the time is earlier than the game's current time, seconds included. It also returns `false` when the game's timeline value cannot be read. The game does not move the day back for an earlier time, so use a later time or wait for the next day.
- `SetDate` calls the game's `TimeOfDayAzure.SetDate`, the call the game itself makes on a save load. It throws `ArgumentException` for a year outside 1 to 9999, a month outside 1 to 12, or a day outside 3 to the last day of the month (the game's calendar throws on year 0 and clamps a bad day). Days 1 and 2 are refused because the game never holds them: when a day change lands on day 1 the game moves the date to day 3 (`TimeOfDayAzure.IncreaseDayCounter`), so the date checks that compare the day of the month with a stored one (`PlayerSubscription`) have never run on those days. It returns `false` while a save loads or saves (`SaveData.Busy`), when the clock is not available, and while the game has time updates off (just after a scene starts), because a date set then would be taken for a new day. It sets the calendar only: the time of day, the game day counter (`GameTime.Day`) and every timer stay as they are, and it raises no `Events.DayPassed`. It works forwards and backwards.
- The weekday follows the date. Anything the game drives from the weekday or the day of the month reacts at the next minute tick: NPC schedules, rent day (Sunday), and the weekly, biweekly and monthly subscription, invoice and bank payment checks (`PlayerSubscription.CheckDateAndTrigger` compares the day of the month with the days since its last check, so a low day of the month can make a payment come due). The date is saved with the game.
- Timers, perishables, mail and crops are not tied to the calendar date. They use the game day counter, the hour and the minute, which `SetDate` does not touch.
- Moving the clock backwards with the game's own calls (`TimeOfDayAzure.SetTimeline`, the developer console) makes the game treat it as a wrap past midnight, so its timers advance by the wrapped difference. `SetTimeOfDay` refuses an earlier time, so it never does this.
- Setting the time does not fire `Events.HourPassed`/`DayPassed` by itself. Those follow the game's own clock.
- Not exposed: `dayCounter` (the game day counter). It has no setter in the game, only `IncreaseDayCounter`, which fires the day event.
- `GameTime.IsDaytime` is the game's day rule, `TimeOfDayAzure.IsItDay`: hours 6 to 20, so 6:00 to 20:59. The game's plants use the same call. It reads the cached `currentTimeAndDay`, refreshed once per in-game minute and not while time updates are disabled, so it can lag `GameTime.Hour`. The answer depends on the hour alone, not on the sun's position.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
