---
title: Volumes
parent: Audio
grand_parent: Screen and UI
nav_order: 6
---

# Volumes

The player sets the master, sound and music volume in the options menu. These properties read what the player chose.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Sounds</code> <span>the player's saved volumes</span></p>

Read `MasterVolume`, `SfxVolume` and `MusicVolume` to see the sliders the player saved. They are in decibels.

<div class="api-pair" markdown="1">

```csharp
public static class Sounds
{
    public static float MasterVolume { get; }
    public static float SfxVolume { get; }
    public static float MusicVolume { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `MasterVolume` | `float` | The master slider, in decibels. |
| `SfxVolume` | `float` | The sound slider (sound effects), in decibels. |
| `MusicVolume` | `float` | The music slider, in decibels. |

</div>
</div>

## Things to know

- The numbers are decibels, not a percentage. The options menu shows them as, for example, `-10 dB`. Zero is the loudest and a more negative number is quieter. They are not a 0 to 1 amount. <span class="tech">(The values are `VolumeSave.soundSave.master`, `.sound` and `.music`. The options menu shows them with the format `"F0" + " dB"`. `SfxVolume` belongs to the mixer parameters `Sound` and `OtherSounds`.)</span>

## Limits

- You can only read these three numbers. Only the game's own options menu changes what the player has saved. To make the game louder or quieter for the current session, use `SetMixerVolume` (see [Changing a volume for this session](changing-a-volume-for-this-session.md)).
- You get `0` when the game has not set up its volume settings yet. The numbers only exist once the game has loaded the player's volume settings. <span class="tech">(The properties return `0` while `VolumeSave.instance` is `null`.)</span>
