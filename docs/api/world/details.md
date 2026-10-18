---
title: Details
parent: World
grand_parent: Places and travel
nav_order: 8
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts, so it can tell your mod about weather, blackouts, generators, swimming and radiation. This page calls each named set of changes a feature group. This page uses five: `obenseuer.moddingframework.world.weather` (the weather event), `obenseuer.moddingframework.world.blackout` (the blackout event), `obenseuer.moddingframework.world.generator` (the generator event), `obenseuer.moddingframework.worldwater.events` (the two water events) and `obenseuer.moddingframework.worldradiation` (the radiation rate). These are the names you will see in the log.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../../glossary.md#main-thread)).
- Reads give a plain default when the game is not ready. That is when no save is loaded, on the main menu, or while loading. Calls that change the game return `true` or `false` for success.
- The only errors your code can get are `ArgumentException` for arguments that are not allowed. The calls that throw are named above.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework turns off the [feature group](../../glossary.md#feature-group) that depends on it, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working. The reads and the calls that change the game keep working. Only the event of the missing group stops.

- Each event has its own feature group, so one failure does not stop the others. If the weather change is missing, only `WeatherChanged` stops. If the blackout or generator change is missing, only that event stops, and the other keeps firing.
- The water events share a group, and so do `PlayerSubmerged` and `PlayerSurfaced`. If the radiation change is missing, `DoseRate` stays `0`. <span class="tech">(The weather group hooks `WeatherManager.ChangeWeather`, the blackout group `BlackoutTrigger.StartBlackout`, the generator group `CraftingBase.Toggle`, the water group `UnderwaterEffect.EnableEffect` and `DisableEffect`, and the radiation group `RadiationController.MeasureRadiation`.)</span>

## Save loads

- `WeatherChanged` is raised when a save loads, because the game sets the weather then. The two water events are not raised while a save loads.
- For the other events, a handler that should ignore events raised during a save load can check `SaveData.Busy`, which is `true` while a save is loading or being written.
