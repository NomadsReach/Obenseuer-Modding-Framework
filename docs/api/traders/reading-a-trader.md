---
title: Reading a trader
parent: Traders
grand_parent: People
nav_order: 2
---

# Reading a trader

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`Traders.Get(shopId)` gives a `TraderInfo`, a snapshot of one trader at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

```csharp
public static class Traders
{
    public static bool IsAvailable { get; }               // true when reading a trader and changing its money work
    public static bool PriceFactorsAvailable { get; }     // true when price factors work
    public static TraderInfo Get(string shopId);          // the trader with this shop id; null when not found
}

public sealed class TraderInfo    // a snapshot of one trader
{
    public string Id { get; }                         // the trader's shop id
    public bool Open { get; }                         // true when the trader is open; you can only read this
    public float Money { get; }                       // how much money the trader has to pay the player with
    public MoneyCurrency Currency { get; }            // the currency that money is in (OC or RM)
    public float BuyFactor { get; }                   // all mods' buy price factors multiplied together; 1 when none
    public float SellFactor { get; }                  // all mods' sell price factors multiplied together; 1 when none
    public bool BuysStolen { get; }                   // true when the trader buys stolen goods
    public string[] AllowedCategories { get; }        // the kinds of item the trader buys; empty means no limit
    public string[] ForbiddenCategories { get; }      // the kinds of item the trader refuses to buy
}
```

- **A trader is found by its shop id.** It is the same id that `Shops.All()` and `Shops.Stock` use.
  - Technical: it is the `GUID` of the `Trade`. `Get` gives `null` for an empty or unknown id, and when `IsAvailable` is `false`.
- **`Money` is in the trader's own currency, which is OC or RM.** OC is Open Sewer Coins and RM is Real Money (see the [glossary](../../glossary.md#oc)).
- **You can only read `Open`.** The game opens and closes a trader by the clock, and it would undo anything your mod set, so there is no way to set it. `Buy` and `Sell` refuse with `Closed` when the trader is closed.
  - Technical: the game sets `Trade.open` from the trader's opening hours every in-game minute when `autoOpenClose` is on. The game's own buy and sell checks do not read it. See [Shop hours](../shophours.md) to read the opening times.
- **`BuysStolen`, `AllowedCategories` and `ForbiddenCategories` say what the trader will take from the player.** `Sell` follows them.
