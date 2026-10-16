---
title: Base shops API
parent: API reference
nav_order: 3
---

# Base shops API

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.baseshops`.
Files: `src/Api/BaseShops.cs`.

This area wraps the `BaseShop` base class: the money a shop holds, its restock, and the buy and sell of furniture with a `FurnitureShop`. It does not buy from or sell to a `TrainShop`. See "Buy", "Sell" and "Not exposed".

## Threading and safety

- Main thread only.
- Every member is safe at any time. With no save loaded, `All` returns an empty list.
- Nothing throws. A null or empty `id` finds nothing.
- `SetMoney` and `Restock` return `false` while a save is loading or saving. `Buy` and `Sell` return `Busy` then.

## API

```csharp
public static class BaseShops
{
    public static List<BaseShopInfo> All();
    public static bool SetMoney(string id, int oc, int rm);
    public static bool Restock(string id);
    public static BaseShopBuyResult Buy(string id, string furnitureId, int count);
    public static BaseShopSellResult Sell(string id, string furnitureId, int count);
    public static event Action<BaseShopBoughtArgs> Bought;
    public static event Action<BaseShopSoldArgs> Sold;
}

public enum BaseShopBuyResult
{
    Ok, Busy, NotFound, Unsupported, Refused, OutOfStock, NoMoney, Unavailable, Failed
}

public enum BaseShopSellResult
{
    Ok, Busy, NotFound, Unsupported, Refused, OutOfStock, NoMoney, Unavailable, Failed
}

public sealed class BaseShopInfo
{
    public string Id { get; }           // the save GUID, or the scene path
    public string Kind { get; }         // the C# type name, for example FurnitureShop or TrainShop
    public int Oc { get; }              // BaseShop.currentOcMoney
    public int Rm { get; }              // BaseShop.currentRmMoney
    public bool RestockDaily { get; }   // BaseShop.restockEveryDay
}

public sealed class BaseShopBoughtArgs
{
    public string Id { get; }           // same as BaseShopInfo.Id
    public string Kind { get; }         // the C# type name, FurnitureShop
    public string FurnitureId { get; }  // Furniture.Id of the stock entry bought
    public int Count { get; }
    public int Oc { get; }              // total OC paid
    public int Rm { get; }              // total RM paid
}

public sealed class BaseShopSoldArgs
{
    public string Id { get; }           // same as BaseShopInfo.Id
    public string Kind { get; }         // the C# type name, FurnitureShop
    public string FurnitureId { get; }  // Furniture.Id of the piece sold
    public int Count { get; }
    public int Oc { get; }              // total OC paid to the player
    public int Rm { get; }              // total RM paid to the player
}
```

- `All()` lists every `BaseShop` in the active scene. It uses `FindObjectsByType`, which skips inactive GameObjects.
- `SetMoney(id, oc, rm)` writes `currentOcMoney` and `currentRmMoney` directly. The game has no setter for them. The sale checks read these fields, so the new amounts limit what the shop can pay out. Negative amounts return `false`.
- `Restock(id)` calls the game's `Restock()`. It draws new OC and RM amounts in the shop's ranges, then calls `UpdateShopItems()`, which a subclass overrides. The restock timer is not reset. `timeToRestock` and `restockedDay` keep their values, so the next scheduled restock is unchanged.
- `RestockDaily` true means the shop restocks on the first minute tick of a new day. Otherwise it restocks on its own timer, between `minRestockingTime` and `maxRestockingTime` seconds.

## Buy

`Buy(id, furnitureId, count)` buys `count` units of one stock entry from a `FurnitureShop`, as the game's shop screen does (`ShopBaseUI.Buy`, `ShopBaseUI`).

- **Stock entry.** The first entry of `GetAvailableItems()` whose `furniture.Id` is `furnitureId`. Its `amount` must be at least `count`. There is no stock listing call yet, so the id comes from the piece itself (for a mod piece, the id `FurnitureRegistry` returned).
- **Price.** The game's buy price per unit: `GetOcBuyPrice` and `GetRmBuyPrice` (`BaseShop`, `buyPercent`), times `count`. The game's buy has no haggling, discount or owner price.
- **Money.** The player's wallet only, in OC and then RM (`Money.RemoveMoney`, `Money`, through `Cash.TrySpend`). The bank is not used.
- **Order.** The checks run first and take nothing: save not busy, id and count, the stock entry and its amount, a custom-image piece with an image (`ShopBaseUI.InfoButtonPressed`), the wallet holding both prices. Then the wallet is charged OC, then RM. Then the piece goes to the player's building stock (`BuildingSystem.AddFurniture`, `BuildingSystem`). Only after that does the shop give up the stock (`RemoveItem`) and gain the money (`AddBuyMoney` in the game's buy, `BaseShop`).
- **Failure after payment.** If the RM charge fails, the OC charge is refunded. If the delivery fails or throws, both charges are refunded, and the stock and the shop's money are left as they were. If the shop's stock removal fails or throws after the delivery, the delivered count is taken back out of the building stock and both charges are refunded, and the result is `Failed`. If the delivered count cannot be taken back out, the charges are not refunded, an error is logged, and the result is `Failed`, so the player never keeps both the piece and the money. A refund adds the money back without the wallet cap check and without the save and nesting refusal, so it cannot be refused for those reasons. If the game's money add itself throws, the refund is logged at Error and the money is not returned. The wallet is an int for OC, so the OC refund is exact. The RM wallet is a float, so a charge and its refund can differ by one float step.
- **Event.** `Bought` fires once, after the stock and the shop's money have changed. It fires for `Buy` calls only. A purchase made in the game's own shop screen does not raise it.
- **Results.**
  - `Ok`: the purchase is complete. The shop's stock removal comes after the delivery and must succeed; if it fails, the delivery is undone and the charges refunded, and the result is `Failed`. A throw from the shop's till update is logged and the purchase stands.
  - `Busy`: a save is loading or saving, a Cash handler is nested too deep, or the game's shop screen is open (`ShopController.menuIsOpen`), whose selected entry would otherwise point at a removed stock entry.
  - `NotFound`: no furniture shop has that id, or the shop has no entry with that furniture id.
  - `Unsupported`: the shop is a `TrainShop`. A bought train is placed on the track by `TrainShopUI.AddItem` from the screen's preview object, and a sold train is destroyed from the track. The framework reproduces neither.
  - `Refused`: `count` below 1, an empty `furnitureId`, a custom-image piece without its image, or a total above the Cash limit.
  - `OutOfStock`: the entry has fewer units than `count`.
  - `NoMoney`: the wallet does not hold the OC or RM price. Nothing is taken.
  - `Unavailable`: the wallet or the building system is not ready.
  - `Failed`: a charge was refused, delivery failed, or an exception was caught before delivery. Any money taken is refunded.
- **Closed.** A furniture shop has no opening hours in the game, so `Buy` has no closed refusal. Hours belong to `Trade` (`ShopHours`, `docs/api/shophours.md`).
- **Inventory full.** The game sets no limit on the building stock: `AddFurniture` returns `true` every time. `Buy` has no full refusal to mirror.
- **Crime, stolen and save.** The game's buy sets no crime or stolen flag, and `Buy` sets none. The game marks nothing dirty; the shop's money and stock are written at the next game save (`BaseShop.OnSavingGame`, `BaseShop`).

## Sell

`Sell(id, furnitureId, count)` sells `count` units of one piece to a `FurnitureShop`, as the game's sell does (`ShopBaseUI.Sell`, `ShopBaseUI`). That method is private and reads the open screen's selected slot, so the framework repeats its steps with public calls, as `Buy` does.

- **Player stock.** The first entry of `BuildingSystem.availableFurnitures` whose `furniture.Id` is `furnitureId` and whose `gameObject` is null. A piece with contents is skipped; the game refuses it with "Contains Items!". Its `amount` must be at least `count`.
- **Refusals.** A free piece (`Furniture.IsFree`, `Furniture`) and a piece the shop does not take (`CanSell`, `FurnitureShop`) are refused, as the game's sell gates refuse them.
- **Price.** `GetOcSellPrice` and `GetRmSellPrice` (`BaseShop`, `sellPercent`) per unit, times `count`, worked out in 64-bit so a large count cannot wrap. A total below 0 in either currency (a mod piece with a negative price), or 0 in both, is refused. The shop must hold the total in OC and in RM. The game checks one unit (`CheckIfShopHasMoney`, `BaseShop`); `Sell` checks the total, so a sale is never part paid.
- **Busy.** `Sell` returns `Busy` while another `Sell` runs, for example when a `Cash.MoneyChanged` handler calls `Sell` during the payment. It also returns `Busy` while the game's shop screen is open (`ShopController.menuIsOpen`), because the screen keeps a pointer to the player's stack and could sell a stack that `Sell` already removed.
- **Order.** The checks take nothing. Then, in this order:
  1. The shop's money goes down by the total (`RemoveSellMoney` in the game's sell, `BaseShop`).
  2. The piece leaves the player's stock (`BuildingSystem.RemoveFurniture`, and the call `ShopBaseUI.RemoveItemFromPlayer` makes).
  3. The shop takes the piece (`AddItem`, `FurnitureShop`).
  4. The wallet is paid OC, then RM, through `Cash.Give`, the public add behind `Money.AddMoney`. The wallet cap is checked for both before step 1, so it cannot refuse after the first part is paid.

  The game pays the wallet first and removes the piece last. The framework pays last, so money is created only after every stock step has succeeded, and a `MoneyChanged` handler sees a finished sale.
- **Failure.** If a step returns `false` or throws, the steps already taken are undone in reverse order and the result is `Failed`:
  - The RM payment failed after the OC payment: the OC is taken back with an unguarded spend that mirrors the refund, so the undo cannot be refused by a save or a nested handler.
  - The shop took the piece: the same count is removed from that shop entry (`RemoveItem`).
  - The removal from the player started: the entry gets back the `amount` saved before the call, and goes back to its place in the building stock if it left it. If the removal finished, the task-item count it lowered is added back as well.
  - The shop's money goes back up by the total.

  An undo that cannot be applied is logged at Error. See "Ceilings and risks".
- **Event.** `Sold` fires once after the sale is complete, for `Sell` calls only. It fires after the `Busy` guard is released, so a `Sold` handler can call `Sell`. The game's own sells do not raise it.
- **Results.**
  - `Ok`: every step succeeded.
  - `Busy`: a save is loading or saving, a Cash handler is nested too deep, another `Sell` is running, or the shop screen is open.
  - `NotFound`: no furniture shop has that id, or the player holds no matching piece without contents.
  - `Unsupported`: the shop is a `TrainShop`. See the `Unsupported` result under Buy.
  - `Refused`: `count` below 1, an empty `furnitureId`, a free piece, a piece with contents, a piece the shop does not take, a total of 0 or less, a total above the Cash limit, or a wallet that cannot hold the amount.
  - `OutOfStock`: the player's entry holds fewer units than `count`.
  - `NoMoney`: the shop cannot pay the total in OC or RM. Nothing is taken.
  - `Unavailable`: the wallet or the building system is not ready.
  - `Failed`: a step was refused or threw. Every step already taken is undone.
- **Closed.** There is no opening-hours check, as in `Buy`.

## Not exposed

- Buying from or selling to a train shop. See `Unsupported` above.
- The stock list. Each subclass owns it (for example `FurnitureShop.availableFurnitures`). Furniture stock is covered by the furniture area.
- The price percentages (`sellPercent`, `buyPercent`) and the amount ranges.
- Market stalls. See `docs/api/marketshops.md`.

## Ceilings and risks

- `SetMoney` writes the fields directly. A shop screen that is already open may show the old amounts until it refreshes.
- `Restock` on a `FurnitureShop` runs the furniture hooks on `UpdateShopItems` (`src/Api/FurniturePatches.cs`).
- `Buy` takes the first matching stock entry. A piece with custom images can have several entries, so one `Buy` cannot take units from two of them; it returns `OutOfStock`.
- `Buy` does not run the game's buy UI. A purchase made here does not refresh an open shop screen.
- A display purchase (`InteractableFurnitureDisplay.Purchase`) is a different path and does not go through `Buy`.
- `Sell` takes the first matching player entry without contents. A piece kept in two entries (different meta) sells from the first one only, and returns `OutOfStock` if that entry is short.
- `Sell` does not refresh the building menu.
- `Sell`'s undo has limits. If a `MoneyChanged` handler spends the OC just paid and the RM payment then fails, the OC cannot be taken back. If the task-item update inside `RemoveFurniture` throws, the building stock is restored but the task-item count stays as the throw left it. A shop `AddItem` that throws after recording a custom image path keeps that path. The first case is logged at Error.
