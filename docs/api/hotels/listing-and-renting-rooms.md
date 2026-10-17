---
title: Listing and renting rooms
parent: Hotels
grand_parent: Places and travel
nav_order: 2
---

# Listing and renting rooms

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

`Hotels` lists and rents the hotel rooms in the scene you are in. Each room is a `HotelRoomInfo`, a snapshot of it at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Hotels</code> <span>list, price, rent and watch hotel rooms</span></p>
<div class="api-pair" markdown="1">

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

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<HotelRoomInfo>` | Every room of every hotel in the scene you are in, free or not. |
| `Get(string roomId)` | `HotelRoomInfo` | The room with this id. It is `null` when there is none. |
| `Price(string roomId, int days)` | `RentPayment` | What a stay costs. It is `null` when no price is offered. |
| `Rent(string roomId, int days)` | `bool` | Rents the room to the player. It is `false` when the room was not rented. |

| Property | Type | What it means |
|---|---|---|
| `MaxDays` | `int` | The most nights you can rent or price in one go. It is 7. |
| `IsAvailable` | `bool` | `false` when the rent and expiry events could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `RoomRented` | `RoomRentedArgs` | The player has just rented a room. |
| `RoomExpired` | `RoomExpiredArgs` | The player's stay has just run out. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>HotelRoomInfo</code> <span>a snapshot of one hotel room</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class HotelRoomInfo
{
    public string Id { get; }
    public string HotelId { get; }
    public string Name { get; }
    public bool Rented { get; }
    public int DaysLeft { get; }
    public bool Available { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | This room's id. Use it with `Get`, `Price` and `Rent`. |
| `HotelId` | `string` | The id of the hotel the room is in. |
| `Name` | `string` | The room's name. It can be empty. |
| `Rented` | `bool` | `true` when the player holds this room. |
| `DaysLeft` | `int` | The nights left on the player's stay. It is 0 when the room is free. |
| `Available` | `bool` | `true` when the room is free and nobody holds it. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RoomRentedArgs</code> <span>what RoomRented gives you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class RoomRentedArgs
{
    public string RoomId { get; }
    public string RoomName { get; }
    public int Days { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `RoomId` | `string` | The room's id. |
| `RoomName` | `string` | The room's name. |
| `Days` | `int` | The number of nights rented. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RoomExpiredArgs</code> <span>what RoomExpired gives you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class RoomExpiredArgs
{
    public string RoomId { get; }
    public string RoomName { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `RoomId` | `string` | The room's id. |
| `RoomName` | `string` | The room's name. |

</div>
</div>

## Things to know

- `Price` gives a `RentPayment`, which is on the [Housing](../housing.md) page. It has `Oc` (a whole number of Open Sewer Coins) and `Rm` (Real Money, which can have a fraction). See [OC](../../glossary.md#oc) and [RM](../../glossary.md#rm).
- A room's id is its save id if it has one, otherwise its world id, otherwise its place in the scene. The id of a room with only a place in the scene changes if the hotel's objects are rearranged, so do not store such an id between sessions. <span class="tech">(The id is the room's save GUID, else the world object GUID, else its scene path.)</span>
- `All()` lists every room of every hotel in the scene you are in, whether it is free or not. `Get(roomId)` gives `null` for an id that no room has.
- `Rented` is `true` only for the room the player holds. `Available` is `true` only for a free room. A room held by someone else is neither. The player holds at most one room at a time. <span class="tech">(`Rented` is the room's `rentedForPlayer`. `Available` is true when the room's `reservedDays` is -1.)</span>
- `Price(roomId, days)` gives you what a stay would cost, in both currencies. You get `null` when no room has that id, and also while the player already holds a room, because the game then offers no price. <span class="tech">(The price is the room's `rentPrice` times the hotel's daily price factor times the nights, using the game's own `*` for money.)</span>
- `Price` is the exact amount `Rent` takes, but the game's menu can show a slightly different RM number. The menu rounds RM to a whole number for each night. For an RM rate that is not a whole number, the menu can differ from `Price` by up to 0.5 RM per night. If the framework can't read the daily price factor, `Price` gives the menu's rate instead. <span class="tech">(The menu uses `Hotel.GetRoomPrice`. The factor is the private field `dailyPricefactor`, read with `AccessTools.Field`.)</span>
- `Rent(roomId, days)` rents the room the way the game does, so payment, the door, the bed, the notification and the save all work as in the game. It closes the hotel's selection menu if that is open. <span class="tech">(It calls the game's `Hotel.RentRoom(index, days)`, which calls `selector?.Exit()`.)</span>
- `RoomRented` fires when the game accepts a rental by the player. It fires for a rental from your mod and also for one made through the game's own hotel menu. A rental that is refused raises nothing. <span class="tech">(It fires after `HotelRoom.RentRoomPlayer` returns `true`, which covers both the hotel's `RentRoom(int, int)` and the game's selection menu.)</span>
- `RoomExpired` fires when the player's stay ends, on the day the game counts the last night off. It does not fire for a room held by someone else. <span class="tech">(It fires after `HotelRoom.CountDays` when `rentedForPlayer` changed from `true` to `false`.)</span>
- `IsAvailable` is `false` when the rent and expiry events could not be set up. The reads and `Rent` keep working when it is `false`. See [If something fails to set up](details.md#if-something-fails-to-set-up).

## Limits

- `Rent` returns `false` and takes no money when no room has that id, or when the game is not ready yet (for example on the main menu). <span class="tech">("Not ready" is `Cash.Ready()` being false. The game's own rule for holding a room is `Hotel.PlayerHasRoom`, and the free check is `HotelRoom.RentRoomPlayer` returning `false`.)</span>
- `Rent` also returns `false` and takes no money while a save is being loaded or written, while the player already holds a room, when the room is not free, or when the player can't pay. The game checks both currencies before it takes either.
- `days` must be from 1 to 7 (`MaxDays`), or `Price` and `Rent` throw an `ArgumentException`. Seven is the most nights the game's own menu offers. The game would take more, and `0` would give a free stay of no nights, so the framework refuses both.
