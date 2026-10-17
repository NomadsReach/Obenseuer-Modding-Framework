---
title: Hotels
parent: Places and travel
grand_parent: API reference
has_children: true
nav_order: 3
---

# Hotels

Status: experimental.

## What it is

Hotels in the game rent out rooms by the night. The player pays for a room, gets a key to its door and a bed to sleep in, and loses the room when the nights run out. This API lets a mod list the hotel rooms in the scene you are in, see which are free and how many nights are left on the player's room, ask what a stay costs, rent a room for the player, and get told when a room is rented or runs out.

## When to use it

- Show the price of a room, or the nights left on the player's room.
- Rent a room for the player from your own menu or quest.
- Give the player something when they rent a room, or take it away when the stay ends.

## Quick example

This mod logs every room when a save is loaded, with its price for one night, and writes a line each time the player rents a room or a stay ends.

```csharp
using ObenseuerCore;

[ModInfo("example.hotelwatch", "Hotel Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class HotelWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Hotels.RoomRented += args => Log.Info("Rented " + args.RoomName + " for " + args.Days + " nights");
        Hotels.RoomExpired += args => Log.Info("The stay in " + args.RoomName + " is over");
        Events.WhenReady(() =>
        {
            foreach (HotelRoomInfo room in Hotels.All())
            {
                RentPayment price = Hotels.Price(room.Id, 1);
                Log.Info(room.Name + ": free " + room.Available + ", nights left " + room.DaysLeft + ", one night " + (price == null ? "no price" : price.Oc + " OC + " + price.Rm + " RM"));
            }
        });
    }
}
```

`RoomRented` and `RoomExpired` are events: the framework calls your code each time they happen. `WhenReady` runs your code once a save is loaded (see [Using events](../guides/getting-started/using-events.md)). `Price` gives the cost in both of the game's currencies, OC and RM (see [Money](economy/money-cash.md#money-cash)).
