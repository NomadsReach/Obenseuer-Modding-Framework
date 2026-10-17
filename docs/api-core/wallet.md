---
title: Wallet
parent: Framework core classes
grand_parent: Framework basics
nav_order: 7
---

# Wallet

`Wallet` reads and spends the player's OC (Open Sewer Coins, the game's everyday money, see [OC](../glossary.md#oc)). It is the older API, kept so old mods keep working. For new code use `Cash`, which handles OC and RM (Real Money, see [RM](../glossary.md#rm)). It is described on the [economy](../api/economy/money-cash.md#money-cash) page.

```csharp
public static class Wallet
{
    public static int Balance { get; }           // how much OC the player has
    public static bool Has(int amount);          // true when the player has at least this much OC
    public static bool TrySpend(int amount);     // take the OC away; false when it could not be done (see below)
    public static bool Give(int amount);         // give the OC to the player; false when it could not be done (see below)
}
```

- **`TrySpend` and `Give` never throw an error.** If something is wrong, you get `false`.
- **`TrySpend` and `Give` refuse an amount that is not a whole number from 1 to 16,000,000.** You also get `false` when the player does not have enough OC to spend, or when the game's money is not ready.
  - Technical: the cap of 16,000,000 stays below 2^24 (16,777,216), the largest whole number a float holds exactly. The game passes amounts as float, so larger values could be rounded.
- **`TrySpend` and `Give` return `false` while the game is loading or saving, the same as `Cash.Give` and `Cash.TrySpend`.** Try again later.
- **On the main menu the balance is `0`, and `Has`, `TrySpend` and `Give` return `false`.** There is no player money yet.
  - Technical: this is when `Money.instance` is null.
- **Rarely, a call can return `false` even though the balance did change.** If the game fails right after it changes the money, the call returns `false`, but the new balance stays.
  - Technical: the failure is in the game's `UpdateInteractableMoney` step, which runs after the balance is set.

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

public class Toll : BaseUnityPlugin
{
    private void Start()
    {
        if (Wallet.TrySpend(50))
        {
            Logger.LogInfo("paid 50 OC, balance " + Wallet.Balance);
        }
    }
}
```
