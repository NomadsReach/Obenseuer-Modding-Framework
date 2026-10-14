---
title: Doors API
parent: API reference
nav_order: 17
---

# Doors API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.doors`.
Files: `src/Api/Doors.cs`, `src/Api/DoorSchedules.cs`, `src/Api/DoorsPatches.cs`.

Everything not listed here is internal.

## If a hook cannot be installed

The framework installs the `Opened` and `Closed` hooks under one owner. If either hook is missing, both are removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.doors")` becomes true, and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.doors.log`. The reads, `Open` and `Close` keep working; only the events stop.

## Threading and safety

- Main thread only.
- `Doors.IsAvailable` is false when the hooks were not installed or the owner was disabled.
- Every member is safe at any time. With no scene loaded, or on the main menu, reads return an empty list, `null` or `false`.
- Writes return `bool`. Nothing throws to the caller. An unknown or empty id returns `false`.
- `Opened` and `Closed` are raised synchronously from the game's own door update, so a handler that throws is logged and the other handlers still run.

## Doors

```csharp
public static class Doors
{
    public static List<DoorInfo> All();
    public static DoorInfo Get(string id);
    public static bool IsOpen(string id);
    public static bool Open(string id);
    public static bool Close(string id);
    public static List<DoorOpenTime> OpeningTimes(string id);                       // null when the door has no schedule
    public static bool SetOpeningTimes(string id, IList<DoorOpenTime> times);

    public static event Action<DoorChangedArgs> Opened;
    public static event Action<DoorChangedArgs> Closed;

    public static bool IsAvailable { get; }
}

public sealed class DoorInfo
{
    public string Id { get; }              // the door's GUID, or a scene path when it has none
    public string Name { get; }            // the GameObject name
    public bool IsOpen { get; }            // Door.doorIsOpen when the list was built
    public bool Moving { get; }            // Door.moving: the door is swinging or sliding
    public Vector3 Position { get; }       // Door's transform.position when the list was built
    public Vector3? StandPosition { get; } // the linked Waypoint_Door's position; null when none
}

public sealed class DoorOpenTime
{
    public DoorOpenTime(DayOfWeek day, int opens, int closes);
    public DayOfWeek Day { get; }
    public int Opens { get; }              // first hour the door is open, 0 to 23
    public int Closes { get; }             // hour the door shuts, above Opens and at most 24. The door is open from Opens up to, not including, Closes.
}

public sealed class DoorChangedArgs
{
    public string Id { get; }              // the same id as DoorInfo.Id
}
```

- `id` is the door's `GUID` field. A door with an empty GUID uses the scene path (scene name plus child path), so it is still addressable. That id changes if the hierarchy is edited.
- `All` returns every active `Door` in the active scene, including doors without a GUID. Inactive GameObjects are left out, as in `FindObjectsByType`'s default.
- `StandPosition` is the position of the scene waypoint that links to the door (the game's `Waypoint_Door`). The game's NPCs wait at that waypoint while the door opens. It is `null` when no active waypoint links the door, and when two waypoints link it, the one found first is used. `Position` is not a standing spot.
- `Position` is the door's `transform.position`, read when the list is built, so it does not follow the door while it moves. It is the GameObject's origin with no offset added. A swinging door turns around that origin, and a sliding door's origin moves along its slide. The game has no standing spot or front offset of its own for a door, and the framework does not check where the origin sits against the door frame, so do not read it as the spot in front of the door. The spot NPCs use is the linked waypoint that `StandPosition` returns (`Waypoint_Door.door`).
- `Get` returns `null` for an unknown id. `IsOpen` returns `false` for an unknown id.
- `IsOpen` reads `Door.doorIsOpen`, the game's own open flag. It turns `true` when an open starts and turns `false` when the door is fully closed. It stays `true` while the door is closing.
- `Open` returns `false` without touching the door when `Door.Locked` is true. Otherwise it calls the game's public `Door.Open()`. The check exists because the game's `Lock.Use` would use the player's key, start the player's lockpick minigame, or play the locked sound, and a door that is already closing would reopen even when locked. To open a locked door, unlock it with `Locks` first.
  Returns `true` when `IsOpen` is `true` after the call.
- `Close` calls the game's public `Door.Close()`. It does not check locks, the same as the game's own call. Returns `true` when the door is already closed or is now closing. Returns `false` only in the first frame of an open (position 0), where `Close` does nothing.
- `Opened` and `Closed` fire when the door reaches fully open or fully closed, not when the move starts. An interrupted move (`Door.Stop`) does not fire them. Loading a save does not fire them by itself. A move that a save resumes is finished by `Update` and fires then.
- Events have no `byPlayer`. The game does not record who started a move, and it has several callers (`Interact`, NPC `ForceOpen`, elevators, relays, delayed key unlock), so the initiator is not reliably knowable.
- `OpeningTimes` reads the game's `DoorSchedule.OpenTimes` for the schedule that controls the door. A door has opening hours only when a `DoorSchedule` lists it; most doors have none and `OpeningTimes` returns `null`. A schedule with an empty list returns an empty list, and its doors stay locked all week. A door in more than one schedule reports the first one found. The days are .NET `DayOfWeek` values: the game counts Monday as 0, and the framework converts. One day can have several windows.
- `SetOpeningTimes` replaces the whole list of every schedule that lists the door, and returns `true` when at least one schedule was changed. It returns `false` for an unknown id, a door with no schedule, a null list, a null entry, an entry with `Opens` outside 0 to 23, `Closes` not above `Opens` or above 24 (a window cannot pass midnight; add a second window on the next day instead), and while a save loads or saves (`SaveData.Busy`). An empty list is allowed and keeps the doors locked.
- The schedule drives locks, not the door's swing. At the next game minute the game locks the schedule's doors outside the hours and unlocks them inside (`Door.Lock`, `Door.Unlock`), so the change shows within about a game minute and not at once. A lock the player has opened with a key or a lockpick is closed again only when the schedule moves from open to shut. `Locks.Locked` and `Locks.Unlocked` report those changes.
- One schedule often serves several doors and the shop sign texts that go with them. Setting the hours for one door sets them for all the doors of that schedule. The hours are not saved: a reload brings back the scene's own hours until you set them again. The lock state the schedule produced is saved with the doors.
- Elevator car and floor doors are `Door` objects that the elevator opens and closes. They appear in `All`, and they raise `Opened` and `Closed` when they move.
- Lock changes are not in this area. Use `Locks` for `Locked` and `Unlocked`. An `Open` that unlocks a door with a key goes through the game's `Unlock`, so `Locks.Unlocked` fires as well.

## Ceilings and risks

- Each call scans `FindObjectsByType<Door>`. Fine for occasional calls; do not poll `IsOpen` every frame, subscribe to `Opened`/`Closed` instead.
- Two doors with the same GUID resolve to the first one found. GUIDs are unique per scene, not across scenes.
- Completion events depend on `OnFullyOpen` and `OnFullyClosed` being the only paths that finish a move. Only the game's `Door.Update` calls them.
- Doors with `noOppositeDirection` and `invertOppositeDirection` set route `Open` through the game's `OpenOpposite`. `IsOpen` and the events still follow `doorIsOpen`.
- The private `OnFullyOpen` and `OnFullyClosed` are patched by name. The game code has one overload of each.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
