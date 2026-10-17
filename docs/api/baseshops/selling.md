---
title: Selling
parent: Base shops
grand_parent: Money and shops
nav_order: 4
---

# Selling

`Sell(id, furnitureId, count)` sells `count` pieces of one kind of furniture from the player's building stock to a furniture shop, the way the shop screen does. The game's own sale code reads the selection on the open shop screen, so the framework repeats its steps with calls that work without the screen.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BaseShopSellResult</code> <span>what happened when you tried to sell</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum BaseShopSellResult
{
    Ok,
    Busy,
    NotFound,
    Unsupported,
    Refused,
    OutOfStock,
    NoMoney,
    Unavailable,
    Failed
}
```

| Value | Meaning |
|---|---|
| `Ok` | The sale is complete. |
| `Busy` | Not now. A save is loading or being written, a money handler is nested too deep, another `Sell` is running, or the shop screen is open. |
| `NotFound` | No furniture shop has that id, or the player has no matching piece. |
| `Unsupported` | The shop is a train shop. |
| `Refused` | The request is not allowed. See the notes below. |
| `OutOfStock` | The player has fewer pieces than you asked for. |
| `NoMoney` | The shop cannot pay the total. Nothing was taken. |
| `Unavailable` | The player's wallet or home building is not ready. |
| `Failed` | A step was refused or failed. Every step already taken is undone. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BaseShopSoldArgs</code> <span>what Sold tells you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class BaseShopSoldArgs
{
    public string Id { get; }
    public string Kind { get; }
    public string FurnitureId { get; }
    public int Count { get; }
    public int Oc { get; }
    public int Rm { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The shop's id, the same as `BaseShopInfo.Id`. |
| `Kind` | `string` | The type of shop. It is always `FurnitureShop`. |
| `FurnitureId` | `string` | The id of the furniture that was sold. |
| `Count` | `int` | How many pieces were sold. |
| `Oc` | `int` | The total OC the player was paid. |
| `Rm` | `int` | The total RM the player was paid. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BaseShops</code> <span>selling furniture to a shop</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class BaseShops
{
    public static BaseShopSellResult Sell(string id, string furnitureId, int count);
    public static event Action<BaseShopSoldArgs> Sold;
}
```

| Method | Returns | What it does |
|---|---|---|
| `Sell(string id, string furnitureId, int count)` | `BaseShopSellResult` | Sells `count` pieces of one kind of furniture from the player to a shop. |

| Event | Gives you | When it fires |
|---|---|---|
| `Sold` | `BaseShopSoldArgs` | The player sold furniture through `Sell`. It fires once, after the sale is complete. |

</div>
</div>

## Things to know

- You find the piece by its furniture id, and the player must hold at least `count` pieces of it. A piece that has things inside it is skipped, because the game refuses it too ("Contains Items!"). <span class="tech">(The stock entry is the first entry of `BuildingSystem.availableFurnitures` whose `furniture.Id` is `furnitureId` and whose `gameObject` is null.)</span>
- Some pieces can't be sold. A free piece and a piece the shop does not take are refused, as the game refuses them. <span class="tech">(The checks are `Furniture.IsFree` and `FurnitureShop.CanSell`.)</span>
- The price is the shop's selling price for each piece, times `count`. A total below 0 in either currency, for example a mod piece with a negative price, is refused, and so is a total of 0 in both.
- The shop must hold the whole total in both OC and RM. The game checks only one piece, so `Sell` checks the whole total, and a sale is never paid in part. <span class="tech">(It uses `GetOcSellPrice` and `GetRmSellPrice`, which use `sellPercent`, worked out in 64-bit numbers so a large count can't wrap around. The game's check is `CheckIfShopHasMoney`.)</span>
- `Refused` covers a `count` below 1, an empty `furnitureId`, a free piece, a piece with things inside it, a piece the shop does not take, a total of 0 or less, a total above 16 million, and a wallet that could not hold the amount.
- `Sell` returns `Busy` while another `Sell` is running. That can happen when a money handler calls `Sell` while a sale is paying out.
- `Sell` also returns `Busy` while the game's shop screen is open. The screen keeps a pointer to the player's stack and could sell a stack that `Sell` already removed.
- The steps run in a fixed order. The checks take nothing. Then the shop's money goes down by the total, the pieces leave the player's stock, the shop takes the pieces, and then the wallet is paid OC and then RM.
- The wallet limit is checked for both currencies before the first step, so the wallet can't refuse after the first part is paid. The framework pays the player last, so money is only created after every stock step has worked, and a money handler sees a finished sale. <span class="tech">(The steps use `RemoveSellMoney` in the game's sell, `BuildingSystem.RemoveFurniture`, `FurnitureShop.AddItem`, and `Cash.Give`, the public add behind `Money.AddMoney`. The game pays the wallet first and removes the piece last.)</span>
- If a step fails, the steps already taken are undone in reverse order and the result is `Failed`.
- If the RM payment fails after the OC payment, the OC is taken back from the wallet. That undo can't be refused by a save or a nested handler.
- If the shop took the pieces, the same number is removed from the shop's stock.
- If the pieces were removed from the player, the player's entry gets back the amount it had before, and goes back to its place in the building stock if it left it. If the removal finished, the task-item count it lowered is added back as well.
- The shop's money goes back up by the total.
- An undo that can't be applied is logged as an error. See the limits below.
- `Sold` only fires for sales made with `Sell`. The game's own sales do not raise it. It fires after the `Busy` guard has been released, so a `Sold` handler can call `Sell` again.
- A shop has no opening hours, so `Sell` never refuses because a shop is closed.
- `Sell` takes the first matching entry without contents. A piece that the player holds in two entries (for example with different extra data) sells from the first one only, and `Sell` returns `OutOfStock` if that entry is short. `Sell` does not refresh the building menu.

## Limits

- The undo has limits. If a money handler spends the OC just paid and the RM payment then fails, the OC can't be taken back, and an error goes to the log.
- If the task-item update inside `RemoveFurniture` fails, the building stock is restored but the task-item count stays as the failure left it. <span class="tech">(The task-item update is in `BuildingSystem.RemoveFurniture`.)</span>
- If a shop's `AddItem` fails after it recorded a custom picture path, the path stays.
