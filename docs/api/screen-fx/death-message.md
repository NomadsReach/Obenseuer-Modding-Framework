---
title: Death message
parent: Screen effects
grand_parent: Screen and UI
nav_order: 5
---

# Death message

This call sets the reason the death screen shows.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ScreenFx</code> <span>the reason on the death screen</span></p>

`ScreenFx.SetDeathMessage` sets the reason the death screen shows. Call it just before the death screen appears, because the game may replace the text before then.

<div class="api-pair" markdown="1">

```csharp
public static bool SetDeathMessage(string text);
```

| Method | Returns | What it does |
|---|---|---|
| `SetDeathMessage(text)` | `bool` | Sets the reason shown on the death screen. |

</div>
</div>

## Things to know

- The text stays until the game sets it again. The game does that when the player's stats change. Nothing is saved. <span class="tech">(It calls `DeathMessage.ChangeDeathMessage`. The game calls it from `PlayerStatsUI`. The death screen is shown by `Death` (`ShowDeathMessage`).)</span>

## Limits

- Set your text just before the death screen is shown, or the game may replace it.
