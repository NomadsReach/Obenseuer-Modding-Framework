---
title: Volumes
parent: Audio
grand_parent: Screen and UI
nav_order: 6
---

# Volumes

The player sets the master, sound and music volume in the options menu. These properties read what the player chose.

```csharp
public static class Sounds
{
    public static float MasterVolume { get; }   // the master slider, in decibels
    public static float SfxVolume { get; }      // the sound slider (sound effects), in decibels
    public static float MusicVolume { get; }    // the music slider, in decibels
}
```

- **The numbers are decibels, not a percentage.** The options menu shows them as, for example, `-10 dB`. Zero is the loudest and a more negative number is quieter. They are not a 0 to 1 amount.
  - Technical: the values are `VolumeSave.soundSave.master`, `.sound` and `.music`. The options menu shows them with the format `"F0" + " dB"`. `SfxVolume` belongs to the mixer parameters `Sound` and `OtherSounds`.
- **You can only read these three numbers.** Only the game's own options menu changes what the player has saved. To make the game louder or quieter for the current session, use `SetMixerVolume` below.
- **You get `0` when the game has not set up its volume settings yet.** The numbers only exist once the game has loaded the player's volume settings.
  - Technical: the properties return `0` while `VolumeSave.instance` is `null`.
