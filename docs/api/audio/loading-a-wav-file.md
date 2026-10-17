---
title: Loading a WAV file
parent: Audio
grand_parent: Screen and UI
nav_order: 4
---

# Loading a WAV file

A WAV file is a sound file. `LoadWav` reads one from disk and gives you a clip you can play.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Sounds</code> <span>loads WAV files</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Sounds
{
    public static AudioClip LoadWav(string path);
}
```

| Method | Returns | What it does |
|---|---|---|
| `LoadWav(string path)` | `AudioClip` | Reads a WAV file into a clip. Returns `null` when the file can't be used. |

</div>
</div>

## Things to know

- These WAV types work: 8, 16, 24 and 32-bit whole-number sound, and 32-bit decimal-number sound, with any number of channels. Files that carry extra information (unknown chunks) are fine too. <span class="tech">(RIFF/WAVE with PCM at 8, 16, 24 or 32 bits and IEEE float at 32 bits. Unknown chunks are skipped. A data chunk size of `-1`, or larger than the file, reads to the end of the file. WAVE_FORMAT_EXTENSIBLE files are accepted when the sub-format is PCM or float. The clip is built with `AudioClip.Create`.)</span>

## Limits

- Only WAV files work. `.ogg`, `.mp3` and every other format give `null`. Convert the sound to WAV first.
- `LoadWav` gives you `null` for a missing file, a file that is not a WAV, a format it can't read, or a file that is cut short. A path that is `null` or empty throws an error.
- WAV files bigger than 16 MiB are refused (you get `null`). The game keeps the sound in memory as decimal numbers, so a big file can use a lot of memory. A 16 MiB file with 8-bit sound takes about 64 MiB while it loads.
