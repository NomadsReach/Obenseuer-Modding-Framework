---
title: Playing your own clip
parent: Audio
grand_parent: Screen and UI
nav_order: 3
---

# Playing your own clip

This page uses `Vector3`, `AudioClip` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

An `AudioClip` is Unity's name for a sound held in memory. These calls play one, and `Find` checks that a mixer group exists. A mixer group is one of the game's volume channels, so the player's volume sliders apply to your sound.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Sounds</code> <span>plays your own clips</span></p>

Use `Find` to get one of the game's volume channels, such as `Effects`, then `PlayClip` or `PlayClipAt` to play your clip through it.

<div class="api-pair" markdown="1">

```csharp
public static class Sounds
{
    public static AudioMixerGroup Find(string name);
    public static bool PlayClip(AudioClip clip, string group, float volume = 1f);
    public static bool PlayClipAt(AudioClip clip, string group, Vector3 position, float volume = 1f);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Find(name)` | `AudioMixerGroup` | The game's volume channel with this name. `null` when there is none. |
| `PlayClip(clip, group, volume)` | `bool` | Plays a clip with no position. Returns `false` when it could not start. |
| `PlayClipAt(clip, group, position, volume)` | `bool` | Plays a clip at a place in the world. Returns `false` when it could not start. |

</div>
</div>

## Things to know

- `Find(name)` looks up one of the game's volume channels, and gives you `null` when there is no channel with that name. `Effects` is the channel the game uses for ordinary sound effects. The game also has channels named `Music`, `Menu`, `Ambience`, `Footsteps`, `Underwater` and others. <span class="tech">(It searches the game's main `AudioMixer` with `FindMatchingGroups`. `Effects` is the default group of `MiscAudioPlayer`.)</span>
- Call `Find` first if you want to be sure a channel exists before you rely on it.
- `PlayClip` plays the clip once with no position, and `PlayClipAt` plays it once at a place in the world. The player's volume sliders apply, because the sound goes through the group you name.
- `volume` goes from 0 (silent) to 1 (full). The game removes the temporary sound source for you shortly after the clip ends. <span class="tech">(The clip plays on a temporary object that is destroyed after `clip.length * 1.1` seconds.)</span>

## Limits

- Both calls return `false` when the group is missing or the clip can't be played. Wrong arguments throw an error instead: a clip that is `null`, a group that is `null` or empty, or a volume below 0 or above 1.
