---
title: Events
parent: Casino
grand_parent: Money and shops
nav_order: 3
---

# Events

Amounts are in OC. All events fire from your mod's normal code, inside the game's own call. If one of your handlers throws an error, the framework logs it and still runs the others.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Casino</code> <span>the casino events and their totals</span></p>

Subscribe with `+=` to `Played`, `Won`, `CashedOut`, `Doubled` or `Scratched` to hear about casino play. `LastPayout` holds the last win of this session.

<div class="api-pair" markdown="1">

```csharp
public static class Casino
{
    public static event Action<CasinoPlayedArgs> Played;
    public static event Action<CasinoWonArgs> Won;
    public static event Action<CasinoCashedOutArgs> CashedOut;
    public static event Action<CasinoDoubledArgs> Doubled;
    public static event Action<ScratchcardArgs> Scratched;

    public static int LastPayout { get; }
    public static bool IsAvailable { get; }
}
```

| Event | Gives you | When it fires |
|---|---|---|
| `Played` | `CasinoPlayedArgs` | A paid round has started. |
| `Won` | `CasinoWonArgs` | A win was credited inside a machine. |
| `CashedOut` | `CasinoCashedOutArgs` | Winnings reached the player's wallet. |
| `Doubled` | `CasinoDoubledArgs` | The player's double on a slot win was settled. |
| `Scratched` | `ScratchcardArgs` | A scratchcard was used. |

| Property | Type | What it means |
|---|---|---|
| `LastPayout` | `int` | The payout of the last `Won` event this session. `0` before the first win. Not saved. |
| `IsAvailable` | `bool` | `false` when a needed event could not be set up. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CasinoPlayedArgs</code> <span>what Played tells you</span></p>

`Played` hands you this when a paid round starts. `Machine` says which machine, and `Stake` is the stake for the round in OC.

<div class="api-pair" markdown="1">

```csharp
public sealed class CasinoPlayedArgs
{
    public CasinoMachineInfo Machine { get; }
    public int Stake { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Machine` | `CasinoMachineInfo` | The machine. |
| `Stake` | `int` | The stake of this round, in OC. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CasinoWonArgs</code> <span>what Won tells you</span></p>

`Won` hands you this when a win is credited inside a machine. That is not money in the wallet yet, and `CashedOut` reports when it is. `Payout` is the win in OC.

<div class="api-pair" markdown="1">

```csharp
public sealed class CasinoWonArgs
{
    public CasinoMachineInfo Machine { get; }
    public int Payout { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Machine` | `CasinoMachineInfo` | The machine. |
| `Payout` | `int` | The win, in OC. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CasinoCashedOutArgs</code> <span>what CashedOut tells you</span></p>

`CashedOut` hands you this when winnings reach the player's wallet. `Amount` is the OC that went into the wallet.

<div class="api-pair" markdown="1">

```csharp
public sealed class CasinoCashedOutArgs
{
    public CasinoMachineInfo Machine { get; }
    public int Amount { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Machine` | `CasinoMachineInfo` | The machine. |
| `Amount` | `int` | The OC that went into the wallet. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CasinoDoubledArgs</code> <span>what Doubled tells you</span></p>

`Doubled` hands you this when the player's double on a slot win is settled. `Before` and `After` are the winnings either side of the double.

<div class="api-pair" markdown="1">

```csharp
public sealed class CasinoDoubledArgs
{
    public CasinoMachineInfo Machine { get; }
    public int Before { get; }
    public int After { get; }
    public bool Won { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Machine` | `CasinoMachineInfo` | The slot machine. |
| `Before` | `int` | The winnings of the round before the double. |
| `After` | `int` | The winnings of the round after the double. |
| `Won` | `bool` | `true` when `After` is more than `Before`. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ScratchcardArgs</code> <span>what Scratched tells you</span></p>

`Scratched` hands you this each time a scratchcard is used, win or not. `Prize` is what the game paid, and `Won` is `true` when it is above 0.

<div class="api-pair" markdown="1">

```csharp
public sealed class ScratchcardArgs
{
    public int CardItemId { get; }
    public string CardTitle { get; }
    public bool Won { get; }
    public int Prize { get; }
    public bool PrizeInRm { get; }
    public int PrizeOc { get; }
    public int Cost { get; }
    public int ReceiptItemId { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `CardItemId` | `int` | The item number of the card. |
| `CardTitle` | `string` | The card's name. |
| `Won` | `bool` | `true` when the card paid a prize. |
| `Prize` | `int` | The prize the game paid. It is in RM when `PrizeInRm` is `true`, and in OC otherwise. |
| `PrizeInRm` | `bool` | `true` when the prize is in RM (Real Money). |
| `PrizeOc` | `int` | The prize worth in OC. It is the same as `Prize` when `PrizeInRm` is `false`. |
| `Cost` | `int` | The card's value, in OC. |
| `ReceiptItemId` | `int` | The item number of the card's receipt. `-1` when the card has none or it is not known. |

</div>
</div>

## Things to know

- `Played` fires when a paid round starts. For a slot machine, `Stake` is the stake taken from the machine's pot for the round. For the coin pusher, one coin was launched and `Stake` is `1`. <span class="tech">(For the slot machine, `SlotMachineGameplay.ChargeMoney` returned `true`.)</span>
- If the round is refused, because the machine has too little money or it is busy, nothing fires.
- `Won` fires when a win is credited inside the machine. It is not money going to the player. The money reaches the wallet only at `CashedOut`. <span class="tech">(For the slot machine, the round's reward is handled in `SlotMachineGameplay.HandleWinning`. For the coin pusher, it is `PajatsoGameplay.CoinGateHit`.)</span>
- For a slot machine, `Payout` is the round's win before any doubling. The player may double it, lose it or keep it. Doubling results are reported by `Doubled`, not as another `Won`.
- For the coin pusher, a gate hit paid `Payout` coins into the win pile. The pile reaches the wallet when the player collects it. A gate hit when the gutter has no coins does not fire.
- `CashedOut` fires when the winnings reach the wallet. Use it for what the player really received.
- For a slot machine, the player pressed cash out. `Amount` is the whole machine balance, including the money the player put in and any doubled winnings. <span class="tech">(It is raised around `SlotMachineGameplay.HandleQuittingProcedure`.)</span>
- For the coin pusher, the player collected the win pile. `Amount` is the pile. An empty pile does not fire. <span class="tech">(It is `PajatsoGameplay.OnCollectWins`.)</span>
- `Doubled` fires when the player's double on a slot round is settled. The round's winnings are still in the machine, so the wallet does not change here. <span class="tech">(It is raised from `SlotMachineGameplay.HandleDoublingResult`. The game logs "Trying to double without any winnings" when `Before` is `0`.)</span>
- If the coin lands on its edge, `After` equals `Before`. The winnings are kept and `Won` is `false`. A wrong guess gives `After` of `0`.
- A double with nothing to double does not fire.
- `Scratched` fires every time a scratchcard is used, win or not. It fires while the card is still in the inventory. <span class="tech">(It is raised around `Scratchcard.Use`. A call to `Scratchcard.Use` from another mod also fires it.)</span>
- `Scratched.Prize` is what the game paid. A card that pays RM sets `PrizeInRm` to `true`, and `PrizeOc` converts it to OC. `Won` is `true` when the prize is above 0. <span class="tech">(`PrizeOc` uses `Money.RmToOcConvert`.)</span>
- `Won` is not the same as the receipt's "can be cashed out" flag, which can be on for a zero prize. <span class="tech">(A receipt can have `canBeCashedOut` true with a zero prize when the card's `val1` is 0.)</span>
- `Scratched.Cost` is the card's value. The game counts it as a gambling loss even when the card wins.
- `ReceiptItemId` is a best guess. It is the number of the card's receipt item. A receipt given by name may not match the item the game really uses. <span class="tech">(It is the `ID` of the card's receipt reference.)</span>
- `Scratched.Prize` is worked out from the gambling totals. The framework reads how much the game says the player has won before and after the card is used. Any other change made to that total during the use is counted as part of the prize. <span class="tech">(The payout is read from the gambling tally `OCWon` before and after `Use`. Only another mod's change can make that total move during the use.)</span>
- `LastPayout` is the payout of the last `Won` event since the game started. It is `0` before the first win and is not saved. It stays up to date even when nobody listens to `Won`.
- `IsAvailable` is `false` when a needed event could not be set up. A missing part stops only its own events, and the reads are not affected.
- `LastPayout` only changes for a win whose part is set up. If only the doubling change is missing, only `Doubled` stops. See "If something fails to set up" below.
