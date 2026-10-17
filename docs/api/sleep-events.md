---
title: Sleep events
parent: Player life and skills
grand_parent: API reference
nav_order: 8
---

# Sleep events

## What it is

When the player sleeps, or passes out, the game can roll a random event while they are out: a thief steals something, they are beaten, the police take them away, they have a nightmare, and so on. This API lets your mod see which event the game ran, look at the events that could happen before the game rolls, cancel the roll for that sleep, and check whether an event is running. It also tells you what the game thinks about how safe the player's sleeping place is.

## When to use it

- Log or react when something happens to the player in their sleep.
- Stop the random event for a sleep, for example during a story scene.
- Wait until a sleep event has finished before you do something else.

## Quick example

This mod cancels the random event for any sleep where the game could beat the player, and it logs every event that does happen. It cancels the whole roll, not just the beating.

```csharp
using ObenseuerCore;

[ModInfo("example.safesleep", "Safe Sleep", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class SafeSleep : FrameworkMod
{
    protected override void OnLoad()
    {
        SleepEvents.Intercept += args =>
        {
            if (args.Candidates.Contains(SleepEventKind.Beat)) args.Cancel = true;
        };
        SleepEvents.Occurred += args => Log.Info("While you slept: " + args.Kind);
    }
}
```

`Intercept` and `Occurred` are events: the framework calls your code each time they happen. `Intercept` happens just before the game picks a random event, and `Occurred` happens just after the game runs one.

## Common tasks

Check whether a sleep event is running right now.

```csharp
Log.Info("A sleep event is running: " + SleepEvents.InProgress);
```

Read what the game thinks about how safe the player is.

```csharp
if (SleepEvents.PlayerInSafeArea)
    Log.Info("The game last marked the player as safe, factor " + SleepEvents.SafetyFactor);
```

Log every event the game could roll for a sleep.

```csharp
SleepEvents.Intercept += args =>
{
    foreach (SleepEventKind kind in args.Candidates)
        Log.Info("Could roll: " + kind);
};
```

Check whether the events work.

```csharp
if (!SleepEvents.IsAvailable) Log.Warn("Sleep events are off.");
```

## Reference

This is the full list of what the API gives you.

### Checking the state of sleep

These read the game's current state. They never change anything.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SleepEvents</code> <span>the state of sleep</span></p>

This is the class to start from. `InProgress`, `PlayerInSafeArea` and `SafetyFactor` read the game's sleep state, and the `Occurred` and `Intercept` events below are where you hear about a sleep event or act on its roll.

<div class="api-pair" markdown="1">

```csharp
public static class SleepEvents
{
    public static bool InProgress { get; }
    public static bool IsAvailable { get; }
    public static bool PlayerInSafeArea { get; }
    public static float SafetyFactor { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `InProgress` | `bool` | `true` while a sleep event is playing out. |
| `IsAvailable` | `bool` | `false` when the events could not be set up. |
| `PlayerInSafeArea` | `bool` | `true` when the game last marked the player as being in a safe area. |
| `SafetyFactor` | `float` | How safe the game thinks the player's place is. `1` in a safe area. |

</div>
</div>

### The kinds of sleep event

`SleepEventKind` names every event the game can run during a sleep.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SleepEventKind</code> <span>every event the game can run during a sleep</span></p>

`SleepEventOccurredArgs.Kind` and `SleepEventInterceptArgs.Candidates` give you these values. <span class="tech">(The framework casts the game's sleep event value to this enum by its number.)</span>

<div class="api-pair" markdown="1">

```csharp
public enum SleepEventKind
{
    None,
    Wakeup,
    StealRandomItems,
    StealMoney,
    StealOS,
    StealRM,
    StealMostValuableItem,
    Steal,
    Beat,
    Arrest,
    Quarantine,
    CoveredWithJunk,
    InfectSMV,
    GetCaughtFromCrime,
    Nightmare,
    CrazyNeighbor,
    Invoice
}
```

| Value | Meaning |
|---|---|
| `None` | Nothing happens. |
| `Wakeup` | The player simply wakes up. |
| `StealRandomItems` | A thief takes some of the player's items, picked at random. |
| `StealMoney` | A thief takes some OC and some RM. |
| `StealOS` | A thief takes some OC. |
| `StealRM` | A thief takes some RM. |
| `StealMostValuableItem` | A thief takes the player's most valuable stack of items. |
| `Steal` | A thief takes the most valuable stack, some random items and some money. |
| `Beat` | The player is beaten up and loses health. |
| `Arrest` | The police take the player to prison and remove stolen goods. |
| `Quarantine` | The game names it but does nothing for it. |
| `CoveredWithJunk` | Junk items are added to the player's inventory. |
| `InfectSMV` | The player's mushroom infection (SMV) gets worse and hygiene drops. |
| `GetCaughtFromCrime` | The player is caught for a crime: stolen goods are removed, and prison follows if the player's crimes call for an arrest. |
| `Nightmare` | Scary sounds play, and the player may wake up early. |
| `CrazyNeighbor` | A crazy neighbour may take things, add junk, infect the player or make them dirty. |
| `Invoice` | A bill is added to the player's inventory. |

</div>
</div>

### Knowing what happened

`Occurred` tells you which event the game just ran.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SleepEvents</code> <span>the event that ran</span></p>

The `Occurred` event tells you which sleep event the game just ran, through its `Kind`.

<div class="api-pair" markdown="1">

```csharp
public static class SleepEvents
{
    public static event Action<SleepEventOccurredArgs> Occurred;
}
```

| Event | Gives you | When it fires |
|---|---|---|
| `Occurred` | `SleepEventOccurredArgs` | The game has just run a sleep event. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SleepEventOccurredArgs</code> <span>what the Occurred event tells you</span></p>

The `Occurred` event gives you one of these. `Kind` is the event the game just ran.

<div class="api-pair" markdown="1">

```csharp
public sealed class SleepEventOccurredArgs
{
    public SleepEventKind Kind { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Kind` | `SleepEventKind` | The event the game just ran. |

</div>
</div>

### Cancelling the random event

`Intercept` lets you stop the game from rolling a random event for one sleep.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SleepEvents</code> <span>the roll before it happens</span></p>

The `Intercept` event fires just before the game rolls a random event for a sleep, and lets you cancel that roll.

<div class="api-pair" markdown="1">

```csharp
public static class SleepEvents
{
    public static event Action<SleepEventInterceptArgs> Intercept;
}
```

| Event | Gives you | When it fires |
|---|---|---|
| `Intercept` | `SleepEventInterceptArgs` | A sleep is about to roll a random event. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SleepEventInterceptArgs</code> <span>what the Intercept event tells you</span></p>

The `Intercept` event gives you one of these. Set `Cancel` to `true` to stop the random event for this sleep, and read `Candidates` to see what the game could have rolled.

<div class="api-pair" markdown="1">

```csharp
public sealed class SleepEventInterceptArgs
{
    public List<SleepEventKind> Candidates { get; }
    public bool Cancel { get; set; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Candidates` | `List<SleepEventKind>` | Every event the game could roll for this sleep, in order. |
| `Cancel` | `bool` | Set it to `true` so the game rolls no random event for this sleep. |

</div>
</div>

### Things to know

- `InProgress` is `true` while a sleep event is playing out, and also while the thief or prank sound is playing. So it can be `true` for an event that only played a sound. It is `false` on the main menu, while a save is loading, and with no save loaded. <span class="tech">(It reads `SleepEventController.eventInProgress`. That flag is set while a sleep event runs a coroutine (arrest, get caught, nightmare, crazy neighbour) and while the steal or prank sound coroutine plays.)</span>
- `SafetyFactor` is a number that says how safe the player's sleeping place is, where `1` is fully safe. In a safe area it is `1`. Outside one the game's sleep prompt says "Unsafe" below 0.5 and "Safeish" from 0.5 up. It is `0` when the game has no sleep controller yet, or when the level's logic object is missing and the player is not in a safe area. <span class="tech">(It is `SleepEventController.playerSafetyFactor`, the same value the game's own getter returns. The level's logic object is `info_game_logic`.)</span>
- A bed can make a sleep safe by its own setting, so `PlayerInSafeArea` being `false` does not mean the bed is unsafe. `PlayerInSafeArea` is `false` when the game has no sleep controller yet.
- The kinds follow the game's own list in the same order. The framework turns one into the other by its number. <span class="tech">(The values follow `SleepEventController.SleepEvents`.)</span>
- `StealOS` takes a random 1 to 33 percent of the player's OC (Open Sewer Coins), `StealRM` takes the same share of their RM (Real Money), and `StealMoney` takes from both. See [OC](../glossary.md#oc) and [RM](../glossary.md#rm).
- `Occurred` fires right after the game has started the event. For events that play out over time, the effect has only begun when you are told. For example, an arrest is still on its way when `Occurred` fires.
- `Occurred` also fires for an event that finds nothing to do. A `Steal` when the player has nothing to take still raises it, because the game still runs the event. <span class="tech">(It is raised after `SleepEventController.ExecuteEvent` returns. `ExecuteEvent` runs only from the sleep timer (`WaitingController.CheckEvent`, called by `PlayerStatsTiredness`).)</span>
- `Intercept` fires just before the game picks a random event, when a sleep starts. That includes a passout. It also fires when the player is in a safe area or `Candidates` is empty, even though the game rolls no event in those cases. <span class="tech">(It is raised from a prefix on `SleepEventController.CalculateEvent`, which runs only when a sleep or passout starts, not from save-restore code.)</span>
- Setting `Cancel` to `true` makes the game roll no random event for that sleep. The sleep goes on as if the roll had found nothing. The check for a crime arrest that happens after the roll is not affected.
- If several mods listen, any `Cancel` wins. Once one handler sets it, the later handlers still run and see `Cancel` as `true`. Setting it back to `false` does nothing.
- `Candidates` is a copy. Changing the list does not change what the game rolls.

### Limits

- `PlayerInSafeArea` is the last value a safe-area trigger gave the game. It is not a live check of where the player stands. The game does not clear it when the player leaves, so it can stay `true` after the player has left. <span class="tech">(It reads `SleepEventController.playerInSafeArea`, which a `TriggerSafe` writes. The field is saved with the game, so after a load it keeps the saved value until a trigger writes again. A trigger the player is standing in rewrites it on its first physics tick. A bed's own `safe` flag starts the sleep without the passout events.)</span>
- `Occurred` does not fire when a save is restored. It fires only for events the sleep timer runs.
- You can only cancel the whole roll. You can't choose which event happens. If you want only one kind to be skipped, you cannot do that. You can cancel everything or nothing.
- You can't cancel an event after the game has chosen it. Skipping it then would still show the event's dialogue at wake-up, for an event that did not happen.
- You can't force or replace a sleep event. You can only cancel the roll.
- You can't change how safe the player's place is. The safety number and the safe-area flag can only be read, not changed.
- You can't cancel a crime arrest after sleep.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.sleepevents.log`. Source files: `src/Api/SleepEvents.cs` and `src/Api/SleepEventsPatches.cs`.

Everything not listed in this reference is internal and may change without notice.

### When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game only works safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for the code that handles the events too.
- `InProgress` is safe to read at any time. It gives `false` on the main menu, during loading and with no save loaded.
- Your event handlers run inside the game's own call. If one throws an error, the framework logs it and still runs the other handlers.

### If something fails to set up

The framework changes two of the game's methods when it starts, so it can tell your mod about sleep events. If a game update breaks one of them, only that feature turns off, and the framework writes error lines and a warning to the log. The rest of the framework keeps working. This page has two feature groups.

- `Occurred` uses the feature group `obenseuer.moddingframework.sleepevents`, and `Intercept` uses `obenseuer.moddingframework.sleepevents.intercept`. If the method behind one cannot be changed, only that group is turned off and only its event stops. The other event keeps firing, and `IsAvailable` is `false`. `InProgress`, `PlayerInSafeArea` and `SafetyFactor` keep working, because they read the game's state directly. <span class="tech">(The changed methods are `SleepEventController.ExecuteEvent` (a postfix, which runs after the original) and `SleepEventController.CalculateEvent` (a prefix, which runs before it). If an install throws, both groups are turned off.)</span>
