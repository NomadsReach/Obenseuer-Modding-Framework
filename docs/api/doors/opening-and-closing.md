---
title: Opening and closing
parent: Doors
grand_parent: Places and travel
nav_order: 3
---

# Opening and closing

You can open or close a door from your mod. Both calls give back a `bool` that tells you whether it worked, and neither throws an error.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Doors</code> <span>opens and closes doors</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Doors
{
    public static bool Open(string id);
    public static bool Close(string id);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Open(string id)` | `bool` | Opens the door. `true` when it is now open or opening. |
| `Close(string id)` | `bool` | Closes the door. `true` when it is closed or closing. |

</div>
</div>

## Things to know

- `Open` gives `true` when the door is open after the call. <span class="tech">(The result is `Door.doorIsOpen` after the call. A door with `noOppositeDirection` and `invertOppositeDirection` set opens through the game's `OpenOpposite`. `IsOpen` and the events still follow `doorIsOpen`.)</span>
- `Close` does not check locks, just like the game's own close. It gives `true` when the door is already closed or is now closing. <span class="tech">(It calls the public `Door.Close()`, which does nothing at position 0. The result is true when `doorIsOpen` is false or `DoorIsOpenOrAjar()` is true.)</span>
- Do not call `IsOpen` on every frame to wait for a change. Each call searches every door in the scene. Listen for `Opened` and `Closed` instead.

## Limits

- `Open` does not open a locked door, and it gives `false` for one. To open a locked door, unlock it first. The `Locks` calls are on the [Interaction page](../interaction.md). Otherwise the game would use the player's key, start the player's lockpick game, or play the "locked" sound. <span class="tech">(It returns `false` without touching the door when `Door.Locked` is true. Otherwise it calls the game's public `Door.Open()`. Without the check, `Lock.Use` would use the key or start the lockpick, and a door that is already closing would open again even when locked. A door that opens through a key goes through the game's unlock, so `Locks.Unlocked` fires as well.)</span>
- Both give `false` while a save is loading or being written, and for an id the game does not know. <span class="tech">(Both check `SaveData.Busy` first.)</span>
- `Close` gives `false` in the very first moment of an opening, when the door has not started to move yet and the game ignores the close.
