---
title: Listing and renting rooms
parent: Hotels
grand_parent: Places and travel
nav_order: 2
---

# Listing and renting rooms

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`Hotels` lists and rents the hotel rooms in the scene you are in. Each room is a `HotelRoomInfo`, a snapshot of it at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public static class Hotels
{
    public const int MaxDays = 7;    // the most nights you can rent or price in one go

    public static List<HotelRoomInfo> All();                 // every room of every hotel in the scene you are in, free or not
    public static HotelRoomInfo Get(string roomId);          // the room with this id; null when there is none
    public static RentPayment Price(string roomId, int days);   // what a stay costs; null when no price is offered
    public static bool Rent(string roomId, int days);        // rents the room to the player; false when it was not rented

    public static bool IsAvailable { get; }                  // false when the rent and expiry events could not be set up

    public static event Action<RoomRentedArgs> RoomRented;   // the player has just rented a room
    public static event Action<RoomExpiredArgs> RoomExpired; // the player's stay has just run out
}

public sealed class HotelRoomInfo    // a snapshot of one hotel room
{
    public string Id { get; }          // this room's id; use it with Get, Price and Rent
    public string HotelId { get; }     // the id of the hotel the room is in
    public string Name { get; }        // the room's name; can be empty
    public bool Rented { get; }        // true when the player holds this room
    public int DaysLeft { get; }       // nights left on the player's stay; 0 when the room is free
    public bool Available { get; }     // true when the room is free and nobody holds it
}

public sealed class RoomRentedArgs    // what you are told when the player rents a room
{
    public string RoomId { get; }      // the room's id
    public string RoomName { get; }    // the room's name
    public int Days { get; }           // the number of nights rented
}

public sealed class RoomExpiredArgs   // what you are told when a stay runs out
{
    public string RoomId { get; }      // the room's id
    public string RoomName { get; }    // the room's name
}
```

`Price` gives a `RentPayment`, which is on the [Housing](../housing.md) page. It has `Oc` (a whole number of Open Sewer Coins) and `Rm` (Real Money, which can have a fraction). See [OC](../../glossary.md#oc) and [RM](../../glossary.md#rm).

- **A room's id is its save id if it has one, otherwise its world id, otherwise its place in the scene.** The id of a room with only a place in the scene changes if the hotel's objects are rearranged, so do not store such an id between sessions.
  - Technical: the id is the room's save GUID, else the world object GUID, else its scene path.
- **`All()` lists every room of every hotel in the scene you are in, whether it is free or not.** `Get(roomId)` gives `null` for an id that no room has.
- **`Rented` is `true` only for the room the player holds. `Available` is `true` only for a free room.** A room held by someone else is neither. The player holds at most one room at a time.
  - Technical: `Rented` is the room's `rentedForPlayer`. `Available` is true when the room's `reservedDays` is -1.
- **`Price(roomId, days)` gives you what a stay would cost, in both currencies.** You get `null` when no room has that id, and also while the player already holds a room, because the game then offers no price.
  - Technical: the price is the room's `rentPrice` times the hotel's daily price factor times the nights, using the game's own `*` for money.
- **`Price` is the exact amount `Rent` takes, but the game's menu can show a slightly different RM number.** The menu rounds RM to a whole number for each night. For an RM rate that is not a whole number, the menu can differ from `Price` by up to 0.5 RM per night. If the framework can't read the daily price factor, `Price` gives the menu's rate instead.
  - Technical: the menu uses `Hotel.GetRoomPrice`. The factor is the private field `dailyPricefactor`, read with `AccessTools.Field`.
- **`Rent(roomId, days)` rents the room the way the game does, so payment, the door, the bed, the notification and the save all work as in the game.** It closes the hotel's selection menu if that is open.
  - Technical: it calls the game's `Hotel.RentRoom(index, days)`, which calls `selector?.Exit()`.
- **`Rent` returns `false` and takes no money when the room can't be rented.** That happens when:
  - no room has that id, or the game is not ready yet (for example on the main menu);
  - a save is being loaded or written;
  - the player already holds a room;
  - the room is not free;
  - the player can't pay. The game checks both currencies before it takes either.
  - Technical: "not ready" is `Cash.Ready()` being false. The game's own rule for holding a room is `Hotel.PlayerHasRoom`, and the free check is `HotelRoom.RentRoomPlayer` returning `false`.
- **`days` must be from 1 to 7 (`MaxDays`), or `Price` and `Rent` throw an `ArgumentException`.** Seven is the most nights the game's own menu offers. The game would take more, and `0` would give a free stay of no nights, so the framework refuses both.
- **`RoomRented` fires when the game accepts a rental by the player.** It fires for a rental from your mod and also for one made through the game's own hotel menu. A rental that is refused raises nothing.
  - Technical: it fires after `HotelRoom.RentRoomPlayer` returns `true`, which covers both the hotel's `RentRoom(int, int)` and the game's selection menu.
- **`RoomExpired` fires when the player's stay ends, on the day the game counts the last night off.** It does not fire for a room held by someone else.
  - Technical: it fires after `HotelRoom.CountDays` when `rentedForPlayer` changed from `true` to `false`.
- **`IsAvailable` is `false` when the rent and expiry events could not be set up.** The reads and `Rent` keep working when it is `false`. See [If something fails to set up](details.md#if-something-fails-to-set-up).
