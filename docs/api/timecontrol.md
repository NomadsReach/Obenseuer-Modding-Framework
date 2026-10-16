---
title: TimeControl API
parent: API reference
nav_order: 95.2
---

# TimeControl API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.timecontrol`.
Files: `src/Api/TimeControl.cs`, `src/Api/TimeControlPatches.cs`.

This area changes how fast the in-game clock runs. It also gives in-game minute timers and a minute event. It does not change the date.

## If a hook cannot be installed

The framework installs one prefix on `AzureTimeController.SetNewDayLength(float)`. If it fails, `TimeControl.IsAvailable` is `false` and `TimeScale` stays at `1`. `In`, `Cancel`, `MinutePassed` and `SecondsPassed` keep working, because they do not use the prefix.

## API

```csharp
public static bool IsAvailable { get; }
public static float TimeScale { get; }
public static float DayLengthMinutes { get; }
public static bool SetTimeScale(string modId, float scale);
public static bool ClearTimeScale(string modId);
public static int In(string modId, int inGameMinutes, Action action);
public static bool Cancel(string modId, int handle);
public static event Action MinutePassed;
public static event Action<int> SecondsPassed;
```

- `TimeScale` is the value in effect: the newest scale any mod set, or `1` when none is set.
- `DayLengthMinutes` is the game's own day length, read only. It is `0` when no time controller exists.
- `SetTimeScale` returns `false` for an empty mod id, a scale that is not above zero or an unavailable hook. `SetTimeScale`, `ClearTimeScale`, `In` and `Cancel` work inside `Events.SceneLoaded` and while a save loads or saves, because nothing they set is saved. A call made while a save loads can still be dropped (see Save loads). The scale is clamped to 0.01 to 100.
- `In` returns a handle above zero, or `0` when refused. `Cancel` returns `true` only when the handle belongs to the same mod id.
- `SecondsPassed` gives the number of in-game seconds the clock moved in that frame.

## Example

```csharp
TimeControl.SetTimeScale("com.example.mymod", 2f);
int handle = TimeControl.In("com.example.mymod", 30, () => CoreLog.For("com.example.mymod").Info("30 in-game minutes passed"));
TimeControl.ClearTimeScale("com.example.mymod");
```

## Behaviour

- The scale multiplies the day length that the game passes to its time controller every frame. A scale of 2 makes the clock run twice as fast. The game's own sleep, crafting and card game speed-ups still apply, multiplied by this scale.
- The framework never writes `Time.timeScale` or the game's `timeSpeedOverrideFactor`. The game's pause sets `Time.timeScale` to 0 and back to 1, so the clock stops and starts with the pause and nothing needs restoring.
- Newest call wins. Each mod keeps its own scale. When the newest mod clears, the scale set before it applies again.
- The player's Tweaks time setting is a separate multiplier, and it multiplies with this scale.
- Scales and `In` timers drop when a save is loaded from the menu or the pause menu, when the main menu loads, and when a new game starts. A level change keeps them, so a scale or timer set in one level still applies in the next. A call made while a save loads can be dropped by the load clear. Set it again from `Events.WhenReady`, which runs after the load.
- `In` counts `MinutePassed` events, not game minutes. The game raises `MinutePassed` once each time the minute value changes. At a high time scale a frame can skip a minute, so the timer fires late by that minute. A tick that arrives while the game is saving or loading is dropped, so the timer also runs late by that tick.
- A callback that throws is logged, and the other callbacks still run.
- Events are not raised while a save loads or saves. Writes are not gated by `SaveData.Busy`, so a write during a load can be dropped by the load clear above.

## Not exposed

- Setting the date. Perishables, mail, invoices, subscriptions and other systems store absolute dates, and the day counter does not follow a jump. See `docs/api/world.md`.
- The game's own timer list. The game saves timer callbacks by reference, so `In` keeps its own list.
- The game's `timeSpeedOverrideFactor`. The game writes it in several places, so a mod write would be undone.
- NPC path positions read the game's own day length, not this scale.

## Ceilings and risks

- At high scales `In` runs late, as described under Behaviour.
- Timers are not saved. A timer set before a save is lost when that save is loaded from the main menu.

## Save loads

Scales and timers are kept in memory only, and nothing is written to the save.
