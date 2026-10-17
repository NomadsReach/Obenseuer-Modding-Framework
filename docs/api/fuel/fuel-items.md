---
title: Fuel items
parent: Fuel
grand_parent: Money and shops
nav_order: 4
---

# Fuel items

`FuelTypes` lists the items that the game's machines burn as fuel and says how each one burns. Each one is a `FuelTypeInfo`.

```csharp
public sealed class FuelTypeInfo    // how one item behaves as fuel
{
    public int ItemId { get; }               // the item's number in the game
    public int BurnSeconds { get; }          // how many seconds one unit burns, before a machine's own speed factor
    public int BurntItemId { get; }          // the item left behind when it burns; -1 when it leaves nothing
    public float BurntItemAmount { get; }    // how many of that leftover item one unit leaves
}

public static class FuelTypes
{
    public static List<FuelTypeInfo> All();        // every item that has fuel data
    public static FuelTypeInfo Get(int itemId);    // one item's fuel data; null when it has none
}
```

- **`All()` lists every item the game gives fuel data to.** A generator burns such an item for `BurnSeconds` for each unit and leaves `BurntItemAmount` of `BurntItemId` for each unit.
  - Technical: it lists every item in the game's item database that has `ItemFueldata` metadata. `BurnSeconds` is `ItemFueldata.burntime`, `BurntItemId` is `ItemFueldata.burntItem`, and `BurntItemAmount` is the refuse items per unit burned.
- **A machine may accept some items that are not in this list.** Those burn for 60 seconds per unit. Which items a machine accepts is on the [Power devices](../power-devices.md) page, in `FuelItemIds`.
  - Technical: an item with no fuel data that a machine accepts through its `FuelCategories` burns 60 seconds per unit.
- **Liquids are not in this list.** A container of liquid burns as long as the liquid's burn time multiplied by the amount of liquid. If the liquid has no fuel data, it burns 60 seconds per unit.
  - Technical: `burntime` times the liquid amount, or 60 per unit when the liquid has no fuel data.
- **Battery fuel and minitrain fuel are not in this list.** The game keeps them in other places, so this API can't report them yet.
  - Technical: battery fuel uses ammo and liquid metadata, not `ItemFueldata`. Minitrain fuel is a private `acceptedFuels` list.
- **`BurnSeconds` does not include a machine's own speed factor.** Each machine can burn faster or slower than the number here. The factor is `PowerDeviceInfo.BurnTimeFactor` on the [Power devices](../power-devices.md) page. When a machine runs out of fuel, `PowerDevices.FuelRanOut` tells you.
- **`Get(itemId)` returns `null` for an item with no fuel data.** That includes an item number the game does not have.
