---
title: Door events
parent: Doors
grand_parent: Places and travel
nav_order: 5
---

# Door events

The two events tell you when a door has finished moving.

```csharp
public static class Doors
{
    public static event Action<DoorChangedArgs> Opened;   // a door has just finished opening
    public static event Action<DoorChangedArgs> Closed;   // a door has just finished closing
}

public sealed class DoorChangedArgs    // what happened
{
    public string Id { get; }   // the same id as DoorInfo.Id
}
```

- **`Opened` and `Closed` fire when the door reaches fully open or fully shut, not when it starts to move.**
  - Technical: they come from the game's private `OnFullyOpen` and `OnFullyClosed`, which only `Door.Update` calls.
- **A door that is stopped halfway fires neither event.**
  - Technical: `Door.Stop` does not reach `OnFullyOpen` or `OnFullyClosed`.
- **Loading a save does not fire the events by itself.** A move that a save was in the middle of finishes after the load, and fires then.
- **The events do not tell you who opened the door.** The game does not record who started a move, and many things can start one: the player, NPCs, elevators, relays and delayed key unlocks.
  - Technical: callers include `Interact`, NPC `ForceOpen`, elevators, relays and a delayed key unlock.
- **Lock changes are on the [Interaction page](../interaction.md).** Use `Locks.Locked` and `Locks.Unlocked` for those.
