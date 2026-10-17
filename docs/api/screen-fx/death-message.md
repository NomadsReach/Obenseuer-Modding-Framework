---
title: Death message
parent: Screen effects
grand_parent: Screen and UI
nav_order: 5
---

# Death message

```csharp
public static bool SetDeathMessage(string text);   // set the reason shown on the death screen
```

- **`SetDeathMessage` sets the reason shown on the death screen.** The text stays until the game sets it again. The game does that when the player's stats change, so set your text just before the death screen is shown, or the game may replace it. Nothing is saved.
  - Technical: it calls `DeathMessage.ChangeDeathMessage`. The game calls it from `PlayerStatsUI`. The death screen is shown by `Death` (`ShowDeathMessage`).
