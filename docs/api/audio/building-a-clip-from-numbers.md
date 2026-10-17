---
title: Building a clip from numbers
parent: Audio
grand_parent: Screen and UI
nav_order: 5
---

# Building a clip from numbers

Sometimes the sound has to live inside your DLL instead of a loose file, or your code makes the sound. `FromPcm` and `FromPcm16` build a clip from a list of numbers. Each number is one point on the sound wave, called a sample.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Sounds</code> <span>builds clips from sample numbers</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Sounds
{
    public static AudioClip FromPcm(string name, float[] samples, int channels, int rate);
    public static AudioClip FromPcm16(string name, short[] samples, int channels, int rate);
}
```

| Method | Returns | What it does |
|---|---|---|
| `FromPcm(string name, float[] samples, int channels, int rate)` | `AudioClip` | Builds a clip from decimal samples between -1 and 1. |
| `FromPcm16(string name, short[] samples, int channels, int rate)` | `AudioClip` | Builds a clip from 16-bit whole-number samples. |

</div>
</div>

## Things to know

- `samples` holds the sound one moment at a time. For stereo sound, each moment is a left sample followed by a right sample. <span class="tech">(The samples are interleaved frames.)</span>
- `channels` is 1 to 8, and `rate` is how many moments there are each second, from 8000 to 192000. The number of samples must be more than zero and a whole multiple of `channels`.
- `FromPcm` uses decimal samples between -1 and 1, and `FromPcm16` uses whole numbers from -32768 to 32767.
- `FromPcm` leaves your array alone. It works on a copy, turns a sample that is not a real number into silence, and clamps any other sample into the range -1 to 1.
- `FromPcm16` divides each sample by 32768, so -32768 becomes -1.
- You own the clip you get back, so destroy it when you are finished. The framework does not keep a reference to it. Pass it to `UnityEngine.Object.Destroy` when you no longer need it.
- Play the clip with `PlayClip` or `PlayClipAt`. `Effects` is the usual channel for sound effects.

## Limits

- Big arrays are refused. The limit is the same 16 MiB that `LoadWav` uses, counted in bytes: about 4.2 million `float` values, or about 8.4 million `short` values. A bigger array gives `null` and one line in the log.
- Wrong arguments throw an error that names the argument. A `null` or empty `name` throws `ArgumentException`. A `null` `samples` throws `ArgumentNullException`. A bad `channels`, `rate` or sample count throws `ArgumentException`.
- Call these from your mod's normal code. From a background thread you started, Unity refuses to build the clip and you get `null`. See [Main thread](../../glossary.md#main-thread).
- You get `null` when Unity can't build the clip, and the first failure is written to the log.
