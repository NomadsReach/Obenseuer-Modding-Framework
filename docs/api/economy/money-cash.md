---
title: Money (Cash)
parent: Economy
grand_parent: Money and shops
nav_order: 2
---

# Money (Cash)

This is the full list of what the API gives you, one section for each part of the economy. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

`Cash` is the player's wallet. You can read the balance, check whether the player can afford something, give money, spend money, and get told when either balance changes.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MoneyCurrency</code> <span>the two currencies</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum MoneyCurrency
{
    OC = 0,
    RM = 1
}
```

| Value | Meaning |
|---|---|
| `OC` | Open Sewer Coins, the everyday coins. |
| `RM` | Real Money. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>MoneyChangedArgs</code> <span>what you get when a balance changes</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class MoneyChangedArgs
{
    public MoneyCurrency Currency { get; }
    public float Before { get; }
    public float After { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Currency` | `MoneyCurrency` | Which balance changed. |
| `Before` | `float` | The balance before the change. |
| `After` | `float` | The balance after the change. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Cash</code> <span>the wallet</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Cash
{
    public static event Action<MoneyChangedArgs> MoneyChanged;
    public static bool IsAvailable { get; }
    public static float Balance(MoneyCurrency currency);
    public static bool Has(MoneyCurrency currency, float amount);
    public static bool Give(MoneyCurrency currency, float amount);
    public static bool TrySpend(MoneyCurrency currency, float amount);
    public static int RmToOc(float rm);
    public static float OcToRm(int oc);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Balance(MoneyCurrency currency)` | `float` | The balance in that currency. `0` while money is not loaded. |
| `Has(MoneyCurrency currency, float amount)` | `bool` | `true` when the wallet holds at least that much. |
| `Give(MoneyCurrency currency, float amount)` | `bool` | Adds money to the wallet. `true` when it was added. |
| `TrySpend(MoneyCurrency currency, float amount)` | `bool` | Takes money only if the wallet covers it. `true` when it was taken. |
| `RmToOc(float rm)` | `int` | Converts an RM amount to OC at the game's rate, dropping any fraction. |
| `OcToRm(int oc)` | `float` | Converts an OC amount to RM at the game's rate, rounded to 0.1. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the money change could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `MoneyChanged` | `MoneyChangedArgs` | A balance has changed. |

</div>
</div>

## Things to know

- `MoneyChanged` fires once for each currency whose balance changed, whatever caused the change. That covers shops, banking, money in the mail, dialogue, and your own `Give` and `TrySpend`. It does not fire when a save loads, because loading sets the balance directly. <span class="tech">(it is raised for every change made through the game's `Money.AddMoney(float, Currency)` or `Money.RemoveMoney(float, Currency)`.)</span>
- `MoneyChanged` handlers run right away, except during a trade with a trader. In a trader trade, the event is held until the trade is finished, and a trade that is cancelled raises none. See [Traders](../traders.md). <span class="tech">(the handlers run synchronously inside `Money.AddMoney` or `RemoveMoney`.)</span>
- `Give` and `TrySpend` round the amount the way the game does. OC amounts lose any fraction, and RM amounts are rounded to 0.1, because the game counts RM in tenths.
- `Give` makes money out of nothing, so use it for rewards. It is not a transfer from anywhere.
- `Has` is `true` when the wallet holds at least `amount`. It is `false` for an amount of 0 or less, or above 16,000,000.
- `RmToOc` and `OcToRm` convert one currency into the other at the game's own rate. They give 0 while the game's money is not loaded. <span class="tech">(they call `Money.RmToOcConvert` (which cuts off the fraction) and `Money.OcToRmConvert` (which rounds to 0.1).)</span>
- `Cash` replaces the older `Wallet` and covers both currencies. It uses the same 16,000,000 limit for one call as `Wallet`, and is a little stricter. `Wallet` stays for code that only needs OC. <span class="tech">(`Wallet` accepts `Has(0)` and negative amounts in `Has`; `Cash.Has` refuses them.)</span>

## Limits

- `MoneyChanged` handlers that call `Give` or `TrySpend` raise `MoneyChanged` again, and the framework stops it after 8 levels (see [Handlers that call back into the framework](details.md#handlers-that-call-back-into-the-framework)).
- An amount that rounds to 0 is refused. A single call can move at most 16,000,000.
- `Give` refuses an amount that would push the balance past what the game can store. That is about 2.1 billion OC or about 214 million RM. Past that, the game's number would wrap around and turn negative. <span class="tech">(the limits are `int.MaxValue` for OC and 214748364 for RM.)</span>
- `Give` and `TrySpend` return `false` while a save is loading or being written, and when the game is not ready to move money. The main menu is one such time. <span class="tech">("not ready" means there is no `Money`, `InteractObjects` or `Notifications` instance.)</span>
- `Balance` for OC is a decimal number, and is only exact up to 16,777,216. For big OC balances, use `Cash.Oc` instead (see [Cash.Oc and Cash.Rm](cash-oc-and-cash-rm.md)).
