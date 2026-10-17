---
title: Playing the game's own sounds
parent: Audio
grand_parent: Screen and UI
nav_order: 2
---

# Playing the game's own sounds

This is the full list of the game's own sounds you can use: the item pickup sounds and the random sound libraries. `Sounds` lets you list their names and play them. A play call returns `true` when a sound started and `false` when it did not. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Sounds</code> <span>the game's pickup and library sounds</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Sounds
{
    public static List<string> PickupNames();
    public static List<string> LibraryNames();
    public static bool PlayPickup(string name);
    public static bool PlayLibrary(string name);
    public static bool PlayLibraryAt(string name, Vector3 position);
}
```

| Method | Returns | What it does |
|---|---|---|
| `PickupNames()` | `List<string>` | The names of the item pickup sounds you can play. |
| `LibraryNames()` | `List<string>` | The names of the random sound sets you can play. |
| `PlayPickup(string name)` | `bool` | Plays one pickup sound. Returns `false` when it could not start. |
| `PlayLibrary(string name)` | `bool` | Plays one sound from a random set, with no position. |
| `PlayLibraryAt(string name, Vector3 position)` | `bool` | Plays one sound from a random set at a place in the world. |

</div>
</div>

## Things to know

- `PickupNames()` lists the item pickup sounds, and `PlayPickup(name)` plays one of them. Names include `Can`, `Cash` and `Keys`. The sound has no position, so the player hears it the same everywhere. <span class="tech">(The names are the `PickupSoundName` values, and the sound is played through the game's `ItemSounds`.)</span>
- `LibraryNames()` lists the game's random sound sets, and `PlayLibrary(name)` plays one random sound from a set. `PlayLibraryAt` plays it at a place in the world, so it gets quieter as the player walks away. <span class="tech">(The sets are the `RandomAudio` fields of `RandomAudioLibrary` (`foley`, `harvest`, `arrestSounds`, `stealSounds`, `prankSounds`, `scarySounds`, `crazyNeighborSounds` and `foleySounds`). They play through `MiscAudioPlayer`.)</span>

## Limits

- A pickup name that is not on the list throws an error, so take the name from `PickupNames()`. A library name that is not on the list also throws an error.
- A play call returns `false` when the game's sound player does not exist yet, for example on the main menu. The call does not throw for that.
