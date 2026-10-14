---
title: Sleep events API
parent: API reference
nav_order: 75
---

# Sleep events API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owners: `obenseuer.moddingframework.sleepevents` (`ExecuteEvent`) and `obenseuer.moddingframework.sleepevents.intercept` (`CalculateEvent`).
Files: `src/Api/SleepEvents.cs`, `src/Api/SleepEventsPatches.cs`.

Everything not listed here is internal.

## If a hook cannot be installed

The framework installs two hooks, each under its own owner: a prefix on `SleepEventController.CalculateEvent` (the intercept owner) and a postfix on `SleepEventController.ExecuteEvent` (the sleep events owner). A missing hook removes only its own owner's patches and logs one warning. If an install throws, such as a handler name typo, both owners are removed. `InProgress` keeps working because it reads game state directly.

## Threading and safety

- Main thread only.
- `SleepEvents.IsAvailable` is false when a hook was not installed or an owner was disabled. A missing intercept hook stops `Intercept`, a missing execute hook stops `Occurred`, and the other event keeps firing while `IsAvailable` is `false`.
- `InProgress` is safe at any time. It returns `false` on the main menu, during loading and with no save loaded.
- `Occurred` and `Intercept` are raised, so a handler that throws is logged and the other handlers still run.
- Neither event is raised by save-restore code. `ExecuteEvent` runs only from the sleep timer (`WaitingController.CheckEvent`, called by `PlayerStatsTiredness`), and `CalculateEvent` runs only when a sleep or passout starts.

## Reads

```csharp
public static class SleepEvents
{
    public static bool InProgress { get; }
    public static bool IsAvailable { get; }
    public static bool PlayerInSafeArea { get; }
    public static float SafetyFactor { get; }
}
```

`InProgress` is `SleepEventController.eventInProgress`. It is `true` while a sleep event runs a coroutine (arrest, get caught, nightmare, crazy neighbour) and while the steal or prank sound coroutine plays, so it can be `true` for an event that only played a sound.

`PlayerInSafeArea` is `SleepEventController.playerInSafeArea`, and `SafetyFactor` is the game's `playerSafetyFactor`. Both hold the last value a `TriggerSafe` wrote to the controller. They are not a live check of where the player stands: the game does not clear them when the player leaves a trigger that has no adders, so `PlayerInSafeArea` can stay `true` after the player has left. Neither field is `fsIgnore`, so both are saved with the game. After a load they hold the saved value until a trigger writes again, and a trigger the player is standing in rewrites them on its first physics tick.

`PlayerInSafeArea` is `false` when the controller is missing. `SafetyFactor` is `0` when the controller is missing, or when `info_game_logic` is missing and the player is not in a safe area. In a safe area `SafetyFactor` is `1`, the same value the game's own getter returns.

A bed's own `safe` flag also changes the sleep. It starts without the passout events, so `PlayerInSafeArea` being `false` does not mean the bed is unsafe. For a player outside a safe area the game's sleep prompt reads 'Unsafe' below 0.5 and 'Safeish' from 0.5 up.

## Event kinds

```csharp
public enum SleepEventKind
{
    None, Wakeup, StealRandomItems, StealMoney, StealOS, StealRM, StealMostValuableItem,
    Steal, Beat, Arrest, Quarantine, CoveredWithJunk, InfectSMV, GetCaughtFromCrime,
    Nightmare, CrazyNeighbor, Invoice
}
```

The values follow the game's `SleepEventController.SleepEvents` in order. The framework casts between the two by value.

## Occurred

```csharp
public static event Action<SleepEventOccurredArgs> Occurred;

public sealed class SleepEventOccurredArgs
{
    public SleepEventKind Kind { get; }    // the event the game just executed
}
```

Raised after the game's `ExecuteEvent` returns. For the coroutine events the effect has only started. A `Steal` with nothing to take still raises `Occurred`, because the game still runs the method.

## Intercept

```csharp
public static event Action<SleepEventInterceptArgs> Intercept;

public sealed class SleepEventInterceptArgs
{
    public List<SleepEventKind> Candidates { get; }   // every kind the game could roll for this sleep, in order
    public bool Cancel { get; set; }                  // set true to roll no random event for this sleep
}
```

Raised before the game picks the random sleep event, when a sleep starts (including a passout). It also fires when the player is in a safe area or `Candidates` is empty, where the game rolls no event anyway. Setting `Cancel` makes the game roll no random event for that sleep. The sleep then continues as if the roll had found nothing. The crime arrest check that runs after the roll is not affected. A handler cannot choose a kind; it can only cancel the whole roll. With several subscribers, any `Cancel` wins: once a handler sets it, the later handlers still run and see `Cancel` as `true`, and setting it back to `false` has no effect. `Candidates` is a copy, so changing it does nothing.

`Intercept` is not offered on the execute step. Skipping `ExecuteEvent` would still show the event's dialogue at wake-up, for an event that did not happen.

## Not exposed

- Forcing or replacing a sleep event.
- Changing the safety factor or the safe-area flag.
- Cancelling a crime arrest after sleep.
