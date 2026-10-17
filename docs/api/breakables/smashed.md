---
title: Smashed
parent: Breakables
grand_parent: Objects and devices
nav_order: 4
---

# Smashed

Some objects spawn an item when a hit uses up their health. They are not breakables, so `Broken` never fires for them and `Smashed` never fires for a breakable. `Smashed` tells you about the hit and what the object would drop.

```csharp
public static class Breakables
{
    public static event Action<SmashedArgs> Smashed;    // a smashable object has just been destroyed by a hit
}

public sealed class SmashedArgs    // what Smashed tells you
{
    public string Id { get; }            // the object's id: its saved number, or a scene path when it has none
    public Vector3 Position { get; }     // where the object was, in world coordinates, just before it broke
    public bool IsAnimal { get; }        // true when the object is an animal rather than a thing
    public int SpawnItemId { get; }      // the id of the item it drops; -1 when it drops no item
}
```

- **`Smashed` fires when a hit takes the object's health from above 0 to 0 or less.** A hit the object does not accept does not fire it. It does not fire while a save is loading.
  - Technical: the check is `CheckDamageType(type) && health - damage <= 0`, made before the game's `Damage` runs. An object's `allowedDamageTypes` decide which hits count.
- **An object that is not removed after it is smashed fires `Smashed` again on every later hit.** The game keeps running its break code each time, and drops its item again each time. If your mod counts smashes, remember the `Id` of each one you have already seen.
  - Technical: while `health` stays 0 or less, the game takes its break branch on every hit and re-spawns the loot.
- **An animal that dies also fires `Smashed`.** The game kills an animal through the same code path, so you can tell it apart by `IsAnimal`.
  - Technical: `Destroy()`, which `AnimalStats` calls when an animal dies, goes through `SpawnItemOnBreak.Damage`.
- **The object may already be gone when your handler runs, so read the values in `SmashedArgs` and not the object.** The values were taken just before the break.
  - Technical: the event runs as a Harmony postfix, after the game's break code, so the object may already be destroyed.
- **`Smashed` only tells you what happened, and you can't change it.** You cannot cancel the break, change what drops, or change the crime record or a save through this event.
- **`SpawnItemId` is `-1` when the object drops nothing.** That includes an item with id 0.
  - Technical: it is `itemToSpawn.Item.ID`, or -1 when `itemToSpawn` or its `Item` is null or the `ID` is 0.
