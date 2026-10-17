---
title: Selling
parent: Base shops
grand_parent: Money and shops
nav_order: 4
---

# Selling

```csharp
public enum BaseShopSellResult    // what happened when you tried to sell
{
    Ok,            // the sale is complete
    Busy,          // not now: a save is loading or being written, money handlers are nested too deep, another Sell is running, or the shop screen is open
    NotFound,      // no furniture shop has that id, or the player has no matching piece
    Unsupported,   // the shop is a train shop
    Refused,       // the request itself is not allowed (see below)
    OutOfStock,    // the player has fewer pieces than you asked for
    NoMoney,       // the shop cannot pay the total; nothing was taken
    Unavailable,   // the player's wallet or home building is not ready
    Failed         // a step went wrong; every step already taken was undone
}

public sealed class BaseShopSoldArgs    // what Sold tells you
{
    public string Id { get; }           // the shop's id, the same as BaseShopInfo.Id
    public string Kind { get; }         // the type of shop; always FurnitureShop
    public string FurnitureId { get; }  // the id of the furniture that was sold
    public int Count { get; }           // how many pieces
    public int Oc { get; }              // the total OC the player was paid
    public int Rm { get; }              // the total RM the player was paid
}
```

`Sell(id, furnitureId, count)` sells `count` pieces of one kind of furniture from the player's building stock to a furniture shop, the way the shop screen does. The game's own sale code reads the selection on the open shop screen, so the framework repeats its steps with calls that work without the screen.

- **You find the piece by its furniture id.** The player must hold at least `count` pieces of it. A piece that has things inside it is skipped, because the game refuses it too ("Contains Items!").
  - Technical: the stock entry is the first entry of `BuildingSystem.availableFurnitures` whose `furniture.Id` is `furnitureId` and whose `gameObject` is null.
- **Some pieces can't be sold.** A free piece and a piece the shop does not take are refused, as the game refuses them.
  - Technical: the checks are `Furniture.IsFree` and `FurnitureShop.CanSell`.
- **The price is the shop's selling price for each piece, times `count`.** A total below 0 in either currency, for example a mod piece with a negative price, is refused, and so is a total of 0 in both. The shop must hold the whole total in both OC and RM. The game checks only one piece, so `Sell` checks the whole total: a sale is never paid in part.
  - Technical: it uses `GetOcSellPrice` and `GetRmSellPrice` (which use `sellPercent`), worked out in 64-bit numbers so a large count can't wrap around. The game's check is `CheckIfShopHasMoney`.
- **`Sell` returns `Busy` while another `Sell` is running.** That can happen when a money handler calls `Sell` while a sale is paying out. It also returns `Busy` while the game's shop screen is open, because the screen keeps a pointer to the player's stack and could sell a stack that `Sell` already removed.
- **The steps run in a fixed order.** The checks take nothing. Then: the shop's money goes down by the total, the pieces leave the player's stock, the shop takes the pieces, and then the wallet is paid OC and then RM. The wallet limit is checked for both currencies before the first step, so the wallet can't refuse after the first part is paid. The framework pays the player last, so money is only created after every stock step has worked, and a money handler sees a finished sale.
  - Technical: the steps use `RemoveSellMoney` in the game's sell, `BuildingSystem.RemoveFurniture`, `FurnitureShop.AddItem`, and `Cash.Give` (the public add behind `Money.AddMoney`). The game pays the wallet first and removes the piece last.
- **If a step fails, the steps already taken are undone in reverse order and the result is `Failed`.**
  - If the RM payment fails after the OC payment, the OC is taken back from the wallet. That undo can't be refused by a save or a nested handler.
  - If the shop took the pieces, the same number is removed from the shop's stock.
  - If the pieces were removed from the player, the player's entry gets back the amount it had before, and goes back to its place in the building stock if it left it. If the removal finished, the task-item count it lowered is added back as well.
  - The shop's money goes back up by the total.
  - An undo that can't be applied is logged as an error. See the limits below.
- **`Sold` fires once, after the sale is complete.** It only fires for sales made with `Sell`. The game's own sales do not raise it. It fires after the `Busy` guard has been released, so a `Sold` handler can call `Sell` again.
- **The results mean this:**
  - `Ok`: every step worked.
  - `Busy`: a save is loading or being written, a money handler is nested too deep, another `Sell` is running, or the shop screen is open.
  - `NotFound`: no furniture shop has that id, or the player holds no matching piece without things inside it.
  - `Unsupported`: the shop is a train shop. See `Unsupported` under Buying.
  - `Refused`: `count` is below 1, `furnitureId` is empty, the piece is free, it has things inside it, the shop does not take it, the total is 0 or less, the total is above 16 million, or the wallet could not hold the amount.
  - `OutOfStock`: the player's entry holds fewer pieces than `count`.
  - `NoMoney`: the shop cannot pay the total in OC or RM. Nothing is taken.
  - `Unavailable`: the player's wallet or home building is not ready.
  - `Failed`: a step was refused or failed. Every step already taken is undone.
- **A shop has no opening hours, so `Sell` never refuses because a shop is closed.**
- **`Sell` takes the first matching entry without contents.** A piece that the player holds in two entries (for example with different extra data) sells from the first one only, and `Sell` returns `OutOfStock` if that entry is short. `Sell` does not refresh the building menu.
- **The undo has limits.** If a money handler spends the OC just paid and the RM payment then fails, the OC can't be taken back, and an error goes to the log. If the task-item update inside `RemoveFurniture` fails, the building stock is restored but the task-item count stays as the failure left it. If a shop's `AddItem` fails after it recorded a custom picture path, the path stays.
  - Technical: the task-item update is in `BuildingSystem.RemoveFurniture`.
