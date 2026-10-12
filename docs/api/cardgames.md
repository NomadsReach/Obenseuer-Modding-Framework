# Card games API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.cardgames`.
Files: `src/Api/CardGames.cs`, `src/Api/CardGamesPatches.cs`. Table ids use `SceneIds.Of` from `src/Api/SceneIds.cs`.

The facade covers the card tables and their two games, China Hand and Ristiseiska. It is read and event only. Mods cannot start a game, deal, play a card or set a stake.

## Wiring

```csharp
internal static void CardGames.Init();
```

Core calls this from `Core.Awake`.

`CardGames.Init` installs every hook under one owner. If any hook is missing, the owner is removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.cardgames")` becomes true, and one warning is logged. `Tables` and `Active` keep working. Only `Started` and `Ended` stop.

## Threading and safety

- Main thread only.
- `CardGames.IsAvailable` is false when the hooks were not installed or the owner was disabled. Reads are not affected.
- Every member is safe at any time. On the main menu or with no save loaded, `Tables` returns an empty list and `Active` returns `null`.
- Nothing throws to the caller. Each member catches its own exceptions and logs the first failure per member.
- Events are raised synchronously from the game's own call. `Events.Raise` isolates each handler, so one that throws is logged and the others still run.

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
    public static List<CardTableInfo> Tables();   // every card table in the loaded scene
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

- `Started(CardTableInfo)`: the game's own `StartGame` ran (`ChinaHand.cs:44`, `Ristiseiska.cs:42`). That is one second after the stake is taken (`CardGameTable.cs:316`, `CardGame.cs:200-202`). The stake is in `Stake` already.
- `Ended(CardGameEndedArgs)`: the table finished the game (`CardGameTable.OnGameFinished`, `CardGameTable.cs:366`). It fires for a normal finish only. A forced finish (`CardGame.FinishGame(true)`, used when the player leaves the table, `CardGameTable.cs:589`, from `CleanUp`) does not reach it: the stake is lost and no event fires.
  - `Payout` is the stake times the placing's multiplier, rounded like the game: 2, 1.2, 0.8 and 0 for placings 1 to 4. It is what the game pays back, not the net gain. The multipliers are copied from `CardGameTable.cs:368-369` because they are a local array literal inside the `async` method `OnGameFinished`, not a field, so nothing can be read or injected. `Payout` goes stale if the game changes these values. Re-check the array when the game build changes.
  - `PlayerWon` is the same test as the table's win trigger (`CardGameTable.cs:383`).

## Not in this area

- Starting a game (`CardGameTable.StartSelectedGame`). It needs a seated player, the stake menu and the money or item check. That path is a UI flow, not a clean start method, so no start member is exposed.
- Card play, dealing and turn events. The game's own `CardGame` events (`OnCardPlayed`, `OnTurnStarted`) are static game events and would expose game types. Add them when a mod needs them.

## Unverified in game

- `Started` on a real table after the stake menu: one China Hand round and one Ristiseiska round.
- `Ended` payout: one win and one loss at a table with money, compared with the Money total.
- Item bets: `ItemBet` true with an item stake.
- The access check passes for this area. The private table fields are read by cached `FieldInfo`s checked for name and type in `Install`. All three patches were applied against the real `Assembly-CSharp.dll` in a standalone check outside the game.
