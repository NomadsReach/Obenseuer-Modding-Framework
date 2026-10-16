---
title: Casino API
parent: API reference
nav_order: 9
---

# Casino API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.casino`.
Files: `src/Api/Casino.cs`, `src/Api/CasinoPatches.cs`.

The facade covers the placed machines: the slot machine and the coin pusher (Pajatso). Gambling stats (`PlayerStatsGambling`, the gambling need and addiction levels) are not wrapped here. `Survival` covers only the gambling need and addiction levels, not the tallies.

## Threading and safety

- Main thread only.
- `Casino.IsAvailable` is false when a required group (slot, pajatso or scratch) was not installed or the owner was disabled. The optional doubling hook does not count. A missing group stops only its own events, and reads are not affected. `LastPayout` changes only when a win's group is installed. If the doubling hook is missing, only `Doubled` stops.
- Every member is safe at any time. On the main menu or with no save loaded, `Machines` returns an empty list and `LastPayout` returns `0`.
- Nothing throws to the caller. Each member catches its own exceptions and logs the first failure per member.
- Events are raised synchronously from the game's own call, after the game method has run far enough to know the outcome. Each handler is isolated, so one that throws is logged and the others still run.
- The hooks build nothing and allocate nothing when an event has no subscriber (the coin pusher hooks can fire many times a second). `LastPayout` is still kept up to date.
- The facade has one write, `SetPayoutFactor` (see Payout factors). Mods cannot set a stake, a machine's money or the odds. Other writes stay in the game (the player's own input).

## Surface

```csharp
public enum CasinoMachineKind { Slot = 0, Pajatso = 1 }

public sealed class CasinoMachineInfo
{
    public string Id { get; }            // see Machines
    public CasinoMachineKind Kind { get; }
    public int Stake { get; }            // Slot: the stake selected now; Pajatso: 1 coin per launch
}

public sealed class CasinoPlayedArgs
{
    public CasinoMachineInfo Machine { get; }
    public int Stake { get; }
}

public sealed class CasinoWonArgs
{
    public CasinoMachineInfo Machine { get; }
    public int Payout { get; }
}

public sealed class CasinoCashedOutArgs
{
    public CasinoMachineInfo Machine { get; }
    public int Amount { get; }           // OC added to the wallet
}

public sealed class CasinoDoubledArgs
{
    public CasinoMachineInfo Machine { get; }
    public int Before { get; }           // round winnings before the double
    public int After { get; }            // round winnings after the double
    public bool Won { get; }             // After > Before
}

public sealed class ScratchcardArgs
{
    public int CardItemId { get; }
    public string CardTitle { get; }
    public bool Won { get; }             // Prize > 0
    public int Prize { get; }            // the game's payout: RM when PrizeInRm, else OC
    public bool PrizeInRm { get; }
    public int PrizeOc { get; }          // Prize in OC (the same value when PrizeInRm is false)
    public int Cost { get; }             // the card's value, OC
    public int ReceiptItemId { get; }    // -1 when the card has no receipt; best effort, see Scratched
}

public static class Casino
{
    public static bool IsAvailable { get; }
    public static bool PayoutFactorsAvailable { get; }   // true when the payout hooks are installed
    public static bool SetPayoutFactor(string modId, CasinoMachineKind kind, float factor);   // 0 to 4, see Payout factors
    public static bool Clear(string modId);              // removes every payout factor this mod set
    public static List<CasinoMachineInfo> Machines();    // every slot machine and coin pusher in the active scene
    public static int LastPayout { get; }                 // payout of the last Won event this session; 0 before the first win; not saved
    public static event Action<CasinoPlayedArgs> Played;
    public static event Action<CasinoWonArgs> Won;
    public static event Action<CasinoCashedOutArgs> CashedOut;
    public static event Action<CasinoDoubledArgs> Doubled;
    public static event Action<ScratchcardArgs> Scratched;
}
```

Amounts are in OC. Both machines take and pay OC (`SlotMachineGameplay` cashes out with `Money.AddMoney(..., RM: false)`, `PajatsoGameplay.OnCollectWins` does the same).

## Events

- `Played(CasinoPlayedArgs)`: a paid round starts.
  - Slot machine: the stake was taken from the machine's pot for a round (`SlotMachineGameplay.ChargeMoney` returned true). `Stake` is that stake.
  - Coin pusher: one coin was launched. `Stake` is `1`.
  - A refused start (not enough money in the machine, or the machine is busy) does not fire.
- `Won(CasinoWonArgs)`: a win was credited inside the machine. It is not a transfer to the player.
  - Slot machine: the round's win, before any doubling. `Payout` is the round's reward (`SlotMachineGameplay.HandleWinning`). The player may double it, lose it, or keep it, and the money reaches the wallet only at the cash-out. Doubling results are reported by `Doubled`, not as another `Won`.
  - Coin pusher: a gate hit paid `Payout` coins into the win pile (`PajatsoGameplay.CoinGateHit`). The pile reaches the wallet when the player collects it. A gate hit with an empty gutter does not fire.
- `CashedOut(CasinoCashedOutArgs)`: the winnings reached the wallet. Use this event for what the player actually received.
  - Slot machine: the player pressed cash out (`SlotMachineGameplay.HandleQuittingProcedure`). `Amount` is the whole machine balance, which includes inserted money and any doubled winnings.
  - Coin pusher: the player collected the win pile (`PajatsoGameplay.OnCollectWins`). `Amount` is the pile. An empty pile does not fire.
- `Doubled(CasinoDoubledArgs)`: the player's double on a slot round was resolved (`SlotMachineGameplay.HandleDoublingResult`). `Before` is the round's winnings before the double and `After` is the winnings after it. The round's winnings are still in the machine, so the wallet does not change here.
  - `Won` is `After > Before`. A coin edge gives `After == Before`: the game takes its failure path but keeps the winnings, so `Won` is false and nothing is lost. A wrong guess gives `After` of `0`.
  - A double with `Before` of `0` does not fire. The game logs "Trying to double without any winnings" for that case.
- `Scratched(ScratchcardArgs)`: a scratchcard was used (`Scratchcard.Use`). It fires for every use, win or not, while the card is still in the inventory.
  - `Prize` is the payout as the game computes it. When the card pays RM, `PrizeInRm` is true and `PrizeOc` converts it with `Money.RmToOcConvert`.
  - `Won` is `Prize > 0`. The receipt the game writes can have `canBeCashedOut` true with a zero prize (when the card's `val1` is 0), so `Won` is not the receipt flag.
  - `Cost` is the card's value. The game charges it to the gambling losses, even on a win.
  - `ReceiptItemId` is the `ID` of the card's receipt reference. It is best effort: a name-based reference may not match the resolved item. It is `-1` when the card has no receipt reference or the id is not set.
  - The payout is read from the gambling tally: `OCWon` before and after `Use`. Any other change to `OCWon` during `Use` (only another mod's patch can make one) is counted as the prize.
  - A call to `Scratchcard.Use` from another mod also fires the event.
- All events fire on the main thread from inside the game call.

## Machines

- `Machines` lists both kinds.
- `Id` is the machine's save GUID when one of its components has one. The slot machine and the coin pusher have none in the game's scenes as far as the framework knows, so the id is the scene id: the scene name, a colon, then each node from the root down with its sibling index (for example `Bar:Room[2]/SlotMachine[0]`). It is stable between calls and between sessions for a machine that stays in the scene hierarchy, and it is the same value the events report.
- `Stake` for a slot machine is read at call time. It changes when the player raises the stake, so an old `CasinoMachineInfo` is a snapshot.

## Payout factors

`SetPayoutFactor(modId, kind, factor)` sets one mod's payout factor for one machine kind. The factor is from 0 to 4, and `1` is the game's own payout. It returns `false` for an invalid mod id, a factor outside 0 to 4, an unknown kind, or `PayoutFactorsAvailable` being false.

- **Newest call wins.** Each mod keeps one factor per kind, and for a kind the newest call applies. When a second mod sets a kind that another mod already set, one warning names both mods, and the second mod's factor applies. `Clear(modId)` removes that mod's factors for every kind, and the factor set before it applies again. A mod that calls `SetPayoutFactor` again replaces its own factor with no warning.
- **Slot machine.** The factor multiplies the round's win where the game computes it (`SlotMachineHelper.CalculateReward`, `HandleWinning`). The doubling, the cash-out and the gambling tally all use the factored win, and `Won` reports it as `Payout`.
- **Coin pusher.** The factor multiplies the coins of each gate hit before the gate's coins drop (`PajatsoGameplay.CoinGateHit`). The pile, the collect and the gambling tally all use the factored coins, and `Won` reports them. Each coin still comes out of the machine's gutter, so a factor above 1 drains more gutter coins per hit. When the gutter runs out part way through a hit, the game reports "No coins left in gutter", and the coins it already dropped are not paid. So a factor above 1 can turn a win into "no coins left". A factor that rounds a gate's coins to 0 still plays the win sound and shows "Collect 0 OC". `Won` does not fire for it, because a hit with a payout of 0 is not reported.
- **Not for scratchcards.** A scratchcard's prize is a receipt that is cashed out later, so no factor reaches it.
- **Rounding and cap.** The factor is applied to the game's whole-coin amount and rounded to the nearest whole number (`Math.Round`). The result is capped at 16000000.
- **Session only.** Factors are not saved. Every Single scene load clears them, so set them again from your `Events.SceneLoaded` handler.
- **Per kind, not per machine.** The game's reward function is static and receives no machine, so one factor covers every slot machine in the scene, and one covers every coin pusher.

## Not in this area

- Gambling tallies and addiction stats: `PlayerStatsGambling` and the gambling need. `Survival` covers only the need and addiction levels.
- Odds. There is no odds write, by design. The slot's win chance is a fixed value in the game code, so changing it needs a transpiler. The scratchcard's odds are read from item data.
- Scratchcard prizes. See Payout factors.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
