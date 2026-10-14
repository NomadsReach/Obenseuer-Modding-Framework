---
title: Card games API
parent: API reference
nav_order: 8
---

# Card games API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.cardgames`.
Files: `src/Api/CardGames.cs`, `src/Api/CardGamesPatches.cs`. Table ids use the save GUID, or the scene path.

The facade covers the card tables and their two games, China Hand and Ristiseiska. It reads, reports events, and has one write: per-mod payout factors (see Payout factors). Mods cannot start a game, deal, play a card or set a stake.

## If a hook cannot be installed

The framework installs the event hooks under one owner. If any hook is missing, the owner is removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.cardgames")` becomes true, and one warning is logged. `Tables` and `Active` keep working. Only `Started` and `Ended` stop. The payout hook has its own owner, `obenseuer.moddingframework.cardgames.payout`. If it cannot be installed, only `PayoutFactorsAvailable` turns false and the events are not affected.

## Threading and safety

- Main thread only.
- `CardGames.IsAvailable` is false when the hooks were not installed or the owner was disabled. Reads are not affected.
- Every member is safe at any time. On the main menu or with no save loaded, `Tables` returns an empty list and `Active` returns `null`.
- Nothing throws to the caller. Each member catches its own exceptions and logs the first failure per member.
- Events are raised synchronously from the game's own call. Each handler is isolated, so one that throws is logged and the others still run.

## Surface

```csharp
public enum CardGameKind { Unknown = 0, ChinaHand = 1, Ristiseiska = 2 }

public sealed class CardTableInfo
{
    public string Id { get; }            // save GUID when the table has one, otherwise the scene id `<scene>:<root>[i]/.../<table>[j]` (see Tables)
    public CardGameKind Kind { get; }    // the game selected at the table; Unknown when none
    public int Players { get; }          // players in that game, NPC opponents included (CardGame.PlayerCount)
    public int Stake { get; }            // the seated player's stake (money, or items when the bet is an item); 0 between games
    public bool Active { get; }          // a game is running at the table
}

public sealed class CardGameEndedArgs
{
    public string TableId { get; }
    public CardGameKind Kind { get; }
    public bool PlayerWon { get; }       // the seated player placed first
    public int Stake { get; }
    public int Payout { get; }           // what the game pays for the placing
    public bool ItemBet { get; }         // true when Stake and Payout count items, not money
    public MoneyCurrency Currency { get; } // the money currency of the bet; ignored when ItemBet is true
}

public static class CardGames
{
    public static bool IsAvailable { get; }
    public static bool PayoutFactorsAvailable { get; }   // true when the payout hook is installed
    public static bool SetPayoutFactor(string modId, CardGameKind kind, float factor);   // 0 to 4, see Payout factors
    public static bool Clear(string modId);              // removes every payout factor this mod set
    public static List<CardTableInfo> Tables();   // every card table in the active scene
    public static CardTableInfo Active();         // the first table with a running game; null when none
    public static event Action<CardTableInfo> Started;
    public static event Action<CardGameEndedArgs> Ended;
}
```

## Tables

`Id` is the table's save GUID when one of its components has one. `CardGameTable` has none in build 25839246, so the id is the scene id: the scene name, a colon, then each node from the root down with its sibling index (for example `Bar:Room[2]/CardTable[0]`). It is stable between calls and sessions for a table that stays in the scene hierarchy, and it is the value `Started` and `Ended` report. The event hooks build nothing when the event has no subscriber.

## States

`Active` is true when the table's game is in any state other than `NotStarted` or `GameEnded`. A table that was set up but has not started its game (the stake menu is open) reports `Active` false. `Stake` is the stake of the running game. It is `0` while no game has started (including the first second after the player confirms the stake, before `StartGame`) and after a forced finish. The game never clears its stake field on a forced finish (the player left the table), so the facade reports `0` rather than the stale value.

## Events

- `Started(CardTableInfo)`: the game's own `StartGame` ran. That is one second after the stake is taken. The stake is in `Stake` already.
- `Ended(CardGameEndedArgs)`: the table finished the game (`CardGameTable.OnGameFinished`, `CardGameTable`). It fires for a normal finish only. A forced finish (`CardGame.FinishGame(true)`, used when the player leaves the table, `CardGameTable`, from `CleanUp`) does not reach it: the stake is lost and no event fires.
  - `Payout` is the stake times the placing's multiplier, rounded like the game: 2, 1.2, 0.8 and 0 for placings 1 to 4, then scaled by the winning payout factor of the game kind when a mod set one. It is what the game pays back, not the net gain. The multipliers are copied from `CardGameTable` because they are a local array literal inside the `async` method `OnGameFinished`, not a field, so nothing can be read or injected. `Payout` goes stale if the game changes these values. Re-check the array when the game build changes.
  - `PlayerWon` is the same test as the table's win trigger.

## Payout factors

`SetPayoutFactor(modId, kind, factor)` sets one mod's payout factor for one card game, `ChinaHand` or `Ristiseiska`. The factor is from 0 to 4, and `1` is the game's own payout. It returns `false` for an invalid mod id, a factor outside 0 to 4, a kind that is `Unknown` or not defined, `PayoutFactorsAvailable` being false, or a save or load running.

- **Newest call wins.** Each mod keeps one factor per game, and for a game the newest call applies. When a second mod sets a game that another mod already set, one warning names both mods, and the second mod's factor applies. `Clear(modId)` removes that mod's factors for both games, and the factor set before it applies again. A mod that calls `SetPayoutFactor` again replaces its own factor with no warning.
- **Where it applies.** The factor scales the amount the game works out for the placing, `Mathf.RoundToInt(stake * multiplier)` in `CardGameTable.OnGameFinished`, before the game uses it. The wallet credit (or the item credit for an item bet), the gambling tally and `Ended.Payout` all use the scaled amount. It is scaled once per finished game, and the framework adds no payment of its own.
- **Rounding.** The scaled amount is `Math.Round(amount * factor)`, at most 16,000,000. A factor of `1`, a payout of `0` (placing 4) and a factor on a game that nobody set leave the game's amount as it is. A factor of `0` pays nothing and still takes the stake. A scaled amount is never negative.
- **Item bets.** For an item bet the amount is a count of items, so it is scaled and rounded as a count.
- **No factor set.** With no factor set the game's code path computes the same numbers as the game: the hook returns its input.
- **Reset.** The factors are cleared on every Single scene load, so a mod that wants one in force sets it again after the scene loads.
- **Unknown games.** A `CardGame` subclass that is neither China Hand nor Ristiseiska has kind `Unknown` and is never scaled.

## Not in this area

- Starting a game (`CardGameTable.StartSelectedGame`). It needs a seated player, the stake menu and the money or item check. That path is a UI flow, not a clean start method, so no start member is exposed.
- Card play, dealing and turn events. The game's own `CardGame` events (`OnCardPlayed`, `OnTurnStarted`) are static game events and would expose game types. Add them when a mod needs them.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
