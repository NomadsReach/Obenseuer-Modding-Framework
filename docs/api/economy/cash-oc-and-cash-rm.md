---
title: Cash.Oc and Cash.Rm
parent: Economy
grand_parent: Money and shops
nav_order: 3
---

# Cash.Oc and Cash.Rm

These two properties read the balance directly, as a whole number for OC and as a decimal number for RM.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Cash</code> <span>the balance, read directly</span></p>

Read `Cash.Oc` or `Cash.Rm` whenever you need the balance. They are plain reads, not events, so ask again after a change.

<div class="api-pair" markdown="1">

```csharp
public static class Cash
{
    public static int Oc { get; }
    public static float Rm { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Oc` | `int` | The OC balance as a whole number. `0` while money is not loaded. |
| `Rm` | `float` | The RM balance. It is the same as `Cash.Balance(MoneyCurrency.RM)`. |

</div>
</div>

### Before and after

Before, as a complete mod. It reads the balance as a decimal number. The limits are on the [Money and cash](money-cash.md#limits) page.

```csharp
using ObenseuerCore;

[ModInfo("example.balancecheck", "Balance Check", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class BalanceCheck : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            if (Cash.Balance(MoneyCurrency.OC) >= 50f)
                Log.Info("The player can pay 50 OC.");
            else
                Log.Warn("The player cannot pay 50 OC.");
        });
    }
}
```

After, as a complete mod. It reads the same balance as a whole number.

```csharp
using ObenseuerCore;

[ModInfo("example.ocbalancecheck", "OC Balance Check", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class OcBalanceCheck : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            if (Cash.Oc >= 50)
                Log.Info("The player has " + Cash.Oc + " OC.");
            else
                Log.Warn("The player has less than 50 OC. See the limits on the money and cash page.");
        });
    }
}
```

### Things to know

- You can only read both of them, and they work at any time. They are not blocked while a save is loading, the same as `Balance`.
- Use `Oc` for big OC balances. `Oc` reads the game's whole number directly. `Balance(MoneyCurrency.OC)` gives the same number as a decimal, which is only exact up to 16,777,216.
- `Rm` gives the RM balance as the game stores it, with no rounding.
- `Cash.Oc` has the same value as `Wallet.Balance`.
