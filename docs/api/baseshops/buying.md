---
title: Buying
parent: Base shops
grand_parent: Money and shops
nav_order: 3
---

# Buying

`Buy(id, furnitureId, count)` buys `count` pieces of one kind of furniture from a furniture shop for the player, the way the shop screen does.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BaseShopBuyResult</code> <span>what happened when you tried to buy</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum BaseShopBuyResult
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
| `Ok` | The purchase is complete. |
| `Busy` | Not now. A save is loading or being written, money handlers are nested too deep, or the shop screen is open. |
| `NotFound` | No furniture shop has that id, or the shop has no such furniture. |
| `Unsupported` | The shop is a train shop. |
| `Refused` | The request is not allowed. `count` is below 1, `furnitureId` is empty, a piece that needs a custom picture has none, or the total is above 16 million in either currency. |
| `OutOfStock` | The shop holds fewer pieces than you asked for. |
| `NoMoney` | The player cannot pay. Nothing was taken. |
| `Unavailable` | The player's wallet or home building is not ready. |
| `Failed` | A step went wrong. A charge was refused, the delivery failed, or an error was caught before delivery. Any money taken was given back. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BaseShopBoughtArgs</code> <span>what Bought tells you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class BaseShopBoughtArgs
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
| `FurnitureId` | `string` | The id of the furniture that was bought. |
| `Count` | `int` | How many pieces were bought. |
| `Oc` | `int` | The total OC paid. |
| `Rm` | `int` | The total RM paid. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>BaseShops</code> <span>buying furniture from a shop</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class BaseShops
{
    public static BaseShopBuyResult Buy(string id, string furnitureId, int count);
    public static event Action<BaseShopBoughtArgs> Bought;
}
```

| Method | Returns | What it does |
|---|---|---|
| `Buy(string id, string furnitureId, int count)` | `BaseShopBuyResult` | Buys `count` pieces of one kind of furniture from a shop for the player. |

| Event | Gives you | When it fires |
|---|---|---|
| `Bought` | `BaseShopBoughtArgs` | The player bought furniture through `Buy`. It fires once, after the stock and the shop's money have changed. |

</div>
</div>

## Things to know

- You find the furniture by its furniture id, and the shop must hold at least `count` pieces of it. There is no call that lists a shop's stock yet, so for a piece your mod added, use the id the furniture registry gave you. <span class="tech">(The stock entry is the first entry of `GetAvailableItems()` whose `furniture.Id` is `furnitureId`, and its `amount` must be at least `count`.)</span>
- The price is the shop's buying price for each piece, times `count`. There is no haggling, discount or owner price. <span class="tech">(It uses the game's `GetOcBuyPrice` and `GetRmBuyPrice`, which use `buyPercent`.)</span>
- The money comes from the player's wallet only, and the bank is not used. OC is taken first, then RM. <span class="tech">(It uses `Money.RemoveMoney` through `Cash.TrySpend`.)</span>
- The framework checks everything before it takes any money: the save must not be busy, the id and count must be valid, the shop must have the piece and enough of it, a piece that needs a custom picture must have one, and the wallet must hold both prices.
- Then it charges OC and RM, puts the piece in the player's building stock, and only after that takes the piece out of the shop's stock and adds the money to the shop. <span class="tech">(A custom-image piece is one with `canPlaceCustomImage`, which the game checks in `ShopBaseUI.InfoButtonPressed`. The delivery is `BuildingSystem.AddFurniture`. The shop's stock goes through `RemoveItem` and its money through the game's `AddBuyMoney`.)</span>
- If the RM charge fails, the OC charge is refunded. If the delivery fails, both charges are refunded and the shop's stock and money are left as they were. <span class="tech">(A refund adds the money back without the wallet-limit check and without the save and nesting refusals, so those can't block it. If the game's money add itself throws, the refund is logged as an error and the money is not returned. OC is a whole number in the wallet, so its refund is exact. RM is a decimal number, so a charge and its refund can differ by one step of the number type.)</span>
- If the shop can't remove its stock after the delivery, the piece is taken back out of the player's building stock, both charges are refunded, and the result is `Failed`.
- If the piece can't be taken back, the money is not refunded, an error goes to the log, and the result is `Failed`. This means the player never keeps both the piece and the money.
- `Bought` only fires for purchases made with `Buy`. A purchase made on the game's own shop screen does not raise it.
- An `Ok` result stands even when the shop's till fails afterwards. That problem is written to the log, and the purchase is not undone.
- `Busy` also covers the shop screen being open. The screen has to be closed, because its selected entry would otherwise point at stock that was just removed. <span class="tech">(`Busy` comes from `Cash.Refused`, which is true while a save is loading or being written, while the player's inventory is in the middle of a change (`PlayerInventory.InTransaction`), and when `MoneyChanged` or `Received` handlers are nested too deep. The 16 million limit is `Cash.MaxAmount`. The shop screen is `ShopController.menuIsOpen`.)</span>
- `Unsupported` covers train shops. Buying a train places it on the track from the shop screen's preview, and selling one removes it from the track. The framework does neither.
- A shop has no opening hours, so `Buy` never refuses because a shop is closed. Opening hours belong to trading with characters: see [Shop hours](../shophours.md).
- The player can always hold more furniture. The game puts no limit on the building stock, so `Buy` has no "full" result. <span class="tech">(`AddFurniture` returns `true` every time.)</span>
- A purchase does not count as a crime and does not mark anything as stolen. The shop's money and stock are saved at the next game save. <span class="tech">(The game's buy sets no crime or stolen flag. The shop's data is written by `BaseShop.OnSavingGame`.)</span>

## Limits

- `Buy` takes the first matching entry only. A piece with custom pictures can have several entries, and one `Buy` can't take pieces from two of them. It returns `OutOfStock` instead.
- `Buy` does not refresh a shop screen that is open.
- A purchase from a furniture display in the world goes a different way and is not covered. <span class="tech">(The display purchase is `InteractableFurnitureDisplay.Purchase`.)</span>
