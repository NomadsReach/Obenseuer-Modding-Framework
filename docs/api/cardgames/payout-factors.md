---
title: Payout factors
parent: Card games
grand_parent: Money and shops
nav_order: 3
---

# Payout factors

A payout factor is a number that your mod multiplies a card game's payout by. `1` pays what the game normally pays, `2` pays double, `0.5` pays half. Each mod keeps its own factor for each game, so two mods do not overwrite each other. The game is named by a `CardGameKind` (see [Tables and games](tables-and-games.md)).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CardGames</code> <span>payout factors</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class CardGames
{
    public static bool PayoutFactorsAvailable { get; }
    public static bool SetPayoutFactor(string modId, CardGameKind kind, float factor);
    public static bool Clear(string modId);
}
```

| Property | Type | What it means |
|---|---|---|
| `PayoutFactorsAvailable` | `bool` | `false` when payout factors could not be set up. The events are not affected. |

| Method | Returns | What it does |
|---|---|---|
| `SetPayoutFactor(string modId, CardGameKind kind, float factor)` | `bool` | Sets how much your mod scales the payout of one game. The factor runs from 0 to 4, and 1 is the game's normal payout. `true` when it was set. |
| `Clear(string modId)` | `bool` | Takes back every payout factor your mod set. `true` when there was something to remove. |

</div>
</div>

## Things to know

- `SetPayoutFactor` returns `false` when it did not set the factor. That happens for a bad mod id, a factor below 0 or above 4, the kind `Unknown` or an unknown kind, payout factors not being set up, and while the game is loading or saving a save. Call it again afterwards. <span class="tech">(A mod id must be 1 to 64 characters of letters, digits, `.`, `_` or `-`. A bad factor or kind writes one line to the log.)</span>
- The factor changes the real payout, not just the number you see. The coins or items the player gets, the gambling statistics and `Ended.Payout` all use the scaled amount. <span class="tech">(The framework changes the game's `Mathf.RoundToInt(stake * multiplier)` in `CardGameTable.OnGameFinished`, so the scaled amount is what the game uses from then on.)</span>
- The factor is applied once for each finished game, and the framework does not pay anything on its own.
- If two mods set a factor for the same game, the newest call wins. The framework writes one warning to the log that names both mods.
- If a mod calls `SetPayoutFactor` again, it replaces its own factor and no warning is written. `Clear` takes back all of your mod's factors. If another mod had set one for that game earlier, that one applies again.
- Amounts are rounded to a whole number, and the largest payout is 16,000,000. A payout is never below 0. <span class="tech">(The scaled amount is `Math.Round(amount * factor)` capped at 16,000,000. A factor of `1` leaves the amount untouched.)</span>
- A factor of `0` pays nothing back and still takes the stake. Fourth place pays `0` already, so a factor changes nothing there. For an item bet the amount is a count of items and is rounded the same way.
- The factors are forgotten when the game loads a new main scene. That happens whenever a save is loaded or the player goes back to the main menu. Set your factors again after each load, for example from `Events.WhenReady`. <span class="tech">(They are cleared on every `LoadSceneMode.Single` scene load.)</span>

## Limits

- Games the framework does not know keep their normal payout. A card game that is neither Shittypants nor Crosseven has the kind `Unknown`, and a factor cannot be set for it.
- You can't start a game, deal or play cards, or set the stake from a mod. Starting a game goes through the game's menus and its checks for the player's money or items, so there is no clean call to offer. The game's own card and turn events are also not offered. They would hand you the game's own card types. <span class="tech">(`CardGameTable.StartSelectedGame` needs a seated player and the stake menu. The game's static `CardGame` events (`OnCardPlayed`, `OnTurnStarted`) are not wrapped.)</span>
