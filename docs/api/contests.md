---
title: Contests API
parent: API reference
nav_order: 14
---

# Contests API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.contests`.
Files: `src/Api/Contests.cs`, `src/Api/ContestsPatches.cs`. Ids use the save GUID, or the scene path.

The facade covers three kinds of contest: the drinking contest and the sauna contest (both run by `ContestController`), and the rat fight arena. The arena has its own betting and is not a `ContestController` contest, but it fits the same members. The facade is read and event only, plus two writes: `GiveUp`, which ends the player's own contest as a loss, and a per-mod payout factor for a rat fight's winnings (see Payout factors). Mods cannot start a contest or score one.

## If a hook cannot be installed

The framework installs every hook under one owner. If any hook or the private field it needs is missing (or has a different type), the owner is removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.contests")` becomes true, and one warning is logged. `Active`, `Participants` and `LastResult` keep working. Only `Started` and `Finished` stop.

The payout factor hook is a separate owner, `obenseuer.moddingframework.contests.payout`. If it fails, `PayoutFactorsAvailable` is false and `SetPayoutFactor` returns false. Nothing else changes.

## Threading and safety

- Main thread only. `OpponentTolerance` and `GiveUp` are main thread only too, and neither supports rat fights.
- `SetPayoutFactor` and `Clear` are main thread only. They also work inside `Events.SceneLoaded` and while a save loads or saves, because the factors are not saved.
- `Contests.IsAvailable` is false when the hooks were not installed or the owner was disabled. Reads are not affected.
- Every member is safe at any time. On the main menu or with no save loaded, `Active` and `Participants` return empty lists and `LastResult` returns `null`.
- Nothing throws to the caller. Each member catches its own exceptions and logs the first failure per member.
- Events are raised synchronously from the game's own call. Each handler is isolated, so one that throws is logged and the others still run.

## Surface

```csharp
public enum ContestKind { Unknown = 0, Drinking = 1, Sauna = 2, RatFight = 3 }

public sealed class ContestInfo
{
    public string Id { get; }            // see Ids
    public ContestKind Kind { get; }
    public string Name { get; }          // the game's contest name; "Rat fight" for an arena
}

public sealed class ContestParticipantInfo
{
    public string Name { get; }          // the participant's NPC object name, or the combatant's name for a rat fight
}

public sealed class ContestResultInfo
{
    public ContestInfo Contest { get; }
    public bool PlayerWon { get; }       // see Finished below
    public string Winner { get; }        // the winning combatant's name for a rat fight; "" for a ContestController contest
}

public static class Contests
{
    public static bool IsAvailable { get; }
    public static List<ContestInfo> Active();                                  // every contest running now
    public static List<ContestParticipantInfo> Participants(ContestInfo contest); // empty for null, an unknown contest, or one that has finished
    public static float OpponentTolerance(ContestInfo contest);                // see Opponent tolerance and GiveUp; -1 when no tolerance is readable
    public static bool GiveUp();                                               // see Opponent tolerance and GiveUp
    public static ContestResultInfo LastResult();                              // the last finished contest this session; null before the first
    public static bool PayoutFactorsAvailable { get; }
    public static bool SetPayoutFactor(string modId, string contestId, float factor); // see Payout factors
    public static bool Clear(string modId);                                           // see Payout factors
    public static event Action<ContestInfo> Started;
    public static event Action<ContestResultInfo> Finished;
}
```

## Ids

- A drinking or sauna contest has no id of its own in the game (`SaunaContest` is created with `new` for each run, `SaunaContestParticipant`). The id is the kind name, a colon, and the id of the opposing NPC: `Sauna:<npc GUID>`. The NPC's save GUID is used when its object has one, otherwise the scene id `<scene>:<root>[i]/.../<node>[j]`. The id is the same in `Started`, `Active` and `Finished`, so a caller can keep it and ask `Participants` again.
- A rat fight uses the arena's save GUID (`RatFightArena.GUID`), or the scene id `<scene>:<root>[i]/.../<node>[j]` when the GUID is empty.
- `Participants(contest)` checks the id and the kind against what is running now. A `ContestInfo` from a finished contest, or from another contest of the same kind, returns an empty list.

## Events

- `Started(ContestInfo)`:
  - Drinking and sauna: after `ContestController.StartContest` returns. The participant is already set up.
  - Rat fight: after `RatFightArena.StartRound` moves the arena from `Betting` to `Fighting` (`RatFightArena`). A call on an arena that is not betting does not fire. An arena that is already `Fighting` when a save loads does not fire, but `Active` lists it.
- `Finished(ContestResultInfo)`:
  - Drinking and sauna: when `ContestController.StopContest(playerWon)` runs. `Winner` is `""`.
    - `PlayerWon` is true only when the contest itself ends it because the opposing NPC's tolerance reached zero. In the sauna the player does not have a tolerance meter: the NPC is overheated by the sauna and gives up, so `true` means the player outlasted the NPC.
    - `PlayerWon` is false when the player holds interact to give up, when a mod calls `Contests.GiveUp`, or in the drinking contest when the player's blood alcohol reaches 100. The sauna has no other losing path.
    - The game's only reaction to the flag is the participant's `TriggerWin` or `TriggerFail`, which fire the `onPlayerWin` or `onPlayerFail` relays authored on the NPC in the map, plus standing up from the chair on a loss. The game code gives no reward and shows no message itself. Rewards come from whatever the map wires to those relays.
  - Rat fight: when `RatFightArena.EndRound(winningCombatant)` runs in `Fighting` (`RatFightArena`). `Winner` is the combatant's name. `PlayerWon` is true when the player's bet was on that combatant and the bet was not empty. It is false both for a lost bet and for no bet at all. The winnings stay in the arena until the player collects them.

## Participants

- `Participants(contest)` for a drinking or sauna contest lists the one active participant, the NPC whose `npcInfo` the contest runs on. It is empty when that contest is not running.
- `Participants(contest)` for a rat fight lists the arena's `combatants` in list order.

## Opponent tolerance and GiveUp

- `OpponentTolerance(contest)` returns the opposing NPC's remaining tolerance, from 0 to 1, read from the running contest's participant. It is 1 when the contest starts, and the contest ends with `PlayerWon` true when it reaches 0.
  - Sauna: the NPC's `tolerance`. It falls while the room is overheated and recovers while it cools (`SaunaContest`), so it can rise again during the contest.
  - Drinking: the NPC's `tolerance`, set to `1 - AlcoholInBlood / 100`.
  - Returns -1 for a null contest, a rat fight, a contest that is not running now (the same id and kind check as `Participants`), or when the participant field could not be read.
- `GiveUp()` ends the running `ContestController` contest as a loss, the same as holding interact. It calls `StopContest(false)`, so `Finished` fires with `PlayerWon` false.
  - Returns true when the contest was stopped. Returns false while `SaveData.Busy`, when no contest is running, or when the participant is missing. The participant check is needed because the game's stop path would throw without one.
  - A rat fight cannot be given up through this call.
  - From a `Finished` handler it returns false, because the contest is already cleared. From a `Started` handler it works, because `Started` runs after `StartContest` returns.

## Payout factors

- `SetPayoutFactor(modId, contestId, factor)` scales what the player collects from a rat fight. `contestId` is the rat fight's `ContestInfo.Id` (see Ids), and the arena must be loaded in the current scene. `factor` is from 0 to 4, and 1 is the game's amount. It returns false for an invalid mod id, a factor outside that range, an id that matches no loaded arena, or when the payout hook is not installed. It works inside `Events.SceneLoaded` and while a save loads or saves.
- The factor is applied to the win the game sets when the round ends (`RatFightArena.winAmount`). The player then collects the scaled amount through the game's own `CollectWinnings`. The bet itself is not changed, and a lost bet stays lost.
- A scaled win of zero leaves nothing to collect. The arena goes back to betting at once, as after a loss, so the player can bet again.
- `Finished` still reports `PlayerWon` as the game decided it. The factor changes the amount, not the result. At factor 0 the bet is still a win, so `PlayerWon` is true and the player collects nothing.
- The game's gambling statistics record the unscaled win, as the game computed it before the factor. A factor changes what the player collects, not the recorded win.
- Newest call wins. Each mod has one factor per arena, and a call from the same mod replaces its own. When two mods set the same arena, the newest factor applies and one warning names both mods, logged once per pair and arena.
- `Clear(modId)` removes that mod's factors on every arena. It returns true when it removed at least one, and false for an invalid mod id or when that mod had none.
- Factors last for this session. They are cleared on each Single scene load (`Events.SceneLoaded`), because arena ids are per scene.
- The game's gambling statistics keep the game's amount. The game records the round before the factor applies, so the `OCWon` total and the satisfaction value do not change. Only the amount collected changes.

## Not in this area

- Starting a contest, and the per-contest scoring (the sauna tolerance, the drink count). The game owns those. Stopping is only `GiveUp`, which makes the same `StopContest(false)` call as the hold-to-give-up path.
- Betting on a rat fight (`PlaceOCBet`, `PlaceRMBet`, `CollectWinnings`). Those are the game's money path and are not wrapped.
- Rat fight odds. The fight decides the winner (the round coroutine ends it when at most one combatant is still spawned), and the arena has no chance value to set.
- `ContestUI` and the arena's selection UI.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
