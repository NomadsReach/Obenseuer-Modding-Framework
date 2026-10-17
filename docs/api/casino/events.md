---
title: Events
parent: Casino
grand_parent: Money and shops
nav_order: 3
---

# Events

Amounts are in OC. All events fire from your mod's normal code, inside the game's own call. If one of your handlers throws an error, the framework logs it and still runs the others.

```csharp
public sealed class CasinoPlayedArgs    // what Played tells you
{
    public CasinoMachineInfo Machine { get; }   // the machine
    public int Stake { get; }                   // the stake of this round, in OC
}

public sealed class CasinoWonArgs    // what Won tells you
{
    public CasinoMachineInfo Machine { get; }   // the machine
    public int Payout { get; }                  // the win, in OC
}

public sealed class CasinoCashedOutArgs    // what CashedOut tells you
{
    public CasinoMachineInfo Machine { get; }   // the machine
    public int Amount { get; }                  // the OC that went into the wallet
}

public sealed class CasinoDoubledArgs    // what Doubled tells you
{
    public CasinoMachineInfo Machine { get; }   // the slot machine
    public int Before { get; }                  // the winnings of the round before the double
    public int After { get; }                   // the winnings of the round after the double
    public bool Won { get; }                    // true when After is more than Before
}

public sealed class ScratchcardArgs    // what Scratched tells you
{
    public int CardItemId { get; }       // the item number of the card
    public string CardTitle { get; }     // the card's name
    public bool Won { get; }             // true when the card paid a prize
    public int Prize { get; }            // the prize the game paid: in RM when PrizeInRm is true, otherwise in OC
    public bool PrizeInRm { get; }       // true when the prize is in RM (Real Money)
    public int PrizeOc { get; }          // the prize worth in OC (the same as Prize when PrizeInRm is false)
    public int Cost { get; }             // the card's value, in OC
    public int ReceiptItemId { get; }    // the item number of the card's receipt; -1 when the card has none or it is not known
}

public static class Casino
{
    public static event Action<CasinoPlayedArgs> Played;         // a paid round has started
    public static event Action<CasinoWonArgs> Won;               // a win was credited inside a machine
    public static event Action<CasinoCashedOutArgs> CashedOut;   // winnings reached the player's wallet
    public static event Action<CasinoDoubledArgs> Doubled;       // the player's double on a slot win was settled
    public static event Action<ScratchcardArgs> Scratched;       // a scratchcard was used

    public static int LastPayout { get; }                        // the payout of the last Won event this session; 0 before the first win; not saved
    public static bool IsAvailable { get; }                      // false when a needed event could not be set up
}
```

- **`Played` fires when a paid round starts.** For a slot machine, the stake was taken from the machine's pot for a round, and `Stake` is that stake. For the coin pusher, one coin was launched and `Stake` is `1`. If the round is refused (the machine has too little money, or it is busy), nothing fires.
  - Technical: for the slot machine, `SlotMachineGameplay.ChargeMoney` returned true.
- **`Won` fires when a win is credited inside the machine. It is not money going to the player.** The money reaches the wallet only at `CashedOut`.
  - For a slot machine, `Payout` is the round's win before any doubling. The player may double it, lose it or keep it. Doubling results are reported by `Doubled`, not as another `Won`.
  - For the coin pusher, a gate hit paid `Payout` coins into the win pile. The pile reaches the wallet when the player collects it. A gate hit when the gutter has no coins does not fire.
  - Technical: for the slot machine, the round's reward is handled in `SlotMachineGameplay.HandleWinning`. For the coin pusher, it is `PajatsoGameplay.CoinGateHit`.
- **`CashedOut` fires when the winnings reach the wallet. Use it for what the player really received.**
  - For a slot machine, the player pressed cash out. `Amount` is the whole machine balance, including the money the player put in and any doubled winnings.
  - For the coin pusher, the player collected the win pile. `Amount` is the pile. An empty pile does not fire.
  - Technical: for the slot machine, it is raised around `SlotMachineGameplay.HandleQuittingProcedure`. For the coin pusher, it is `PajatsoGameplay.OnCollectWins`.
- **`Doubled` fires when the player's double on a slot round is settled.** The round's winnings are still in the machine, so the wallet does not change here. If the coin lands on its edge, `After` equals `Before`: the winnings are kept and `Won` is `false`. A wrong guess gives `After` of `0`. A double with nothing to double does not fire.
  - Technical: it is raised from `SlotMachineGameplay.HandleDoublingResult`. The game logs "Trying to double without any winnings" when `Before` is `0`.
- **`Scratched` fires every time a scratchcard is used, win or not.** It fires while the card is still in the inventory.
  - Technical: it is raised around `Scratchcard.Use`. A call to `Scratchcard.Use` from another mod also fires it.
- **`Scratched.Prize` is what the game paid. A card that pays RM sets `PrizeInRm` to `true` and `PrizeOc` converts it to OC.** `Won` is `true` when the prize is above 0. It is not the same as the receipt's "can be cashed out" flag, which can be on for a zero prize.
  - Technical: `PrizeOc` uses `Money.RmToOcConvert`. A receipt can have `canBeCashedOut` true with a zero prize when the card's `val1` is 0.
- **`Scratched.Cost` is the card's value.** The game counts it as a gambling loss even when the card wins.
- **`ReceiptItemId` is a best guess.** It is the number of the card's receipt item. A receipt given by name may not match the item the game really uses. It is `-1` when the card has no receipt or the number is not set.
  - Technical: it is the `ID` of the card's receipt reference.
- **`Scratched.Prize` is worked out from the gambling totals.** The framework reads how much the game says the player has won before and after the card is used. Any other change made to that total during the use, which only another mod's change can cause, is counted as part of the prize.
  - Technical: the payout is read from the gambling tally `OCWon` before and after `Use`.
- **`LastPayout` is the payout of the last `Won` event since the game started.** It is `0` before the first win and is not saved. It stays up to date even when nobody listens to `Won`.
- **`IsAvailable` is `false` when a needed event could not be set up.** A missing part stops only its own events, and the reads are not affected. `LastPayout` only changes for a win whose part is set up. If only the doubling change is missing, only `Doubled` stops. See "If something fails to set up" below.
