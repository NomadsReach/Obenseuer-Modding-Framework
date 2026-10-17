---
title: Ambient sound (soundscape)
parent: Audio
grand_parent: Screen and UI
nav_order: 8
---

# Ambient sound (soundscape)

The game plays a background ambient sound for the area the player is in. This is called a soundscape. These members read it and change it.

```csharp
public static class Sounds
{
    public static string SoundscapeId { get; }       // the id of the ambient sound area playing now; null when none
    public static bool SetSoundscape(string id);     // plays the ambient sound area with this id now; false when there is none
    public static bool ResetSoundscape();            // goes back to the game's ordinary ambient sound
}
```

- **`SoundscapeId` is the id of the ambient sound the game is playing now, or `null` when there is none.**
  - Technical: it is `SoundscapeArea.id` of the controller's current soundscape.
- **`SetSoundscape(id)` starts the ambient sound with that id straight away.** It returns `false` when no ambient sound with that id is loaded, or when the game's sound controller does not exist yet. An empty id throws an error.
  - Technical: it calls `SoundscapeController.PlaySoundscape` for the area that `FindSoundscapeAreaById` finds.
- **`ResetSoundscape()` goes back to the game's global ambient sound.** The game can change the ambient sound again whenever it likes, for example when day turns to night.
  - Technical: the day and night switch is in `SoundscapeGlobal`.
