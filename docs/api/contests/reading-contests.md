---
title: Reading contests
parent: Contests
grand_parent: Player life and skills
nav_order: 2
---

# Reading contests

Each running contest is a `ContestInfo`, a snapshot taken when you asked. The snapshot does not update by itself, so ask again for fresh values. When a contest ends you get a `ContestResultInfo`.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ContestKind</code> <span>the sort of contest</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum ContestKind
{
    Unknown = 0,
    Drinking = 1,
    Sauna = 2,
    RatFight = 3
}
```

| Value | Meaning |
|---|---|
| `Unknown` | A contest the framework has no name for. |
| `Drinking` | The drinking contest. |
| `Sauna` | The sauna contest. |
| `RatFight` | The rat fight arena. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ContestInfo</code> <span>one contest</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ContestInfo
{
    public string Id { get; }
    public ContestKind Kind { get; }
    public string Name { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | A name for this contest. It is the same in `Started`, `Active` and `Finished`. |
| `Kind` | `ContestKind` | Which sort of contest it is. |
| `Name` | `string` | The name the game gives it. It is "Rat fight" for an arena. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ContestParticipantInfo</code> <span>one person or combatant in a contest</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ContestParticipantInfo
{
    public string Name { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Name` | `string` | The NPC's name in the scene, or the rat's name in a rat fight. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ContestResultInfo</code> <span>how a finished contest went</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ContestResultInfo
{
    public ContestInfo Contest { get; }
    public bool PlayerWon { get; }
    public string Winner { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Contest` | `ContestInfo` | Which contest it was. |
| `PlayerWon` | `bool` | `true` when the player won. See below for what that means in each contest. |
| `Winner` | `string` | The winning rat's name in a rat fight. It is `""` in a drinking or sauna contest. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Contests</code> <span>contests running now, and their results</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Contests
{
    public static bool IsAvailable { get; }
    public static bool PayoutFactorsAvailable { get; }

    public static List<ContestInfo> Active();
    public static List<ContestParticipantInfo> Participants(ContestInfo contest);
    public static float OpponentTolerance(ContestInfo contest);
    public static bool GiveUp();
    public static ContestResultInfo LastResult();
    public static bool SetPayoutFactor(string modId, string contestId, float factor);
    public static bool Clear(string modId);

    public static event Action<ContestInfo> Started;
    public static event Action<ContestResultInfo> Finished;
}
```

| Method | Returns | What it does |
|---|---|---|
| `Active()` | `List<ContestInfo>` | Every contest running now. |
| `Participants(ContestInfo contest)` | `List<ContestParticipantInfo>` | Who is in the contest. It is empty when it is not running. |
| `OpponentTolerance(ContestInfo contest)` | `float` | How much the opponent has left, from 0 to 1. It is `-1` when it cannot be read. |
| `GiveUp()` | `bool` | Ends the player's contest as a loss. It is `true` when it did. |
| `LastResult()` | `ContestResultInfo` | The last contest that finished. It is `null` before the first. |
| `SetPayoutFactor(string modId, string contestId, float factor)` | `bool` | Scales what the player collects from a rat fight. It is `true` when it was set. |
| `Clear(string modId)` | `bool` | Takes back every payout factor your mod set. It is `true` when there was something to remove. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the `Started` and `Finished` events could not be set up. |
| `PayoutFactorsAvailable` | `bool` | `false` when rat fight payout factors could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Started` | `ContestInfo` | A contest has just started. |
| `Finished` | `ContestResultInfo` | A contest has just finished. |

</div>
</div>

## Things to know

- A drinking or sauna contest has an id made from its kind and its opponent. The game has no id of its own for it. The id looks like `Sauna:` followed by a name for the opposing NPC. It is the same in `Started`, `Active` and `Finished`, so you can keep it and ask `Participants` again. <span class="tech">(The game creates a new `SaunaContest` for each run, so there is nothing to give it an id. The framework uses the kind name, a colon and the opposing NPC's save GUID, or the scene id (the scene name, a colon, then each object from the root down with its sibling index) when the NPC's object has no GUID.)</span>
- A rat fight uses the arena's own id. If the arena has no id of its own, it gets one made from its place in the scene. <span class="tech">(It is `RatFightArena.GUID`, else the scene id.)</span>
- `Participants()` only works for a contest that is running now. It checks the contest's id and kind against what is running. A `ContestInfo` from a contest that has finished, or from a different contest of the same kind, gives an empty list. A drinking or sauna contest lists its one opponent. A rat fight lists all the rats in the order the arena has them.
- `Active()` lists a rat fight only while the rats are fighting. An arena that is taking bets is not listed. If a save loads while a fight is going, `Active()` lists it, but `Started` does not fire for it.
- `Started` fires when a contest begins. For the drinking and sauna contest it fires once the contest is fully set up, so the opponent is ready. For a rat fight it fires when the round goes from betting to fighting. <span class="tech">(For drinking and sauna it fires after `ContestController.StartContest` returns. For a rat fight it fires after `RatFightArena.StartRound` when the arena was in its `Betting` state, so a call on an arena that is not betting does not fire.)</span>
- `Finished` fires when a contest ends. For a drinking or sauna contest, `PlayerWon` is `true` only when the opponent runs out of tolerance (how much they can take before they quit).
- In the sauna the player has no meter: the sauna overheats the NPC until they give up, so `true` means the player outlasted them. The sauna has no other way to lose.
- The game gives no reward or message when a contest ends. It only runs the scripted actions the map attaches to the opponent, one for a win and one for a loss, and a lost player stands up from the chair. Rewards come from whatever the map connects to those actions, or from your mod. <span class="tech">(The participant's `TriggerWin` or `TriggerFail` fires the `onPlayerWin` or `onPlayerFail` relays authored on the NPC.)</span>
- For a rat fight, `PlayerWon` is `true` only when the player bet on the winning rat. It is `false` both for a lost bet and for no bet at all. `Winner` is the winning rat's name. The winnings wait in the arena until the player collects them. <span class="tech">(It fires after `RatFightArena.EndRound(winningCombatant)` ran in the `Fighting` state. `PlayerWon` is true when `playerBettingOn` equals the winner and `betAmount` is not zero.)</span>
- `OpponentTolerance()` tells you how much the opponent can still take, from 0 to 1. It is `1` when the contest starts. The player wins when it reaches 0.
- In the sauna the tolerance falls while the room is overheated and recovers while it cools, so it can go back up. In the drinking contest it follows the opponent's blood alcohol. <span class="tech">(Sauna uses `tolerance` of the participant. Drinking sets `tolerance` to `1 - AlcoholInBlood / 100`.)</span>
- `LastResult()` is not saved. It only remembers the last contest since the game was started, and it is `null` before the first one.

## Limits

- `PlayerWon` is `false` when the player holds the interact key to give up, when a mod calls `GiveUp()`, or, in the drinking contest, when the player's blood alcohol reaches 100. <span class="tech">(It fires after `ContestController.StopContest(playerWon)`. `Winner` is `""`.)</span>
- `OpponentTolerance()` returns `-1` for a `null` contest, for a rat fight, for a contest that is not running now, and when the framework cannot read it.
- `GiveUp()` ends the player's drinking or sauna contest as a loss, the same as holding the interact key. `Finished` fires with `PlayerWon` set to `false`. It returns `true` when the contest was stopped. It returns `false` while a save is loading or saving, when no contest is running, or when the contest has no opponent. A rat fight cannot be given up this way. <span class="tech">(It calls `ContestController.StopContest(false)`. The opponent check is needed because the game's stop code would throw without one. From a `Finished` handler it returns `false`, because the contest is already cleared. From a `Started` handler it works.)</span>
