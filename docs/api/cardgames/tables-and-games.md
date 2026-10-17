---
title: Tables and games
parent: Card games
grand_parent: Money and shops
nav_order: 2
---

# Tables and games

Each card table is a `CardTableInfo`, a snapshot of the table at the moment you asked. The snapshot does not update by itself, so ask again for fresh values. When a game ends you get a `CardGameEndedArgs` that describes how it went. Money amounts use the game's currencies: OC is Open Sewer Coins and RM is Real Money (see the [glossary](../../glossary.md#oc)).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CardGames</code> <span>the static class for card games</span></p>

Use `Tables` to list the card tables in the scene and `Active` to find the one with a game running. `Started` and `Ended` tell you when a game begins and finishes.

<div class="api-pair" markdown="1">

```csharp
public static class CardGames
{
    public static bool IsAvailable { get; }
    public static bool PayoutFactorsAvailable { get; }
    public static List<CardTableInfo> Tables();
    public static CardTableInfo Active();
    public static event Action<CardTableInfo> Started;
    public static event Action<CardGameEndedArgs> Ended;
    public static bool SetPayoutFactor(string modId, CardGameKind kind, float factor);
    public static bool Clear(string modId);
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the `Started` and `Ended` events could not be set up. |
| `PayoutFactorsAvailable` | `bool` | `false` when payout factors could not be set up. See [Payout factors](payout-factors.md). |

| Method | Returns | What it does |
|---|---|---|
| `Tables()` | `List<CardTableInfo>` | Every card table in the scene you are in. |
| `Active()` | `CardTableInfo` | The first table with a running game. `null` when none is running. |
| `SetPayoutFactor(modId, kind, factor)` | `bool` | Sets how much your mod scales the payout of one game. Returns `true` when it was set. `factor` runs from 0 to 4, and `1` is the game's normal payout. See [Payout factors](payout-factors.md). |
| `Clear(modId)` | `bool` | Takes back every payout factor your mod set. Returns `true` when there was something to remove. See [Payout factors](payout-factors.md). |

| Event | Gives you | When it fires |
|---|---|---|
| `Started` | `CardTableInfo` | A game has just started. |
| `Ended` | `CardGameEndedArgs` | A game has just finished. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CardTableInfo</code> <span>a snapshot of one card table</span></p>

`Tables` and `Active` give you one of these for each table. It is a snapshot, so ask again for fresh values.

<div class="api-pair" markdown="1">

```csharp
public sealed class CardTableInfo
{
    public string Id { get; }
    public CardGameKind Kind { get; }
    public int Players { get; }
    public int Stake { get; }
    public bool Active { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | A name for this table. Use it to tell tables apart. |
| `Kind` | `CardGameKind` | The game chosen at the table. `Unknown` when none is chosen. |
| `Players` | `int` | How many play, NPC opponents included. |
| `Stake` | `int` | What the player bet: coins, or a number of items for an item bet. `0` between games. |
| `Active` | `bool` | `true` while a game is running at the table. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CardGameKind</code> <span>which card game was played</span></p>

`CardTableInfo.Kind` and `CardGameEndedArgs.Kind` hold this, and `SetPayoutFactor` takes it. Each value is a card game the framework knows, and `Unknown` covers the rest.

<div class="api-pair" markdown="1">

```csharp
public enum CardGameKind
{
    Unknown = 0,
    ChinaHand = 1,
    Ristiseiska = 2
}
```

| Value | Meaning |
|---|---|
| `Unknown` | A card game the framework has no name for. |
| `ChinaHand` | The game players call Shittypants. |
| `Ristiseiska` | The game players call Crosseven. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CardGameEndedArgs</code> <span>how one finished game went</span></p>

`Ended` hands you this when a game finishes normally. `Payout` is what the game pays back for the player's place, not the profit.

<div class="api-pair" markdown="1">

```csharp
public sealed class CardGameEndedArgs
{
    public string TableId { get; }
    public CardGameKind Kind { get; }
    public bool PlayerWon { get; }
    public int Stake { get; }
    public int Payout { get; }
    public bool ItemBet { get; }
    public MoneyCurrency Currency { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `TableId` | `string` | The same value as `CardTableInfo.Id` for that table. |
| `Kind` | `CardGameKind` | Which game was played. |
| `PlayerWon` | `bool` | `true` when the player came first. |
| `Stake` | `int` | What the player bet. |
| `Payout` | `int` | What the game pays back for the player's place. This is not the profit. |
| `ItemBet` | `bool` | `true` when `Stake` and `Payout` count items, not coins. |
| `Currency` | `MoneyCurrency` | OC or RM, the coins that were bet. Ignore it when `ItemBet` is `true`. |

</div>
</div>

## Things to know

- `Tables()` lists the card tables in the scene you are in, and `Active()` is `null` when no game is running. On the main menu, or before a save is loaded, `Tables()` returns an empty list.
- `Id` is a name for one table. It stays the same for as long as the table stays where it is in the scene, between calls and between play sessions. The same value is in `Started` and `Ended`. It is made of the scene name and the table's place in the scene, for example `Bar:Room[2]/CardTable[0]`. <span class="tech">(It is the table's save GUID when one of its components has one. `CardGameTable` has none in the game's scenes as far as the framework knows, so the id is the scene id: the scene name, a colon, then each object from the root down with its sibling index.)</span>
- `Active` is `true` while a game is running. A table whose stake menu is open but whose game has not started reports `false`. <span class="tech">(`Active` is true when the game's state is anything except `NotStarted` or `GameEnded`.)</span>
- `Stake` is `0` when no game is running. That includes the first second after the player confirms the bet, before the game starts, and after a game was stopped early.
- The game never clears its own stake number when the player walks away from the table, so the framework reports `0` instead of the old value.
- `Started` fires when the game itself begins, about one second after the player's stake is taken. `Stake` already holds the bet. <span class="tech">(It fires after the game's own `StartGame` method on `ChinaHand` or `Ristiseiska`.)</span>
- `Ended` fires when a game finishes normally, and not when the player leaves the table mid-game. If the player walks away, the stake is lost and no event fires. <span class="tech">(It fires after `CardGameTable.OnGameFinished`. Leaving the table calls `CardGame.FinishGame(true)` from `CleanUp`, which does not reach it.)</span>
- `Payout` is what the game pays back for the player's place, not the profit. The game pays the stake times 2 for first place, 1.2 for second, 0.8 for third and 0 for fourth, rounded to a whole number. `PlayerWon` is `true` for first place.
- If a mod set a payout factor for that game, the payout is scaled by it as well. <span class="tech">(The multipliers are copied from `CardGameTable.OnGameFinished`, where they are a local array inside an `async` method, so the framework cannot read them from the game. If a game update changes them, `Payout` goes out of date until the copy is checked again.)</span>
- Events fire during a save load too. The framework does not hold them back. A handler that should ignore them can check whether the game is busy loading or saving. <span class="tech">(`SaveData.Busy` is true while a save is loading or being written.)</span>
