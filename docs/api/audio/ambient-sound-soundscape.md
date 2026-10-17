---
title: Ambient sound (soundscape)
parent: Audio
grand_parent: Screen and UI
nav_order: 8
---

# Ambient sound (soundscape)

The game plays a background ambient sound for the area the player is in. This is called a soundscape. These members read it and change it.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Sounds</code> <span>ambient sound controls</span></p>

Read `SoundscapeId` to see which ambient sound is playing, then use `SetSoundscape` or `ResetSoundscape` to change it.

<div class="api-pair" markdown="1">

```csharp
public static class Sounds
{
    public static string SoundscapeId { get; }
    public static bool SetSoundscape(string id);
    public static bool ResetSoundscape();
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetSoundscape(id)` | `bool` | Starts the ambient sound with this id now. Returns `false` when there is none. |
| `ResetSoundscape()` | `bool` | Goes back to the game's ordinary ambient sound. |

| Property | Type | What it means |
|---|---|---|
| `SoundscapeId` | `string` | The id of the ambient sound playing now. `null` when there is none. |

</div>
</div>

## Things to know

- `SoundscapeId` is the id of the ambient sound the game is playing now, or `null` when there is none. <span class="tech">(It is `SoundscapeArea.id` of the controller's current soundscape.)</span>
- `SetSoundscape(id)` starts the ambient sound with that id straight away. <span class="tech">(It calls `SoundscapeController.PlaySoundscape` for the area that `FindSoundscapeAreaById` finds.)</span>
- `ResetSoundscape()` goes back to the game's global ambient sound. <span class="tech">(The day and night switch is in `SoundscapeGlobal`.)</span>

## Limits

- `SetSoundscape` returns `false` when no ambient sound with that id is loaded, or when the game's sound controller does not exist yet. An empty id throws an error.
- The game can change the ambient sound again whenever it likes, for example when day turns to night.
