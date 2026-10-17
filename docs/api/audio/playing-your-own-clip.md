---
title: Playing your own clip
parent: Audio
grand_parent: Screen and UI
nav_order: 3
---

# Playing your own clip

An `AudioClip` is Unity's name for a sound held in memory. These calls play one, and `Find` checks that a mixer group exists. A mixer group is one of the game's volume channels, so the player's volume sliders apply to your sound.

```csharp
public static class Sounds
{
    public static AudioMixerGroup Find(string name);                                                            // the game's volume channel with this name; null when there is none
    public static bool PlayClip(AudioClip clip, string group, float volume = 1f);                               // plays a clip with no position; false when it could not start
    public static bool PlayClipAt(AudioClip clip, string group, Vector3 position, float volume = 1f);          // plays a clip at a place in the world; false when it could not start
}
```

- **`Find(name)` looks up one of the game's volume channels, and gives you `null` when there is no channel with that name.** `Effects` is the channel the game uses for ordinary sound effects. The game also has channels named `Music`, `Menu`, `Ambience`, `Footsteps`, `Underwater` and others. Call `Find` first if you want to be sure a channel exists before you rely on it.
  - Technical: it searches the game's main `AudioMixer` with `FindMatchingGroups`. `Effects` is the default group of `MiscAudioPlayer`.
- **`PlayClip` plays the clip once with no position, and `PlayClipAt` plays it once at a place in the world.** The player's volume sliders apply, because the sound goes through the group you name. `volume` goes from 0 (silent) to 1 (full). The game removes the temporary sound source for you shortly after the clip ends.
  - Technical: the clip plays on a temporary object that is destroyed after `clip.length * 1.1` seconds.
- **Both calls return `false` when the group is missing or the clip can't be played.** Wrong arguments throw an error instead: a clip that is `null`, a group that is `null` or empty, or a volume below 0 or above 1.
