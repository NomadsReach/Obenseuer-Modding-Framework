---
title: Common tasks
parent: Hotels
grand_parent: Places and travel
nav_order: 1
---

# Common tasks

Rent a room for two nights. `Rent` returns `false` when the game does not rent the room, so check the result.

```csharp
foreach (HotelRoomInfo room in Hotels.All())
{
    if (!room.Available) continue;
    if (Hotels.Rent(room.Id, 2)) Log.Info("Rented " + room.Name);
    break;
}
```

Find the room the player holds right now and say how many nights are left.

```csharp
foreach (HotelRoomInfo room in Hotels.All())
    if (room.Rented) Log.Info(room.Name + " has " + room.DaysLeft + " nights left");
```

Look at one room by its id. `Get` gives `null` when there is no room with that id.

```csharp
HotelRoomInfo room = Hotels.Get("some-room-id");
if (room != null) Log.Info(room.Name + " is in hotel " + room.HotelId);
```
