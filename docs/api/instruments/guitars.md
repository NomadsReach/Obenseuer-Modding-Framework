---
title: Guitars
parent: Instruments
grand_parent: Objects and devices
nav_order: 2
---

# Guitars

`Instruments` lists the guitars. A guitar is named by an id, a text name for that guitar in the scene. You get the ids from `Instruments.All()` and pass them to the other members. Each guitar is a `GuitarInfo`, a snapshot of its state at the moment you asked.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Instruments</code> <span>the guitars in the scene</span></p>

This is the class you start from. `All()` and `Get(id)` find the guitars in the scene, and `IsAvailable` tells you whether the strum and comment events work. The calls that play a guitar are on the [Playing notes and songs](playing-notes-and-songs.md) page.

<div class="api-pair" markdown="1">

```csharp
public static class Instruments
{
    public static bool IsAvailable { get; }
    public static List<GuitarInfo> All();
    public static GuitarInfo Get(string id);
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<GuitarInfo>` | Every guitar in the scene you are in, held or not. |
| `Get(id)` | `GuitarInfo` | The guitar with this id. `null` when none has it. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the strum and comment events could not be set up. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>GuitarInfo</code> <span>a snapshot of one guitar</span></p>

`All()` and `Get(id)` give you this, one per guitar. It is a copy of the guitar when you asked, so ask again for a current `Equipped` or `HoldPlaying`. <span class="tech">(`Equipped` is the game's `WieldableGuitar.equipped`.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class GuitarInfo
{
    public string Id { get; }
    public int ItemId { get; }
    public bool Equipped { get; }
    public bool HoldPlaying { get; }
    public bool CanPlayNote { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The guitar's id. |
| `ItemId` | `int` | The guitar's item number. `0` when it has none. |
| `Equipped` | `bool` | `true` when the player is holding it. |
| `HoldPlaying` | `bool` | `true` when the player is holding the Primary Use key and the long note is playing. |
| `CanPlayNote` | `bool` | `true` when the guitar is held, not resting after being picked up, and can make a strum sound. |

</div>
</div>

## Things to know

- `All()` lists every guitar in the scene you are in, whether the player is holding it or not. A guitar that is switched off in the scene is not listed. `Get(id)` gives one guitar, or `null` when none has that id. <span class="tech">(`Id` is the object's `GUID` when it has one, otherwise the scene path. `Equipped` is `WieldableGuitar.equipped`.)</span>
- `CanPlayNote` is the test that `PlayNote` makes. A guitar rests for one second after the player picks it up, and a strum during that second does nothing.
- `HoldPlaying` shows only the player's own long note. A note your mod starts with `HoldNote` does not set it. <span class="tech">(It is the game's own hold sound playing, from the Primary Use key held down.)</span>
