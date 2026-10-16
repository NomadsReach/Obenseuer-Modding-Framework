---
title: Seats API
parent: API reference
nav_order: 69
---

# Seats API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.seats`.
Files: `src/Api/Seats.cs`, `src/Api/SeatsPatches.cs`.

This area wraps the chair object (`InteractableChair`) and its subclass `Toilet`: which seats exist, whether each is taken, and the player's sit and stand. It does not cover NPC scheduling (`Act_sit`), the furniture trigger that sets the occupied flag (`ChairOccupiedTrigger`), or the card-game and contest flows that drive chairs with `DisableStopSit`.

## If a hook cannot be installed

The framework installs four hooks: the private `InteractableChair.Sit(bool, bool)`, `StopSitEnd()`, `NPCSit(GameObject, bool)` and `StopNPCSit(GameObject)`. If any is missing, the owner is disabled and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.seats.log`. `All`, `Get`, `Sit` and `Leave` keep working; `Sat` and `Left` stop.

## Threading and safety

- Main thread only.
- `Seats.IsAvailable` is false when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list or `null`, and `Sit` and `Leave` return `false`.
- Nothing throws. A null or empty `id` finds nothing.
- `Sit` and `Leave` refuse with `false` when the game would refuse the use press. The facade's own gates refuse silently. A toilet's lid and bucket warnings still show, because they come from the game's `Toilet.Use`.
- Events are raised synchronously after the game has changed the state. Nothing is raised while a save is loading, or for the sit a save restores. A handler that throws is logged and the other handlers still run.

## How Sit and Leave follow the game

A use press on a chair runs `GameController.InteractControls`, which calls `InteractObjects.UseInteractable` and then `InteractableChair.Interact`. `Interact` calls `SitToggle`, which refuses an occupied chair with a warning and otherwise calls `Sit`.

`Seats.Sit(id)` runs the same gates first, so a refusal is silent, then calls the chair's public `Interact()`. The virtual call means a toilet keeps its own lid and bucket checks. The result is read back from `PlayerSittingHere`, so a refusal inside the game (for example a camera move still running, which `Interact` ignores) also returns `false`.

Gates for `Sit`:

- the chair is not found, or the player is already seated in any chair (`InteractableChair.PlayerIsSitting`);
- a save is loading or saving (`SaveData.Busy`);
- the player is busy: the menu, a shop, the pause menu, the console or a dialogue is open, keys are disabled or the game is paused (`GameController`), or the player is asleep (`PlayerState.IsSleeping`);
- movement is disabled, for example on a ladder or while lockpicking (`OpenSewerCharacterController.ControlsDisabled`);
- the chair is interact-disabled (`Interactable.IsInteractDisabled`, `Interactable`), or occupied (`Occupied`);
- the camera is more than 1.5 m from the chair's transform (`InteractObjects.useDistance`, `InteractObjects`).

`Seats.Leave()` uses the same busy gate and then `Interact()` on the seated chair. It is refused while `DisableStopSit` is set (contests and card tables) or while the chair's camera move is still running.

## API

```csharp
public static class Seats
{
    public static List<SeatInfo> All();
    public static SeatInfo Get(string id);
    public static bool Sit(string id);
    public static bool Leave();

    public static bool IsAvailable { get; }
    public static event Action<SeatArgs> Sat;
    public static event Action<SeatArgs> Left;
}

public sealed class SeatInfo
{
    public string Id { get; }              // the save GUID, else the scene path
    public bool IsOccupied { get; }        // an NPC or furniture holds the seat, or the player sits in it
    public bool OccupiedByPlayer { get; }  // the player is sitting in this chair
    public Vector3 Position { get; }       // the chair's transform position
    public bool IsToilet { get; }          // the chair is a Toilet
}

public sealed class SeatArgs              // payload of Sat and Left
{
    public string Id { get; }              // as SeatInfo.Id
    public bool ByPlayer { get; }          // true for the player, false for an NPC
}
```

- `All` lists every active `InteractableChair` in the active scene, toilets included.
- `Get(id)` returns one `SeatInfo`, or `null` when no seat has that id.
- `Sit(id)` makes the player sit in that chair. It returns `true` when the player is seated in it afterwards.
- `Leave()` starts the stand-up from the chair the player is in, and returns `true` once it has started. It returns `false` when that chair is a `Toilet` whose `enabled` is `false`, because the game's `Toilet.Use` only shows a warning there and does not stand the player up. `PlayerSittingHere` clears, and `Left` fires, when the camera move ends, so the player may still show as seated right after the call.
- `Sat` fires after a player sits (`ByPlayer` true) or an NPC is seated live (`ByPlayer` false). `Left` fires when the player stands up, or when an NPC is told to stand, before the game's short delay before the NPC is released.

## Not wrapped

- The occupied flag setter (`InteractableChair.ChangeOccupied`). It is written every physics frame by `ChairOccupiedTrigger`, so a write would be overwritten at once.
- NPCs placed into or removed from a chair (`NPCSit`, `StopNPCSit`). They are driven by the NPC schedules and the tenement spawn. Only the live schedule path raises `Sat` and `Left`.
- `DisableStopSit`, which the card and contest flows use to block standing up.
- The camera offsets and sitting positions (`cameraPosition`, `upChairOffset`, `downChairOffset`).
- The toilet's flush, bucket, paper and urinal (`Toilet`).
