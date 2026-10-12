# Casino API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.casino`.
Files: `src/Api/Casino.cs`, `src/Api/CasinoPatches.cs`. Machine ids use `SceneIds.Of` from `src/Api/SceneIds.cs`.

The facade covers the placed machines: the slot machine and the coin pusher (Pajatso). Gambling stats (`PlayerStatsGambling`, the gambling need and addiction levels) are not wrapped here. `Survival` covers them.

## Wiring

```csharp
internal static void Casino.Init();
```

Core calls this from `Core.Awake`.

`Casino.Init` installs every hook under one owner. If any hook or any private field it needs is missing (or has a different type), the owner is removed, `CorePatcher.IsDisabled("obenseuer.moddingframework.casino")` becomes true, and one warning is logged. `Machines` and `LastPayout` keep working. Only `Played`, `Won` and `CashedOut` stop.

## Threading and safety

- Main thread only.
- `Casino.IsAvailable` is false when the hooks were not installed or the owner was disabled. Reads are not affected.
- Every member is safe at any time. On the main menu or with no save loaded, `Machines` returns an empty list and `LastPayout` returns `0`.
- Nothing throws to the caller. Each member catches its own exceptions and logs the first failure per member.
- Events are raised synchronously from the game's own call, after the game method has run far enough to know the outcome. `Events.Raise` isolates each handler, so one that throws is logged and the others still run.
- The hooks build nothing and allocate nothing when an event has no subscriber (the coin pusher hooks can fire many times a second). `LastPayout` is still kept up to date.
- The facade has no write members. Mods cannot set a stake, a payout or a machine's money. Writes stay in the game (the player's own input).

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

public static class Casino
{
    public static bool IsAvailable { get; }
    public static List<CasinoMachineInfo> Machines();    // every slot machine and coin pusher in the loaded scene
    public static int LastPayout { get; }                 // payout of the last Won event this session; 0 before the first win; not saved
    public static event Action<CasinoPlayedArgs> Played;
    public static event Action<CasinoWonArgs> Won;
    public static event Action<CasinoCashedOutArgs> CashedOut;
}
```

Amounts are in OC. Both machines take and pay OC (`SlotMachineGameplay` cashes out with `Money.AddMoney(..., RM: false)`, `PajatsoGameplay.OnCollectWins` does the same).

## Events

- `Played(CasinoPlayedArgs)`: a paid round starts.
  - Slot machine: the stake was taken from the machine's pot for a round (`SlotMachineGameplay.ChargeMoney` returned true). `Stake` is that stake.
  - Coin pusher: one coin was launched. `Stake` is `1`.
  - A refused start (not enough money in the machine, or the machine is busy) does not fire.
- `Won(CasinoWonArgs)`: a win was credited inside the machine. It is not a transfer to the player.
  - Slot machine: the round's win, before any doubling. `Payout` is the round's reward (`SlotMachineGameplay.HandleWinning`). The player may double it, lose it, or keep it, and the money reaches the wallet only at the cash-out. Doubling results are not reported as `Won`.
  - Coin pusher: a gate hit paid `Payout` coins into the win pile (`PajatsoGameplay.CoinGateHit`). The pile reaches the wallet when the player collects it. A gate hit with an empty gutter does not fire.
- `CashedOut(CasinoCashedOutArgs)`: real money reached the wallet. Use this event for what the player actually received.
  - Slot machine: the player pressed cash out (`SlotMachineGameplay.HandleQuittingProcedure`). `Amount` is the whole machine balance, which includes inserted money and any doubled winnings.
  - Coin pusher: the player collected the win pile (`PajatsoGameplay.OnCollectWins`). `Amount` is the pile. An empty pile does not fire.
- All events fire on the main thread from inside the game call.

## Machines

- `Machines` lists both kinds.
- `Id` is the machine's save GUID when one of its components has one. The slot machine and the coin pusher have none in build 25839246, so the id is the scene id: the scene name, a colon, then each node from the root down with its sibling index (for example `Bar:Room[2]/SlotMachine[0]`). It is stable between calls and between sessions for a machine that stays in the scene hierarchy, and it is the same value the events report.
- `Stake` for a slot machine is read at call time. It changes when the player raises the stake, so an old `CasinoMachineInfo` is a snapshot.

## Not in this area

- Scratchcard: its payout is a local in `Scratchcard.Use`. Nothing else carries it, so there is no clean event point. Add it when a game member exposes the payout.
- Gambling tallies and addiction stats: `PlayerStatsGambling` and the gambling need. `Survival` covers them.
- Rigging or setting odds: no write API, by design.

## Unverified in game

- Slot `Won` fires at `HandleWinning`. The in-game check is one win on a slot machine: `Won` fires once with the reward shown on the machine.
- Slot `CashedOut`: insert money, win once, cash out. `Amount` should equal the wallet change.
- Coin pusher `Won` uses a before and after count on the private `coinsWon` field. The in-game check is one gate hit with coins in the gutter, then collecting the pile for `CashedOut`.
- Private fields are read through Harmony injection (`___coinsWon` and others) in the patches and a cached `FieldInfo` for `Machines`. The access check passes for this area, and all six patches were applied against the real `Assembly-CSharp.dll` in a standalone check outside the game.
