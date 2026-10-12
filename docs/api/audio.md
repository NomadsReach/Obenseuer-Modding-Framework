# Obenseuer Modding Framework audio API (Sounds)

Experimental. Not checked in game.
Game: Obenseuer 0.5.05 (build 25839246), Unity 6000.0.77f1 Mono.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.audio`. Logs go to `BepInEx\logs\obenseuer.moddingframework.audio.log`.

Everything not listed here is internal. Main thread only. No `Init` and no events: the facade only reads game state and plays sounds.

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

Not wrapped: UI click and hover sounds and notification sounds. The game keeps them on each button or panel prefab (`SoundInfo`, `AudioSource`), not in a global registry.

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
- WAVE_FORMAT_EXTENSIBLE files are accepted when the sub-format is PCM or float. Files larger than 64 MiB are rejected (`null`).

## Volumes

```csharp
public static float MasterVolume { get; }   // dB, VolumeSave.soundSave.master
public static float SfxVolume { get; }      // dB, VolumeSave.soundSave.sound (mixer "Sound" and "OtherSounds")
public static float MusicVolume { get; }    // dB, VolumeSave.soundSave.music
```

- Read from the game's saved volume state. Reads `0` before `VolumeSave` exists.
- Read only. Changing volume is done through the game's own options menu.

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
