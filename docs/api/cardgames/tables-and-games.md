---
title: Tables and games
parent: Card games
grand_parent: Money and shops
nav_order: 2
---

# Tables and games

This is the full list of what the API gives you. In the code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

Each card table is a `CardTableInfo`, a snapshot of the table at the moment you asked. The snapshot does not update by itself, so ask again for fresh values. When a game ends you get a `CardGameEndedArgs` that describes how it went. Money amounts use the game's currencies: OC is Open Sewer Coins and RM is Real Money (see the [glossary](../../glossary.md#oc)).

```csharp
public enum CardGameKind
{
    Unknown = 0,       // a card game the framework has no name for
    ChinaHand = 1,     // the game players call Shittypants
    Ristiseiska = 2    // the game players call Crosseven
}

public sealed class CardTableInfo    // a snapshot of one card table
{
    public string Id { get; }            // a name for this table; use it to tell tables apart
    public CardGameKind Kind { get; }    // the game chosen at the table; Unknown when none is chosen
    public int Players { get; }          // how many play, NPC opponents included
    public int Stake { get; }            // what the player bet: coins, or a number of items for an item bet; 0 between games
    public bool Active { get; }          // true while a game is running at the table
}

public sealed class CardGameEndedArgs    // how one finished game went
{
    public string TableId { get; }          // the same value as CardTableInfo.Id for that table
    public CardGameKind Kind { get; }       // which game was played
    public bool PlayerWon { get; }          // true when the player came first
    public int Stake { get; }               // what the player bet
    public int Payout { get; }              // what the game pays back for the player's place; not the profit
    public bool ItemBet { get; }            // true when Stake and Payout count items, not coins
    public MoneyCurrency Currency { get; }  // OC or RM, the coins that were bet; ignore it when ItemBet is true
}

public static class CardGames
{
    public static bool IsAvailable { get; }               // false when the Started and Ended events could not be set up
    public static bool PayoutFactorsAvailable { get; }    // false when payout factors could not be set up

    public static List<CardTableInfo> Tables();   // every card table in the scene you are in
    public static CardTableInfo Active();         // the first table with a running game; null when none

    public static event Action<CardTableInfo> Started;     // a game has just started
    public static event Action<CardGameEndedArgs> Ended;   // a game has just finished

    // Sets how much your mod scales the payout of one game. Returns true when it was set.
    // factor runs from 0 to 4; 1 is the game's normal payout.
    public static bool SetPayoutFactor(string modId, CardGameKind kind, float factor);

    // Takes back every payout factor your mod set. Returns true when there was something to remove.
    public static bool Clear(string modId);
}
```

- **`Tables()` lists the card tables in the scene you are in, and `Active()` is `null` when no game is running.** On the main menu, or before a save is loaded, `Tables()` returns an empty list.
- **`Id` is a name for one table.** It stays the same for as long as the table stays where it is in the scene, between calls and between play sessions. The same value is in `Started` and `Ended`. It is made of the scene name and the table's place in the scene, for example `Bar:Room[2]/CardTable[0]`.
  - Technical: it is the table's save GUID when one of its components has one. `CardGameTable` has none in the game's scenes as far as the framework knows, so the id is the scene id: the scene name, a colon, then each object from the root down with its sibling index.
- **`Active` is `true` while a game is running.** A table whose stake menu is open but whose game has not started reports `false`.
  - Technical: `Active` is true when the game's state is anything except `NotStarted` or `GameEnded`.
- **`Stake` is `0` when no game is running.** That includes the first second after the player confirms the bet, before the game starts, and after a game was stopped early. The game never clears its own stake number when the player walks away from the table, so the framework reports `0` instead of the old value.
- **`Started` fires when the game itself begins.** That is about one second after the player's stake is taken. `Stake` already holds the bet.
  - Technical: it fires after the game's own `StartGame` method on `ChinaHand` or `Ristiseiska`.
- **`Ended` fires when a game finishes normally, and not when the player leaves the table mid-game.** If the player walks away, the stake is lost and no event fires.
  - Technical: it fires after `CardGameTable.OnGameFinished`. Leaving the table calls `CardGame.FinishGame(true)` from `CleanUp`, which does not reach it.
- **`Payout` is what the game pays back for the player's place, not the profit.** The game pays the stake times 2 for first place, 1.2 for second, 0.8 for third and 0 for fourth, rounded to a whole number. If a mod set a payout factor for that game, the payout is scaled by it as well. `PlayerWon` is `true` for first place.
  - Technical: the multipliers are copied from `CardGameTable.OnGameFinished`, where they are a local array inside an `async` method, so the framework cannot read them from the game. If a game update changes them, `Payout` goes out of date until the copy is checked again.
- **Events fire during a save load too.** The framework does not hold them back. A handler that should ignore them can check whether the game is busy loading or saving.
  - Technical: `SaveData.Busy` is true while a save is loading or being written.
