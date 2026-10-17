---
title: Money (Cash)
parent: Economy
grand_parent: Money and shops
nav_order: 2
---

# Money (Cash)

This is the full list of what the API gives you, one section for each part of the economy. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`Cash` is the player's wallet. You can read the balance, check whether the player can afford something, give money, spend money, and get told when either balance changes.

```csharp
public enum MoneyCurrency { OC = 0, RM = 1 }   // OC: Open Sewer Coins, the everyday coins. RM: Real Money

public sealed class MoneyChangedArgs    // what you get when a balance changes
{
    public MoneyCurrency Currency { get; }   // which balance changed
    public float Before { get; }             // the balance before the change
    public float After { get; }              // the balance after the change
}

public static class Cash
{
    public static event Action<MoneyChangedArgs> MoneyChanged;   // a balance has changed

    public static bool IsAvailable { get; }                          // false when the money change could not be set up
    public static float Balance(MoneyCurrency currency);             // the balance in that currency; 0 while money is not loaded
    public static bool Has(MoneyCurrency currency, float amount);    // true when the wallet holds at least that much
    public static bool Give(MoneyCurrency currency, float amount);   // adds money to the wallet; true when it was added
    public static bool TrySpend(MoneyCurrency currency, float amount);   // takes money only if the wallet covers it; true when it was taken
    public static int RmToOc(float rm);                              // converts an RM amount to OC at the game's rate, dropping any fraction
    public static float OcToRm(int oc);                              // converts an OC amount to RM at the game's rate, rounded to 0.1
}
```

- **`MoneyChanged` fires once for each currency whose balance changed, whatever caused the change.** That covers shops, banking, money in the mail, dialogue, and your own `Give` and `TrySpend`. It does not fire when a save loads, because loading sets the balance directly.
  - Technical: it is raised for every change made through the game's `Money.AddMoney(float, Currency)` or `Money.RemoveMoney(float, Currency)`.
- **`MoneyChanged` handlers run right away, except during a trade with a trader.** In a trader trade, the event is held until the trade is finished, and a trade that is cancelled raises none. See [Traders](../traders.md). If your handler calls `Give` or `TrySpend`, that raises `MoneyChanged` again, and the framework stops it after 8 levels (see [Handlers that call back into the framework](details.md#handlers-that-call-back-into-the-framework)).
  - Technical: the handlers run synchronously inside `Money.AddMoney` or `RemoveMoney`.
- **`Give` and `TrySpend` round the amount the way the game does.** OC amounts lose any fraction. RM amounts are rounded to 0.1, because the game counts RM in tenths. An amount that rounds to 0 is refused. A single call can move at most 16,000,000.
- **`Give` refuses an amount that would push the balance past what the game can store.** That is about 2.1 billion OC or about 214 million RM. Past that, the game's number would wrap around and turn negative.
  - Technical: the limits are `int.MaxValue` for OC and 214748364 for RM.
- **`Give` makes money out of nothing, so use it for rewards.** It is not a transfer from anywhere.
- **`Give` and `TrySpend` return `false` while a save is loading or being written, and when the game is not ready to move money.** The main menu is one such time.
  - Technical: "not ready" means there is no `Money`, `InteractObjects` or `Notifications` instance.
- **`Has` is `true` when the wallet holds at least `amount`.** It is `false` for an amount of 0 or less, or above 16,000,000.
- **`Balance` for OC is a decimal number, and is only exact up to 16,777,216.** For big OC balances, use `Cash.Oc` instead (see the next section).
- **`RmToOc` and `OcToRm` convert one currency into the other at the game's own rate.** They give 0 while the game's money is not loaded.
  - Technical: they call `Money.RmToOcConvert` (which cuts off the fraction) and `Money.OcToRmConvert` (which rounds to 0.1).
- **`Cash` replaces the older `Wallet` and covers both currencies.** It uses the same 16,000,000 limit for one call as `Wallet`, and is a little stricter. `Wallet` stays for code that only needs OC.
  - Technical: `Wallet` accepts `Has(0)` and negative amounts in `Has`; `Cash.Has` refuses them.
