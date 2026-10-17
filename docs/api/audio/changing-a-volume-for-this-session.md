---
title: Changing a volume for this session
parent: Audio
grand_parent: Screen and UI
nav_order: 7
---

# Changing a volume for this session

`SetMixerVolume` changes how loud one of the game's volume channels is, until the game puts the player's own volumes back.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Sounds</code> <span>volume channels</span></p>

Call `SetMixerVolume` with your mod id, a channel name and a value in decibels to change that channel for this session. The volumes the player saved are not changed.

<div class="api-pair" markdown="1">

```csharp
public static class Sounds
{
    public static bool SetMixerVolume(string modId, string group, float decibels);
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetMixerVolume(modId, group, decibels)` | `bool` | Sets a channel's loudness. Returns `false` when it was not set. |

</div>
</div>

## Things to know

- `group` must be `Master`, `Sound`, `Music` or `Nasty`. These are the game's own volume channels. Setting `Sound` also sets the other sound effects channel, the same as the game's Sound slider does. <span class="tech">(`Sound` also sets the mixer parameter `OtherSounds`.)</span>
- `decibels` must be a real number from -80 (silent) to 0 (loudest). The framework does not compare it with the game's sliders.
- `modId` is your mod's id. It has 1 to 64 letters, digits, `.`, `_` or `-`. See [Mod id](../../glossary.md#mod-id).
- The change lasts for the session only. It does not change the volumes the player saved, so `MasterVolume`, `SfxVolume` and `MusicVolume` keep reporting the saved numbers.

## Limits

- The game puts the player's volumes back whenever it sets up its options, so set your value again after a scene change if it has to last. That also happens when the pause menu builds its options page. <span class="tech">(The game restores the saved mix when `OptionsController.Start` and `OptionsMenu.Start` run, because they call `VolumeSave.GetVolume`, and when the pause menu sets up the options menu.)</span>
- It returns `false` when nothing was changed. That happens for an invalid mod id, an unknown group, a value out of range, a save loading or saving, or a missing game volume setting. A wrong mod id, group or value is also written to the log once.
