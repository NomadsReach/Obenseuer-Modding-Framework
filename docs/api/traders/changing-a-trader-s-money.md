---
title: Changing a trader's money
parent: Traders
grand_parent: People
nav_order: 3
---

# Changing a trader's money

`SetMoney` and `Restock` change how much money a trader has.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Traders</code> <span>changes a trader's money</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Traders
{
    public static bool SetMoney(string modId, string shopId, float amount);
    public static bool Restock(string modId, string shopId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetMoney(string modId, string shopId, float amount)` | `bool` | Sets how much money the trader has. Returns `true` when it was set. |
| `Restock(string modId, string shopId)` | `bool` | Refills the trader's money the way the game does. Returns `true` when it was done. |

</div>
</div>

## Things to know

- `SetMoney` sets how much money the trader has, in the trader's own currency. The trader keeps the money in the save, so it is still there after a save and a load. If two mods set it, the last one wins. <span class="tech">(the money is saved with the trader)</span>
- `Restock` refills the trader's money the way the game does. It picks a random amount between the trader's minimum and maximum, and applies the difficulty setting. It does not reset the trader's own restock timer, so the game still restocks when its timer runs out. <span class="tech">(the amount follows `DifficultyInfo.GetTraderWealth`, as the game does)</span>

## Limits

- Both refuse while a trade is running, during a save or load, for an unknown shop id, for a mod id that is not valid, and when `IsAvailable` is `false`. A refusal returns `false` and changes nothing.
- `SetMoney` also refuses an `amount` that is below 0, above the largest amount of money the framework allows, or not a real number.
