# Obenseuer Modding Framework world API

Namespace `ObenseuerCore`. Target: Obenseuer 0.5.05 (buildid 25839246). Listed here are only the public members. Everything else is internal.

Threading: main thread only. Reads return a neutral default when the game system is not ready (no save loaded, main menu, loading). Writes return `bool` success. The only exception is `ArgumentException` for invalid arguments.

`WorldPower.IsAvailable` and `WorldWeather.IsAvailable` are false when the world hooks were not installed or the owner was disabled. Reads are not affected.

Init: `WorldPatches.Init()` (internal) installs the event hooks. It must be wired from `Core.Awake` with `Isolate("world", WorldPatches.Init);`. Without it, the read and write members still work and the events never fire.

## WorldWeather

```csharp
public static class WorldWeather
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
}
```

- `Set` changes the global weather through the game's `WeatherManager.SetWeather(index, true, transitionSeconds)`. The sky blends over `transitionSeconds`. The game's own timer for the next random change is not reset, so the weather can change again later.
- `Set` returns `false` when no weather system is ready. Otherwise it throws `ArgumentException` for an index outside `0..Count-1` and for a negative transition. It returns `false` when the profile is missing or the game call fails (the failure is logged).
- `WeatherChanged` fires after the game has retargeted the weather. This includes weather changes made by the game's random timer, by `Set`, by save loads, and by any other `SetWeather` call. It does not fire for each frame of the blend.
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
- `ScheduleWakeUp` only stores the wake-up, via `BlackoutController.SetScheduleWakeUp` (`Events/BlackoutController.cs:63`). It takes effect on the next level load, in `BlackoutController.OnLoadingGame` (`Events/BlackoutController.cs:40`), which is when the player wakes. It does not change the level. The caller must do that, as `BlackoutTrigger` does just before `SaveController.ChangeLevel` (`Events/BlackoutTrigger.cs:122`).
- `WakeUp` calls the game's `BlackoutController.TriggerWakeUp` (`Events/BlackoutController.cs:90`). That uses the manual values from `SetPendingFinish`, not the ones from `ScheduleWakeUp`. A blackout with `autoWakeUp` false sets them. With none set, it runs with animation `None` and delay `0`, and it re-enables the controls. It returns `false` when `BlackCanvas.instance` (`UI/BlackCanvas.cs:14`) is null, because `FinishWakeUp` reads `BlackCanvas.instance.gameObject` (`Events/BlackoutController.cs:132`).
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
    public static bool IsPlayerSwimming { get; }
}
```

- The water supply is the `LiquidStorage` returned by `WaterSourceController.globalStorage`.
- No supply event. The game has none, and the amount changes from many unrelated paths.
- Writes are not exposed. The game has no method to set the supply level without a liquid type.

## WorldRadiation

```csharp
public static class WorldRadiation
{
    public static float Dose { get; }                  // accumulated radiation dose of the player
    public static float DoseDamageThreshold { get; }   // dose above which damage starts
    public static float BackgroundLevel { get; }       // background radiation for the current area
    public static bool IsWasteland { get; }
}
```

- `Dose` is the value the game keeps in `PlayerStats.radiation`. It is not a rate.
- No rate or event is exposed. The game computes the rate by calling `RadiationController.MeasureRadiation` every second, which has side effects (receiver bookkeeping). Reading it from a mod would change game state.
- Radiation is under `Player/` in the RE tree but is a world system (background, point sources, triggers).

## WorldLocation

```csharp
public static class WorldLocation
{
    public static string CurrentScene { get; }        // active Unity scene name
    public static List<string> KnownScenes();              // every scene name in LevelNames.bytes
    public static List<string> VisitedScenes();            // scenes visited in this save
}
```

- For a scene change event, use the existing `Events.SceneLoaded` (Core). Not duplicated here.
- Indoors is not exposed. The game does not track whether the player is inside. `InsideTrigger` is for build-mode placement only.

## WorldClock

```csharp
public static class WorldClock
{
    public static int Year { get; }
    public static int Month { get; }
    public static int CalendarDay { get; }           // day of the month. GameTime.Day is the game day counter, a different value.

    public static bool SetTimeOfDay(int hour, int minute);  // hour 0..23, minute 0..59
}
```

- Use `GameTime` for reads of the time of day and the day counter. `WorldClock` adds the missing setters and the calendar reads.
- `SetTimeOfDay` calls the game's `TimeOfDayAzure.SetTimeline`, which takes hours from 0 to 24. The minute is placed in the middle of its minute so it reads back as that minute.
- Not exposed: setting the date. Perishables, timers, mail and crops store absolute `TimeAndDay` values, and the game's day counter would not follow.
- Moving the clock backwards makes the game treat it as a wrap past midnight, so its timers advance by the wrapped difference.
- Setting the time does not fire `Events.HourPassed`/`DayPassed` by itself. Those follow the game's own clock.
- Not exposed: `dayCounter` (the game day counter). It has no setter in the game, only `IncreaseDayCounter`, which fires the day event.
