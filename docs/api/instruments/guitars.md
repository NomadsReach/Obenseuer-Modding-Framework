---
title: Guitars
parent: Instruments
grand_parent: Objects and devices
nav_order: 2
---

# Guitars

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`Instruments` lists the guitars. A guitar is named by an id, a text name for that guitar in the scene. You get the ids from `Instruments.All()` and pass them to the other members. Each guitar is a `GuitarInfo`, a snapshot of its state at the moment you asked.

```csharp
public static class Instruments
{
    public static bool IsAvailable { get; }          // false when the strum and comment events could not be set up
    public static List<GuitarInfo> All();            // every guitar in the scene you are in, held or not
    public static GuitarInfo Get(string id);         // the guitar with this id; null when none has it
}

public sealed class GuitarInfo    // a snapshot of one guitar
{
    public string Id { get; }            // the guitar's id
    public int ItemId { get; }           // the guitar's item number; 0 when it has none
    public bool Equipped { get; }        // true when the player is holding it
    public bool HoldPlaying { get; }     // true when the player is holding the Primary Use key and the long note is playing
    public bool CanPlayNote { get; }     // true when the guitar is held, not resting after being picked up, and can make a strum sound
}
```

- **`All()` lists every guitar in the scene you are in, whether the player is holding it or not.** A guitar that is switched off in the scene is not listed. `Get(id)` gives one guitar, or `null` when none has that id.
  - Technical: `Id` is the object's `GUID` when it has one, otherwise the scene path. `Equipped` is `WieldableGuitar.equipped`.
- **`CanPlayNote` is the test that `PlayNote` makes.** A guitar rests for one second after the player picks it up, and a strum during that second does nothing.
- **`HoldPlaying` shows only the player's own long note.** A note your mod starts with `HoldNote` does not set it.
  - Technical: it is the game's own hold sound playing, from the Primary Use key held down.
