# Sleep events API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.sleepevents`.
Files: `src/Api/SleepEvents.cs`, `src/Api/SleepEventsPatches.cs`.

Everything not listed here is internal.

## Wiring

```csharp
internal static void SleepEvents.Init();
```

Core calls this from `Core.Awake`.

`Init` installs two hooks under one owner: a prefix on `SleepEventController.CalculateEvent` and a postfix on `SleepEventController.ExecuteEvent`. If either is missing, both are removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.sleepevents")` becomes true, and one warning is logged. `InProgress` keeps working because it reads game state directly.

## Threading and safety

- Main thread only.
- `SleepEvents.IsAvailable` is false when the hooks were not installed or the owner was disabled. `Occurred` and `Intercept` only fire while it is true.
- `InProgress` is safe at any time. It returns `false` on the main menu, during loading and with no save loaded.
- `Occurred` and `Intercept` are raised through `Events.Raise`, so a handler that throws is logged and the other handlers still run.
- Neither event is raised by save-restore code. `ExecuteEvent` runs only from the sleep timer (`WaitingController.CheckEvent`, called by `PlayerStatsTiredness`), and `CalculateEvent` runs only when a sleep or passout starts.

## Reads

```csharp
public static class SleepEvents
{
    public static bool InProgress { get; }
    public static bool IsAvailable { get; }
}
```

`InProgress` is `SleepEventController.eventInProgress`. It is `true` while a sleep event runs a coroutine (arrest, get caught, nightmare, crazy neighbour) and while the steal or prank sound coroutine plays, so it can be `true` for an event that only played a sound.

## Event kinds

```csharp
public enum SleepEventKind
{
    None, Wakeup, StealRandomItems, StealMoney, StealOS, StealRM, StealMostValuableItem,
    Steal, Beat, Arrest, Quarantine, CoveredWithJunk, InfectSMV, GetCaughtFromCrime,
    Nightmare, CrazyNeighbor, Invoice
}
```

The values follow the game's `SleepEventController.SleepEvents` in order. Core casts between the two by value.

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

Raised before the game picks the random sleep event, when a sleep starts (including a passout). Setting `Cancel` makes the game roll no random event for that sleep. The sleep then continues as if the roll had found nothing. The crime arrest check that runs after the roll is not affected. A handler cannot choose a kind; it can only cancel the whole roll.

`Intercept` is not offered on the execute step. Skipping `ExecuteEvent` would still show the event's dialogue at wake-up, for an event that did not happen.

## Not exposed

- Forcing or replacing a sleep event.
- Changing the safety factor or the safe-area flag.
- Cancelling a crime arrest after sleep.
