---
title: Hotels API
parent: API reference
nav_order: 27
---

# Hotels API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.hotels`.
Files: `src/Api/Hotels.cs`, `src/Api/HotelsPatches.cs`.

Everything not listed here is internal.

## If a hook cannot be installed

The framework installs both hooks under one owner. If either is missing, both are removed, `Hotels.IsAvailable` becomes false, and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.hotels.log`. The reads and `Rent` keep working; only the events stop.

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no hotel loaded, or on the main menu, reads return an empty list or `null`, and `Rent` returns `false`.
- Writes return `bool`. The only throw is `ArgumentException` for `days` outside `1` to `MaxDays`.
- Events are raised synchronously from the game's own call, after the game method has finished. A handler that throws is logged and the other handlers still run.

## Hotels

```csharp
public static class Hotels
{
    public const int MaxDays = 7;

    public static List<HotelRoomInfo> All();
    public static HotelRoomInfo Get(string roomId);
    public static RentPayment Price(string roomId, int days);
    public static bool Rent(string roomId, int days);

    public static bool IsAvailable { get; }

    public static event Action<RoomRentedArgs> RoomRented;
    public static event Action<RoomExpiredArgs> RoomExpired;
}
```

- `roomId` is the room's save GUID if it has one, else the world object GUID, else its scene path. A path id changes if the hotel's hierarchy changes.
- `All` returns every room of every hotel in the active scene, free or not.
- `Get` returns `null` for an unknown id.
- `Price` returns the game's own rate for the stay, in both currencies, from the room's rate and the hotel's daily factor. It returns `null` for an unknown id, or while the player already holds a room, because the game then offers no price.
- `Rent` is the game's own `Hotel.RentRoom(index, days)`, so payment, door and bed unlocks, the notification and save data all come from the game. `Rent` returns `false` (and charges nothing) when:
  - the id is unknown, or no game scene is ready (`Cash` readiness);
  - a save is loading or being written;
  - the player already holds a room (`Hotel.PlayerHasRoom`, the game's own rule in `Hotel.CheckAvailability`);
  - the room is not free (the game's `HotelRoom.RentRoomPlayer` returns `false`);
  - the player cannot pay. The game checks both currencies before it charges either.

```csharp
public sealed class HotelRoomInfo
{
    public string Id { get; }
    public string HotelId { get; }      // save GUID, or the scene path
    public string Name { get; }         // the room's roomName, may be empty
    public bool Rented { get; }         // the player holds this room
    public int DaysLeft { get; }        // nights left, 0 when free
    public bool Available { get; }      // free: nobody holds it
}

public sealed class RoomRentedArgs
{
    public string RoomId { get; }
    public string RoomName { get; }
    public int Days { get; }
}

public sealed class RoomExpiredArgs
{
    public string RoomId { get; }
    public string RoomName { get; }
}
```

`Price` returns `RentPayment` from Housing: `Oc` (int) and `Rm` (float).

## Events

- `RoomRented` fires after the game accepts a rental by the player, from either game path (the hotel's `RentRoom(int, int)` and the game's own selection UI). A refused rental raises nothing.
- `RoomExpired` fires when the player's stay ends, on the day the game counts it down. It does not fire for a room held by an NPC.

`IsAvailable` is `false` when the hooks were not installed or the owner was disabled. `Rent` and the reads do not depend on it.

## Known differences from the game's UI

- `Price` is the exact amount `Rent` charges: the room's rate times the hotel's current daily factor and the nights, using the game's own `*` operator. The game's menu shows `Hotel.GetRoomPrice`, which rounds RM to a whole number per night, so for a non-whole RM rate the menu can differ from `Price` by up to 0.5 RM per night. If the daily factor field cannot be read, `Price` falls back to the menu rate.
- `Rent` closes the hotel's open selection menu, because the game's `RentRoom` calls `selector?.Exit()`.
- `Price` and `Rent` throw `ArgumentException` for `days` above `MaxDays` (7), the most nights the game's own menu offers. The game's code would accept more, and `0` would give a free, zero-night stay.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
