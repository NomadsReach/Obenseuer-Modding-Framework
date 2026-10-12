# Contests API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.contests`.
Files: `src/Api/Contests.cs`, `src/Api/ContestsPatches.cs`. Ids use `SceneIds.Of` from `src/Api/SceneIds.cs`.

The facade covers three kinds of contest: the drinking contest and the sauna contest (both run by `ContestController`), and the rat fight arena. The arena has its own betting and is not a `ContestController` contest, but it fits the same members. The facade is read and event only. Mods cannot start, stop or score a contest.

## Wiring

```csharp
internal static void Contests.Init();
```

Core calls this from `Core.Awake`.

`Contests.Init` installs every hook under one owner. If any hook or the private field it needs is missing (or has a different type), the owner is removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.contests")` becomes true, and one warning is logged. `Active`, `Participants` and `LastResult` keep working. Only `Started` and `Finished` stop.

## Threading and safety

- Main thread only.
- `Contests.IsAvailable` is false when the hooks were not installed or the owner was disabled. Reads are not affected.
- Every member is safe at any time. On the main menu or with no save loaded, `Active` and `Participants` return empty lists and `LastResult` returns `null`.
- Nothing throws to the caller. Each member catches its own exceptions and logs the first failure per member.
- Events are raised synchronously from the game's own call. `Events.Raise` isolates each handler, so one that throws is logged and the others still run.

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
    public static ContestResultInfo LastResult();                              // the last finished contest this session; null before the first
    public static event Action<ContestInfo> Started;
    public static event Action<ContestResultInfo> Finished;
}
```

## Ids

- A drinking or sauna contest has no id of its own in the game (`SaunaContest` is created with `new` for each run, `SaunaContestParticipant.cs:142`). The id is the kind name, a colon, and the id of the opposing NPC: `Sauna:<npc GUID>`. The NPC's save GUID is used when its object has one, otherwise the scene id `<scene>:<root>[i]/.../<node>[j]`. The id is the same in `Started`, `Active` and `Finished`, so a caller can keep it and ask `Participants` again.
- A rat fight uses the arena's save GUID (`RatFightArena.GUID`), or the scene id `<scene>:<root>[i]/.../<node>[j]` when the GUID is empty.
- `Participants(contest)` checks the id and the kind against what is running now. A `ContestInfo` from a finished contest, or from another contest of the same kind, returns an empty list.

## Events

- `Started(ContestInfo)`:
  - Drinking and sauna: after `ContestController.StartContest` returns (`ContestController.cs:72`). The participant is already set up.
  - Rat fight: after `RatFightArena.StartRound` moves the arena from `Betting` to `Fighting` (`RatFightArena.cs:118`, `:124`). A call on an arena that is not betting does not fire. An arena that is already `Fighting` when a save loads (`RatFightArena.cs:101`) does not fire, but `Active` lists it.
- `Finished(ContestResultInfo)`:
  - Drinking and sauna: when `ContestController.StopContest(playerWon)` runs (`ContestController.cs:93`). `Winner` is `""`.
    - `PlayerWon` is true only when the contest itself ends it because the opposing NPC's tolerance reached zero (`SaunaContest.cs:45`, `DrinkingContest.cs:116`). In the sauna the player does not have a tolerance meter: the NPC is overheated by the sauna and gives up, so `true` means the player outlasted the NPC.
    - `PlayerWon` is false when the player holds interact to give up (`ContestController.cs:149`), or in the drinking contest when the player's blood alcohol reaches 100 (`DrinkingContest.cs:105`). The sauna has no other losing path.
    - The game's only reaction to the flag is the participant's `TriggerWin` or `TriggerFail`, which fire the `onPlayerWin` or `onPlayerFail` relays authored on the NPC in the map, plus standing up from the chair on a loss (`ContestController.cs:104-119`). The game code gives no reward and shows no message itself. Rewards come from whatever the map wires to those relays.
  - Rat fight: when `RatFightArena.EndRound(winningCombatant)` runs in `Fighting` (`RatFightArena.cs:151`, `:157`). `Winner` is the combatant's name. `PlayerWon` is true when the player's bet was on that combatant and the bet was not empty (`RatFightArena.cs:174`). It is false both for a lost bet and for no bet at all. The winnings stay in the arena until the player collects them.

## Participants

- `Participants(contest)` for a drinking or sauna contest lists the one active participant, the NPC whose `npcInfo` the contest runs on. It is empty when that contest is not running.
- `Participants(contest)` for a rat fight lists the arena's `combatants` in list order (`RatFightArena.cs:67`).

## Not in this area

- Starting or stopping a contest, and the per-contest scoring (the sauna tolerance, the drink count). The game owns those.
- Betting on a rat fight (`PlaceOCBet`, `PlaceRMBet`, `CollectWinnings`). Those are the game's money path and are not wrapped.
- `ContestUI` and the arena's selection UI.

## Unverified in game

- Drinking contest `Started` and `Finished` with a real drinking round, and sauna with a real run to overheat.
- That `Participants` of a `ContestInfo` taken from `Started` still matches during the contest (the id is built from the NPC's GUID).
- Rat fight `PlayerWon` with a bet, and with no bet.
- The access check passes for this area. The private `activeParticipant` field is read through a cached `FieldInfo` checked for type, and injected as `___activeParticipant` in the stop patch. All four patched methods were applied against the real `Assembly-CSharp.dll` in a standalone check outside the game.
