---
title: Common tasks
parent: Audio
grand_parent: Screen and UI
nav_order: 1
---

# Common tasks

Play the sound an item makes when you pick it up. `PickupNames()` lists the names you can use, such as `Can`, `Cash` and `Keys`.

```csharp
bool played = Sounds.PlayPickup("Cash");
```

Play one of the game's random sound sets, either with no position or at a place in the world. `LibraryNames()` lists the sets.

```csharp
Sounds.PlayLibrary("scarySounds");
Sounds.PlayLibraryAt("foley", new Vector3(0f, 0f, 0f));
```

Load a WAV file from disk and play it. `LoadWav` gives you `null` when the file can't be used, so check for that.

```csharp
AudioClip clip = Sounds.LoadWav(@"C:\path\to\your\sound.wav");
if (clip != null) Sounds.PlayClipAt(clip, "Effects", new Vector3(0f, 0f, 0f), 1f);
```

Read the volume sliders. The numbers are decibels, not a percentage.

```csharp
Log.Info("Master " + Sounds.MasterVolume + " dB, sound " + Sounds.SfxVolume + " dB, music " + Sounds.MusicVolume + " dB");
```

Make the game quieter for this session only, then change the ambient sound.

```csharp
Sounds.SetMixerVolume("example.beep", "Music", -20f);
Log.Info("Now playing the ambient sound: " + Sounds.SoundscapeId);
Sounds.ResetSoundscape();
```
