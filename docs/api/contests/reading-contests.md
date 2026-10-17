---
title: Reading contests
parent: Contests
grand_parent: Player life and skills
nav_order: 2
---

# Reading contests

This is the full list of what the API gives you. In the code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

Each running contest is a `ContestInfo`, a snapshot taken when you asked. The snapshot does not update by itself, so ask again for fresh values. When a contest ends you get a `ContestResultInfo`.

```csharp
public enum ContestKind
{
    Unknown = 0,    // a contest the framework has no name for
    Drinking = 1,   // the drinking contest
    Sauna = 2,      // the sauna contest
    RatFight = 3    // the rat fight arena
}

public sealed class ContestInfo    // one contest
{
    public string Id { get; }          // a name for this contest; the same in Started, Active and Finished
    public ContestKind Kind { get; }   // which sort of contest it is
    public string Name { get; }        // the name the game gives it; "Rat fight" for an arena
}

public sealed class ContestParticipantInfo    // one person or combatant in a contest
{
    public string Name { get; }    // the NPC's name in the scene, or the rat's name in a rat fight
}

public sealed class ContestResultInfo    // how a finished contest went
{
    public ContestInfo Contest { get; }   // which contest it was
    public bool PlayerWon { get; }        // true when the player won (see below for what that means in each contest)
    public string Winner { get; }         // the winning rat's name in a rat fight; "" in a drinking or sauna contest
}

public static class Contests
{
    public static bool IsAvailable { get; }               // false when the Started and Finished events could not be set up
    public static bool PayoutFactorsAvailable { get; }    // false when rat fight payout factors could not be set up

    public static List<ContestInfo> Active();                                    // every contest running now
    public static List<ContestParticipantInfo> Participants(ContestInfo contest);// who is in the contest; empty when it is not running
    public static float OpponentTolerance(ContestInfo contest);                  // how much the opponent has left, 0 to 1; -1 when it cannot be read
    public static bool GiveUp();                                                 // ends the player's contest as a loss; true when it did
    public static ContestResultInfo LastResult();                                // the last contest that finished; null before the first

    public static event Action<ContestInfo> Started;         // a contest has just started
    public static event Action<ContestResultInfo> Finished;  // a contest has just finished

    // Scales what the player collects from a rat fight. Returns true when it was set.
    public static bool SetPayoutFactor(string modId, string contestId, float factor);
    // Takes back every payout factor your mod set. Returns true when there was something to remove.
    public static bool Clear(string modId);
}
```

- **A drinking or sauna contest has an id made from its kind and its opponent.** The game has no id of its own for it. The id looks like `Sauna:` followed by a name for the opposing NPC. It is the same in `Started`, `Active` and `Finished`, so you can keep it and ask `Participants` again.
  - Technical: the game creates a new `SaunaContest` for each run, so there is nothing to give it an id. The framework uses the kind name, a colon and the opposing NPC's save GUID, or the scene id (the scene name, a colon, then each object from the root down with its sibling index) when the NPC's object has no GUID.
- **A rat fight uses the arena's own id.** If the arena has no id of its own, it gets one made from its place in the scene.
  - Technical: it is `RatFightArena.GUID`, else the scene id.
- **`Participants()` only works for a contest that is running now.** It checks the contest's id and kind against what is running. A `ContestInfo` from a contest that has finished, or from a different contest of the same kind, gives an empty list. A drinking or sauna contest lists its one opponent. A rat fight lists all the rats in the order the arena has them.
- **`Active()` lists a rat fight only while the rats are fighting.** An arena that is taking bets is not listed. If a save loads while a fight is going, `Active()` lists it, but `Started` does not fire for it.
- **`Started` fires when a contest begins.** For the drinking and sauna contest it fires once the contest is fully set up, so the opponent is ready. For a rat fight it fires when the round goes from betting to fighting.
  - Technical: for drinking and sauna it fires after `ContestController.StartContest` returns. For a rat fight it fires after `RatFightArena.StartRound` when the arena was in its `Betting` state, so a call on an arena that is not betting does not fire.
- **`Finished` fires when a contest ends.** For a drinking or sauna contest, `PlayerWon` is `true` only when the opponent runs out of tolerance (how much they can take before they quit). In the sauna the player has no meter: the sauna overheats the NPC until they give up, so `true` means the player outlasted them. `PlayerWon` is `false` when the player holds the interact key to give up, when a mod calls `GiveUp()`, or, in the drinking contest, when the player's blood alcohol reaches 100. The sauna has no other way to lose.
  - Technical: it fires after `ContestController.StopContest(playerWon)`. `Winner` is `""`.
- **The game gives no reward or message when a contest ends.** It only runs the scripted actions the map attaches to the opponent, one for a win and one for a loss, and a lost player stands up from the chair. Rewards come from whatever the map connects to those actions, or from your mod.
  - Technical: the participant's `TriggerWin` or `TriggerFail` fires the `onPlayerWin` or `onPlayerFail` relays authored on the NPC.
- **For a rat fight, `PlayerWon` is `true` only when the player bet on the winning rat.** It is `false` both for a lost bet and for no bet at all. `Winner` is the winning rat's name. The winnings wait in the arena until the player collects them.
  - Technical: it fires after `RatFightArena.EndRound(winningCombatant)` ran in the `Fighting` state. `PlayerWon` is true when `playerBettingOn` equals the winner and `betAmount` is not zero.
- **`OpponentTolerance()` tells you how much the opponent can still take, from 0 to 1.** It is `1` when the contest starts. The player wins when it reaches 0. It returns `-1` for null, for a rat fight, for a contest that is not running now, and when the framework cannot read it.
  - In the sauna it falls while the room is overheated and recovers while it cools, so it can go back up.
  - In the drinking contest it follows the opponent's blood alcohol.
  - Technical: sauna uses `tolerance` of the participant. Drinking sets `tolerance` to `1 - AlcoholInBlood / 100`.
- **`GiveUp()` ends the player's drinking or sauna contest as a loss, the same as holding the interact key.** `Finished` fires with `PlayerWon` set to `false`. It returns `true` when the contest was stopped. It returns `false` while a save is loading or saving, when no contest is running, or when the contest has no opponent. A rat fight cannot be given up this way.
  - Technical: it calls `ContestController.StopContest(false)`. The opponent check is needed because the game's stop code would throw without one. From a `Finished` handler it returns `false`, because the contest is already cleared. From a `Started` handler it works.
- **`LastResult()` is not saved.** It only remembers the last contest since the game was started, and it is `null` before the first one.
