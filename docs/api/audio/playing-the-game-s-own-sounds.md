---
title: Playing the game's own sounds
parent: Audio
grand_parent: Screen and UI
nav_order: 2
---

# Playing the game's own sounds

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

The game has a set of item pickup sounds and a set of random sound libraries. `Sounds` lets you list their names and play them. A play call returns `true` when a sound started and `false` when it did not.

```csharp
public static class Sounds
{
    public static List<string> PickupNames();                           // the names of the item pickup sounds you can play
    public static List<string> LibraryNames();                          // the names of the random sound sets you can play
    public static bool PlayPickup(string name);                         // plays one pickup sound; false when it could not start
    public static bool PlayLibrary(string name);                        // plays one sound from a random set, with no position
    public static bool PlayLibraryAt(string name, Vector3 position);    // plays one sound from a random set at a place in the world
}
```

- **`PickupNames()` lists the item pickup sounds, and `PlayPickup(name)` plays one of them.** Names include `Can`, `Cash` and `Keys`. The sound has no position, so the player hears it the same everywhere. A name that is not on the list throws an error, so take the name from `PickupNames()`.
  - Technical: the names are the `PickupSoundName` values, and the sound is played through the game's `ItemSounds`.
- **`LibraryNames()` lists the game's random sound sets, and `PlayLibrary(name)` plays one random sound from a set.** `PlayLibraryAt` plays it at a place in the world, so it gets quieter as the player walks away. A name that is not on the list throws an error.
  - Technical: the sets are the `RandomAudio` fields of `RandomAudioLibrary` (`foley`, `harvest`, `arrestSounds`, `stealSounds`, `prankSounds`, `scarySounds`, `crazyNeighborSounds` and `foleySounds`). They play through `MiscAudioPlayer`.
- **A play call returns `false` when the game's sound player does not exist yet, for example on the main menu.** The call does not throw for that.
