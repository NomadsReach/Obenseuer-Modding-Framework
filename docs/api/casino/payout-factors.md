---
title: Payout factors
parent: Casino
grand_parent: Money and shops
nav_order: 4
---

# Payout factors

A payout factor changes how much a machine pays. You set one factor for one kind of machine. A factor of `1` is the game's normal payout, `0` pays nothing, and the highest is `4`.

```csharp
public static class Casino
{
    public static bool PayoutFactorsAvailable { get; }                                          // true when payout factors can be used
    public static bool SetPayoutFactor(string modId, CasinoMachineKind kind, float factor);    // sets your factor for a kind of machine; true when it was set
    public static bool Clear(string modId);                                                     // removes every factor your mod set
    public static List<CasinoMachineInfo> Machines();                                           // every machine in the scene you are in
}
```

- **`SetPayoutFactor` returns `false` when it did not set anything.** That is the case for an invalid mod id, a factor outside 0 to 4, an unknown kind of machine, or when `PayoutFactorsAvailable` is `false`.
- **When two mods set a factor for the same kind, the newest call wins.** Each mod keeps one factor for each kind. When a second mod sets a kind that another mod already set, one warning names both mods, and the second mod's factor applies. A mod that sets its own factor again replaces it with no warning. `Clear(modId)` removes your mod's factors for every kind, and the factor set before yours applies again.
- **For a slot machine, the factor multiplies the round's win.** The doubling, the cash-out and the gambling totals all use the changed win, and `Won` reports it as `Payout`.
  - Technical: it is applied where the game works out the reward (`SlotMachineHelper.CalculateReward`, `HandleWinning`).
- **For the coin pusher, the factor multiplies the coins of each gate hit before the coins drop.** The pile, the collect and the gambling totals all use the changed coins, and `Won` reports them.
  - Each coin still comes out of the machine's gutter, so a factor above 1 uses up more gutter coins for each hit. If the gutter runs out part way through a hit, the game says "No coins left in gutter", and the coins it already dropped are not paid. So a factor above 1 can turn a win into "no coins left".
  - A factor that rounds a gate's coins down to 0 still plays the win sound and shows "Collect 0 OC", but `Won` does not fire, because a hit that pays 0 is not reported.
  - Technical: it is applied in `PajatsoGameplay.CoinGateHit`.
- **Scratchcards are not affected.** A scratchcard's prize is a receipt that the player cashes out later, so no factor reaches it.
- **The factor works on whole coins, rounded to the nearest whole number.** The result can't go above 16000000.
  - Technical: it uses `Math.Round`.
- **Payout factors are forgotten when a scene loads.** They are not saved. Every time a new scene replaces the old one, they are cleared, so set them again from your `Events.SceneLoaded` handler.
- **A factor covers every machine of one kind, not a single machine.** The game's reward calculation does not know which machine asks, so one factor covers every slot machine in the scene, and one covers every coin pusher.
