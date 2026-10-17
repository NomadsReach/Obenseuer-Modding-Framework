---
title: Fuel items
parent: Fuel
grand_parent: Money and shops
nav_order: 4
---

# Fuel items

`FuelTypes` lists the items that the game's machines burn as fuel and says how each one burns. Each one is a `FuelTypeInfo`.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FuelTypeInfo</code> <span>how one item behaves as fuel</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class FuelTypeInfo
{
    public int ItemId { get; }
    public int BurnSeconds { get; }
    public int BurntItemId { get; }
    public float BurntItemAmount { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ItemId` | `int` | The item's number in the game. |
| `BurnSeconds` | `int` | How many seconds one unit burns, before a machine's own speed factor. |
| `BurntItemId` | `int` | The item left behind when it burns. `-1` when it leaves nothing. |
| `BurntItemAmount` | `float` | How many of that leftover item one unit leaves. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>FuelTypes</code> <span>the fuel data of each item</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class FuelTypes
{
    public static List<FuelTypeInfo> All();
    public static FuelTypeInfo Get(int itemId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<FuelTypeInfo>` | Every item that has fuel data. |
| `Get(int itemId)` | `FuelTypeInfo` | One item's fuel data. `null` when it has none. |

</div>
</div>

## Things to know

- `All()` lists every item the game gives fuel data to. A generator burns such an item for `BurnSeconds` for each unit and leaves `BurntItemAmount` of `BurntItemId` for each unit. <span class="tech">(It lists every item in the game's item database that has `ItemFueldata` metadata. `BurnSeconds` is `ItemFueldata.burntime`, `BurntItemId` is `ItemFueldata.burntItem`, and `BurntItemAmount` is the refuse items per unit burned.)</span>
- A machine may accept some items that are not in this list. Those burn for 60 seconds per unit. Which items a machine accepts is on the [Power devices](../power-devices.md) page, in `FuelItemIds`. <span class="tech">(An item with no fuel data that a machine accepts through its `FuelCategories` burns 60 seconds per unit.)</span>
- `Get(itemId)` returns `null` for an item with no fuel data. That includes an item number the game does not have.

## Limits

- `BurnSeconds` does not include a machine's own speed factor. Each machine can burn faster or slower than the number here. The factor is `PowerDeviceInfo.BurnTimeFactor` on the [Power devices](../power-devices.md) page. When a machine runs out of fuel, `PowerDevices.FuelRanOut` tells you.
- Liquids are not in this list. A container of liquid burns as long as the liquid's burn time multiplied by the amount of liquid. If the liquid has no fuel data, it burns 60 seconds per unit. <span class="tech">(`burntime` times the liquid amount, or 60 per unit when the liquid has no fuel data.)</span>
- Battery fuel and minitrain fuel are not in this list. The game keeps them in other places, so this API can't report them yet. <span class="tech">(Battery fuel uses ammo and liquid metadata, not `ItemFueldata`. Minitrain fuel is a private `acceptedFuels` list.)</span>
