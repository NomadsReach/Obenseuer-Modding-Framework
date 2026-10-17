---
title: Reading a trader
parent: Traders
grand_parent: People
nav_order: 2
---

# Reading a trader

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

`Traders.Get(shopId)` gives a `TraderInfo`, a snapshot of one trader at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Traders</code> <span>reads and checks traders</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Traders
{
    public static bool IsAvailable { get; }
    public static bool PriceFactorsAvailable { get; }
    public static TraderInfo Get(string shopId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Get(string shopId)` | `TraderInfo` | The trader with this shop id. `null` when not found. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when reading a trader and changing its money work. |
| `PriceFactorsAvailable` | `bool` | `true` when price factors work. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TraderInfo</code> <span>a snapshot of one trader</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class TraderInfo
{
    public string Id { get; }
    public bool Open { get; }
    public float Money { get; }
    public MoneyCurrency Currency { get; }
    public float BuyFactor { get; }
    public float SellFactor { get; }
    public bool BuysStolen { get; }
    public string[] AllowedCategories { get; }
    public string[] ForbiddenCategories { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The trader's shop id. |
| `Open` | `bool` | `true` when the trader is open. You can only read this. |
| `Money` | `float` | How much money the trader has to pay the player with. |
| `Currency` | `MoneyCurrency` | The currency that money is in, OC or RM. |
| `BuyFactor` | `float` | All mods' buy price factors multiplied together. `1` when none. |
| `SellFactor` | `float` | All mods' sell price factors multiplied together. `1` when none. |
| `BuysStolen` | `bool` | `true` when the trader buys stolen goods. |
| `AllowedCategories` | `string[]` | The kinds of item the trader buys. Empty means no limit. |
| `ForbiddenCategories` | `string[]` | The kinds of item the trader refuses to buy. |

</div>
</div>

## Things to know

- A trader is found by its shop id, the same id that `Shops.All()` and `Shops.Stock` use. <span class="tech">(it is the `GUID` of the `Trade`; `Get` gives `null` for an empty or unknown id, and when `IsAvailable` is `false`)</span>
- `Money` is in the trader's own currency, which is OC or RM. OC is Open Sewer Coins and RM is Real Money (see the [glossary](../../glossary.md#oc)).
- You can only read `Open`. The game opens and closes a trader by the clock, and it would undo anything your mod set, so there is no way to set it. <span class="tech">(the game sets `Trade.open` from the trader's opening hours every in-game minute when `autoOpenClose` is on; the game's own buy and sell checks do not read it. See [Shop hours](../shophours.md) to read the opening times)</span>
- `BuysStolen`, `AllowedCategories` and `ForbiddenCategories` say what the trader will take from the player. `Sell` follows them.

## Limits

- `Buy` and `Sell` refuse with `Closed` when the trader is closed.
