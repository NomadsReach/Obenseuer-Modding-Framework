---
title: Cash.Oc and Cash.Rm
parent: Economy
grand_parent: Money and shops
nav_order: 3
---

# Cash.Oc and Cash.Rm

These two properties read the balance directly, as a whole number for OC and as a decimal number for RM.

```csharp
public static class Cash
{
    public static int Oc { get; }      // the OC balance as a whole number; 0 while money is not loaded
    public static float Rm { get; }    // the RM balance; the same as Cash.Balance(MoneyCurrency.RM)
}
```

- **You can only read both of them, and they work at any time.** They are not blocked while a save is loading, the same as `Balance`.
- **Use `Oc` for big OC balances.** `Oc` reads the game's whole number directly. `Balance(MoneyCurrency.OC)` gives the same number as a decimal, which is only exact up to 16,777,216.
- **`Rm` gives the RM balance as the game stores it, with no rounding.** It is the same as `Balance(MoneyCurrency.RM)`.
- **`Cash.Oc` has the same value as `Wallet.Balance`.**

Before:

```csharp
if (Cash.Balance(MoneyCurrency.OC) >= 50f) { }
```

After:

```csharp
if (Cash.Oc >= 50) { }
```
