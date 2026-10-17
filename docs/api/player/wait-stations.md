---
title: Wait stations
parent: Player
grand_parent: Player life and skills
nav_order: 3
---

# Wait stations

A wait station is a place in the world where the player waits, such as a work station: the player uses it and time passes. These calls list the stations in the scene you are in, check whether one can be used, use one, and tell you when a wait at a station ends.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WaitStationInfo</code> <span>one wait station</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class WaitStationInfo
{
    public string Id { get; }
    public WaitKind? Type { get; }
    public int Minutes { get; }
    public bool IgnoresNeeds { get; }
    public bool CanWait { get; }
    public bool SatisfiesHunger { get; }
    public bool SatisfiesThirst { get; }
    public bool SatisfiesBladder { get; }
    public bool SatisfiesBowel { get; }
    public bool SatisfiesTiredness { get; }
    public bool SatisfiesHygiene { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The station's id in the scene. |
| `Type` | `WaitKind?` | What the wait is. `null` when it is not one of the `WaitKind` values. |
| `Minutes` | `int` | How long the wait lasts, in game minutes. |
| `IgnoresNeeds` | `bool` | `true` when the station ignores the player's needs. |
| `CanWait` | `bool` | `true` when the station would let the player wait right now. `false` without a player. |
| `SatisfiesHunger` | `bool` | The wait counts as meeting this need. |
| `SatisfiesThirst` | `bool` | The wait counts as meeting this need. |
| `SatisfiesBladder` | `bool` | The wait counts as meeting this need. |
| `SatisfiesBowel` | `bool` | The wait counts as meeting this need. |
| `SatisfiesTiredness` | `bool` | The wait counts as meeting this need. |
| `SatisfiesHygiene` | `bool` | The wait counts as meeting this need. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WaitStationDoneArgs</code> <span>what you are told when a wait at a station ends</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class WaitStationDoneArgs
{
    public string Id { get; }
    public int Seconds { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The station's id. |
| `Seconds` | `int` | How long the player waited, in game seconds. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>WaitStations</code> <span>list, check and use wait stations</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class WaitStations
{
    public static List<WaitStationInfo> All();
    public static bool CanUse(string id);
    public static bool Use(string id);
    public static event Action<WaitStationDoneArgs> Finished;
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<WaitStationInfo>` | Every wait station in the scene you are in. |
| `CanUse(string id)` | `bool` | `true` when `Use` would start the wait. Starts nothing. |
| `Use(string id)` | `bool` | Uses the station, as if the player pressed its Interact key. |

| Event | Gives you | When it fires |
|---|---|---|
| `Finished` | `WaitStationDoneArgs` | A wait at a station has ended. |

</div>
</div>

## Things to know

- `All()` lists every active wait station in the scene you are in. Job stations and resident stations are included. It only reads, and gives an empty list when the scene has no station or when a read fails (which is logged once). <span class="tech">(It lists every active `InteractableWait`. Job stations (`InteractableWaitWork`) and resident stations (`InteractableWaitResident`) derive from it. It reads the protected `WaitInMinutes` and `type` fields by reflection.)</span>
- `Type` is what kind of wait the station is, or `null` when it is a kind that `WaitKind` does not have. To start the same wait yourself, call `PlayerState.Wait(Minutes * 60, Type.Value)` when `Type` has a value. <span class="tech">(It is the station's `WaitingController.Type`. `None`, `Sleeping`, `Passout` and `Arrest` are not in `WaitKind`, so they come back `null`.)</span>
- `IgnoresNeeds` and the `Satisfies...` values describe how the wait treats the player's needs. A resident station looks up its resident after the wait starts, so its `Satisfies...` values can change during a wait. Read them when no wait is running. <span class="tech">(`IgnoresNeeds` reads the protected `ignorePlayerNeeds` field by reflection, and `FindResidentAndApartment` runs after the wait starts.)</span>
- `CanUse(id)` and `Use(id)` make the same checks, in this order: the id is not empty; no save is loading or being written; the game's waiting controller exists; no level change is running; waiting is not blocked; the player is not asleep or waiting; the game's stats, screens, clock and use text exist; the id matches a station; the station's interaction is not turned off; and the station says the player can wait. `Use` then does what pressing the station's Interact key does. <span class="tech">(The checks need `PlayerStats`, `GameUIController`, `GameController`, `TimeOfDayAzure` and `UseText`. The station checks are `IsInteractDisabled()` being false and `CanWait()` being true. `Use` calls the station's `Interact()`.)</span>
- `Finished` fires for every wait that a station starts, including a wait the player wakes from early. `Seconds` is the time the game reports when it wakes the player, not the planned length. It does not fire for 0 or less, or while a save is loading or being written. At a job station it fires before the pay is added and before `Jobs.Worked` fires. <span class="tech">(It is a postfix on `InteractableWait.WaitDone`. The job station's `WaitDone` calls the base method first.)</span>

## Limits

- Neither `CanUse` nor `Use` checks distance, line of sight or skill, and `Use` does not check skill on a job station.
- The station's negative effects are not listed in `WaitStationInfo`.
