---
title: Obenseuer Modding Framework audio API (Sounds)
parent: API reference
nav_order: 1
---

# Obenseuer Modding Framework audio API (Sounds)

Experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.audio`. Logs go to `BepInEx\logs\obenseuer.moddingframework.audio.log`.
`AudioClip` is in `UnityEngine.AudioModule`, so a mod project needs a reference to `UnityEngine.AudioModule.dll`.

Everything not listed here is internal. Main thread only. It has no events: the facade only reads game state and plays sounds.

## Rules

- **Availability.** Every member is safe on the main menu and before a save is loaded. Reads return `0`, `null` or an empty array when the game object they read is missing. Playing returns `false` when the game player is missing.
- **Bad arguments.** A null or empty name, a null clip, an unknown pickup or library name, or a volume outside `[0, 1]` throws `ArgumentException`. Everything else returns the fallback.
- **Game failures.** When a call into the game throws, the call returns its fallback. The first throw of each member is logged; later throws are not.
- **Volumes are decibels.** `MasterVolume`, `SfxVolume` and `MusicVolume` are the values the options menu stores, in dB (`OptionsMenu`: `"F0" + " dB"`). They are not linear 0 to 1.
- **Mixer routing.** Sounds played with `PlayClip`, `PlayClipAt` and `PlayLibrary*` go through the game's `AudioMixer`, so the player's Master, Sound and Music sliders apply.

## Playing game sounds

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

- `PickupNames` lists the item-pickup sound names (`PickupSoundName`, for example `Can`, `Cash`, `Keys`). `PlayPickup` plays one of them in 2D through `ItemSounds`. An unknown name throws.
- `LibraryNames` lists the `RandomAudioLibrary` slots (`foley`, `harvest`, `arrestSounds`, `stealSounds`, `prankSounds`, `scarySounds`, `crazyNeighborSounds`, `foleySounds`). `PlayLibrary` plays one in 2D. `PlayLibraryAt` plays one at a world position.
- Each play returns `true` when a clip was started.

Not wrapped: UI click and hover sounds. The game keeps them on each button prefab (`SoundInfo`), with no sound id to pass in. For a notification sound, use `Hud.Notify`: the notification box plays its own clip.

## Mixer groups and mod clips

```csharp
public static AudioMixerGroup Find(string name);
public static bool PlayClip(AudioClip clip, string group, float volume = 1f);
public static bool PlayClipAt(AudioClip clip, string group, Vector3 position, float volume = 1f);
```

- `Find` looks up a group of the game's main mixer by name (`AudioMixer.FindMatchingGroups`). It returns `null` when the mixer or the group is missing. `"Effects"` is the game's default output for misc sounds (`MiscAudioPlayer`, `Awake`). The mixer asset also contains the names `Music`, `Menu`, `Ambience`, `Footsteps`, `Underwater` and others. Use `Find` to check that one exists in game before you rely on it.
- `PlayClip` plays a clip in 2D. `PlayClipAt` plays it at a position. The clip plays once, and the temporary source is destroyed after `clip.length * 1.1` seconds.
- Each play returns `false` when the group is missing or the clip cannot be played.

## Loading a WAV file

```csharp
public static AudioClip LoadWav(string path);
```

- Reads a file from disk and builds an `AudioClip` with `AudioClip.Create`. Supported: RIFF/WAVE, PCM at 8, 16, 24 or 32 bits, and IEEE float at 32 bits. Any channel count. Unknown chunks are skipped. A data chunk size of `-1` or larger than the file reads to the end of the file.
- Returns `null` for a missing file, a non-WAV file, an unsupported format or a truncated file.
- **WAV only. `.ogg` (and mp3 and any other format) is not supported and `LoadWav` returns `null` for it.** Convert to WAV first.
- WAVE_FORMAT_EXTENSIBLE files are accepted when the sub-format is PCM or float. Files larger than 16 MiB are rejected (`null`). Decoded samples are kept as floats, so a 16 MiB 8-bit file uses about 64 MiB of memory while it is decoded.

## Raw PCM clips

```csharp
public static AudioClip FromPcm(string name, float[] samples, int channels, int rate);
public static AudioClip FromPcm16(string name, short[] samples, int channels, int rate);
```

- Builds an `AudioClip` from samples you hold in code, for audio that must stay inside the DLL and cannot ship as a loose file. Nothing is read from disk.
- `samples` are interleaved frames. For stereo, each frame is a left sample then a right sample. `channels` is 1 to 8. `rate` is 8000 to 192000 Hz. The length of `samples` must be a non-zero multiple of `channels`.
- The sample array is capped at the 16 MiB budget `LoadWav` uses, counted as the bytes you pass: about 4.2 million `float` values, or about 8.4 million `short` values. A larger array is refused: the call returns `null` and logs once. A WAV load allocates a float per sample as well, so this keeps both paths in the same memory class.
- `FromPcm` takes float samples. It works on a copy, so your array is not changed. A `NaN` or infinite sample becomes 0, and any other sample is clamped to -1 to 1. `FromPcm16` takes 16-bit samples and divides each by 32768, so -32768 gives -1.
- A null or empty `name` throws `ArgumentException`. A null `samples` throws `ArgumentNullException`. A bad `channels`, `rate` or length throws `ArgumentException`. Each names its parameter.
- The clip is returned to the caller, who owns it. The framework keeps no reference. Pass it to `UnityEngine.Object.Destroy` when you no longer need it.
- Call `FromPcm` and `FromPcm16` on the main thread. Off the main thread the Unity call fails, and both return `null`.
- Returns `null` when Unity cannot build the clip. The first failure of each member is logged.
- Play the clip with `PlayClip(clip, group, volume)` for 2D sound, or `PlayClipAt` for a position. `"Effects"` is the game's default group for misc sounds.

## Volumes

```csharp
public static float MasterVolume { get; }   // dB, VolumeSave.soundSave.master
public static float SfxVolume { get; }      // dB, VolumeSave.soundSave.sound (mixer "Sound" and "OtherSounds")
public static float MusicVolume { get; }    // dB, VolumeSave.soundSave.music
```

- Read from the game's saved volume state. Reads `0` before `VolumeSave` exists.
- Read only. The saved values change only through the game's options menu. `SetMixerVolume` changes the live mixer for the session, see below.

## Mixer volume (session only)

```csharp
public static bool SetMixerVolume(string modId, string group, float decibels);
```

- `group` is one of the game's mixer parameters: `Master`, `Sound`, `Music` or `Nasty`. `Sound` also sets `OtherSounds`, as the game's Sound slider does. `decibels` must be finite and from -80 to 0. The facade does not check that range against the game's sliders.
- The write changes the live mixer only. It does not write the saved volume file, so `MasterVolume`, `SfxVolume` and `MusicVolume` still return the saved values.
- The game puts the saved mix back when its options controller or options menu starts (`OptionsController.Start` and `OptionsMenu.Start`, which call `VolumeSave.GetVolume`), and when the pause menu sets up the options menu. Set the value again after a scene load if it has to last.
- Returns `false` for an invalid mod id, an unknown group, a value out of range, a save or a load in progress, or a missing game mixer or parameter. Invalid input is logged once.

## Soundscape

```csharp
public static string SoundscapeId { get; }
public static bool SetSoundscape(string id);
public static bool ResetSoundscape();
```

- `SoundscapeId` is the id of the soundscape area the game is playing now (`SoundscapeArea.id`), or `null`.
- `SetSoundscape` plays the area with that id now, through `SoundscapeController.PlaySoundscape`. It returns `false` when no such area is loaded. An empty id throws.
- `ResetSoundscape` returns to the game's global soundscape. The game's day and night switch in `SoundscapeGlobal` can change the soundscape again at any time.

## Not wrapped

- **Music tracks.** The game has no music or radio player class. `Radio` is one per prop and holds its own `SoundInfo`. There is nothing global to play or stop, so no track functions exist.
- **Background soundscape.** Not wrapped.
