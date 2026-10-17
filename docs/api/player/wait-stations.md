---
title: Wait stations
parent: Player
grand_parent: Player life and skills
nav_order: 3
---

# Wait stations

A wait station is a place in the world where the player waits, such as a work station: the player uses it and time passes. These calls list the stations in the scene you are in, check whether one can be used, use one, and tell you when a wait at a station ends.

```csharp
public sealed class WaitStationInfo    // one wait station
{
    public string Id { get; }               // the station's id in the scene
    public WaitKind? Type { get; }          // what the wait is; null when it is not one of the WaitKind values
    public int Minutes { get; }             // how long the wait lasts, in game minutes
    public bool IgnoresNeeds { get; }       // true when the station ignores the player's needs
    public bool CanWait { get; }            // true when the station would let the player wait right now; false without a player
    public bool SatisfiesHunger { get; }    // the wait counts as meeting this need
    public bool SatisfiesThirst { get; }    // the wait counts as meeting this need
    public bool SatisfiesBladder { get; }   // the wait counts as meeting this need
    public bool SatisfiesBowel { get; }     // the wait counts as meeting this need
    public bool SatisfiesTiredness { get; } // the wait counts as meeting this need
    public bool SatisfiesHygiene { get; }   // the wait counts as meeting this need
}

public sealed class WaitStationDoneArgs    // what you are told when a wait at a station ends
{
    public string Id { get; }       // the station's id
    public int Seconds { get; }     // how long the player waited, in game seconds
}

public static class WaitStations
{
    public static List<WaitStationInfo> All();     // every wait station in the scene you are in
    public static bool CanUse(string id);          // true when Use would start the wait; starts nothing
    public static bool Use(string id);             // uses the station, as if the player pressed its Interact key
    public static event Action<WaitStationDoneArgs> Finished;    // a wait at a station has ended
}
```

- **`All()` lists every active wait station in the scene you are in.** Job stations and resident stations are included. It only reads, and gives an empty list when the scene has no station or when a read fails (which is logged once).
  - Technical: it lists every active `InteractableWait`. Job stations (`InteractableWaitWork`) and resident stations (`InteractableWaitResident`) derive from it. It reads the protected `WaitInMinutes` and `type` fields by reflection.
- **`Type` is what kind of wait the station is, or `null` when it is a kind that `WaitKind` does not have.** To start the same wait yourself, call `PlayerState.Wait(Minutes * 60, Type.Value)` when `Type` has a value.
  - Technical: it is the station's `WaitingController.Type`. `None`, `Sleeping`, `Passout` and `Arrest` are not in `WaitKind`, so they come back `null`.
- **`IgnoresNeeds` and the `Satisfies...` values describe how the wait treats the player's needs.** A resident station looks up its resident after the wait starts, so its `Satisfies...` values can change during a wait. Read them when no wait is running. The station's negative effects are not listed.
  - Technical: `IgnoresNeeds` reads the protected `ignorePlayerNeeds` field by reflection, and `FindResidentAndApartment` runs after the wait starts.
- **`CanUse(id)` and `Use(id)` make the same checks, in this order.** The id is not empty; no save is loading or being written; the game's waiting controller exists; no level change is running; waiting is not blocked; the player is not asleep or waiting; the game's stats, screens, clock and use text exist; the id matches a station; the station's interaction is not turned off; and the station says the player can wait. `Use` then does what pressing the station's Interact key does. Neither one checks distance, line of sight or skill, and `Use` does not check skill on a job station.
  - Technical: the checks need `PlayerStats`, `GameUIController`, `GameController`, `TimeOfDayAzure` and `UseText`. The station checks are `IsInteractDisabled()` being false and `CanWait()` being true. `Use` calls the station's `Interact()`.
- **`Finished` fires for every wait that a station starts, including a wait the player wakes from early.** `Seconds` is the time the game reports when it wakes the player, not the planned length. It does not fire for 0 or less, or while a save is loading or being written. At a job station it fires before the pay is added and before `Jobs.Worked` fires.
  - Technical: it is a postfix on `InteractableWait.WaitDone`. The job station's `WaitDone` calls the base method first.
