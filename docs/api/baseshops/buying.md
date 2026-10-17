---
title: Buying
parent: Base shops
grand_parent: Money and shops
nav_order: 3
---

# Buying

```csharp
public enum BaseShopBuyResult    // what happened when you tried to buy
{
    Ok,            // the purchase is complete
    Busy,          // not now: a save is loading or being written, money handlers are nested too deep, or the shop screen is open
    NotFound,      // no furniture shop has that id, or the shop has no such furniture
    Unsupported,   // the shop is a train shop
    Refused,       // the request itself is not allowed (see below)
    OutOfStock,    // the shop has fewer pieces than you asked for
    NoMoney,       // the player cannot pay; nothing was taken
    Unavailable,   // the player's wallet or home building is not ready
    Failed         // a step went wrong; any money taken was given back
}

public sealed class BaseShopBoughtArgs    // what Bought tells you
{
    public string Id { get; }           // the shop's id, the same as BaseShopInfo.Id
    public string Kind { get; }         // the type of shop; always FurnitureShop
    public string FurnitureId { get; }  // the id of the furniture that was bought
    public int Count { get; }           // how many pieces
    public int Oc { get; }              // the total OC paid
    public int Rm { get; }              // the total RM paid
}
```

`Buy(id, furnitureId, count)` buys `count` pieces of one kind of furniture from a furniture shop for the player, the way the shop screen does.

- **You find the furniture by its furniture id.** There is no call that lists a shop's stock yet, so you need the id of the piece itself. For a piece your mod added, it is the id that the furniture registry gave you. The shop must hold at least `count` pieces of it.
  - Technical: the stock entry is the first entry of `GetAvailableItems()` whose `furniture.Id` is `furnitureId`, and its `amount` must be at least `count`.
- **The price is the shop's buying price for each piece, times `count`.** There is no haggling, discount or owner price.
  - Technical: it uses the game's `GetOcBuyPrice` and `GetRmBuyPrice` (which use `buyPercent`).
- **The money comes from the player's wallet only.** The bank is not used. OC is taken first, then RM.
  - Technical: it uses `Money.RemoveMoney` through `Cash.TrySpend`.
- **The framework checks everything before it takes any money.** The save must not be busy, the id and count must be valid, the shop must have the piece and enough of it, a piece that needs a custom picture must have one, and the wallet must hold both prices. Then it charges OC, then RM, then puts the piece in the player's building stock. Only after that does the shop give up its stock and gain the money.
  - Technical: a custom-image piece is one with `canPlaceCustomImage`; the game checks this in `ShopBaseUI.InfoButtonPressed`. The delivery is `BuildingSystem.AddFurniture`. The shop's stock goes through `RemoveItem` and its money through the game's `AddBuyMoney`.
- **If a step fails after the player paid, the player does not lose the money.** If the RM charge fails, the OC charge is refunded. If the delivery fails, both charges are refunded and the shop's stock and money are left as they were. If the shop can't remove its stock after the delivery, the piece is taken back out of the player's building stock, both charges are refunded, and the result is `Failed`. If the piece can't be taken back, the money is not refunded, an error goes to the log, and the result is `Failed`, so the player never keeps both the piece and the money.
  - Technical: a refund adds the money back without the wallet-limit check and without the save and nesting refusals, so those can't block it. If the game's money add itself throws, the refund is logged as an error and the money is not returned. OC is a whole number in the wallet, so its refund is exact. RM is a decimal number, so a charge and its refund can differ by one step of the number type.
- **`Bought` fires once, after the stock and the shop's money have changed.** It only fires for purchases made with `Buy`. A purchase made on the game's own shop screen does not raise it.
- **The results mean this:**
  - `Ok`: the purchase is complete. A problem with the shop's till afterwards is written to the log and the purchase stands.
  - `Busy`: a save is loading or being written, a money handler is nested too deep, or the game's shop screen is open. The screen has to be closed because its selected entry would otherwise point at stock that was just removed.
  - `NotFound`: no furniture shop has that id, or the shop has no entry with that furniture id.
  - `Unsupported`: the shop is a train shop. Buying a train places it on the track from the shop screen's preview, and selling one removes it from the track. The framework does neither.
  - `Refused`: `count` is below 1, `furnitureId` is empty, a piece that needs a custom picture has none, or the total is above 16 million in either currency.
  - `OutOfStock`: the shop holds fewer pieces than `count`.
  - `NoMoney`: the wallet does not hold the OC or RM price. Nothing is taken.
  - `Unavailable`: the player's wallet or home building is not ready.
  - `Failed`: a charge was refused, the delivery failed, or an error was caught before delivery. Any money taken is given back.
  - Technical: `Busy` comes from `Cash.Refused`, which is true while a save is loading or being written, while the player's inventory is in the middle of a change (`PlayerInventory.InTransaction`), and when `MoneyChanged` or `Received` handlers are nested too deep. The 16 million limit is `Cash.MaxAmount`. The shop screen is `ShopController.menuIsOpen`.
- **A shop has no opening hours, so `Buy` never refuses because a shop is closed.** Opening hours belong to trading with characters: see [Shop hours](../shophours.md).
- **The player can always hold more furniture.** The game puts no limit on the building stock, so `Buy` has no "full" result.
  - Technical: `AddFurniture` returns `true` every time.
- **A purchase does not count as a crime and does not mark anything as stolen.** The shop's money and stock are saved at the next game save.
  - Technical: the game's buy sets no crime or stolen flag. The shop's data is written by `BaseShop.OnSavingGame`.
- **`Buy` takes the first matching entry only.** A piece with custom pictures can have several entries, and one `Buy` can't take pieces from two of them. It returns `OutOfStock` instead. `Buy` also does not refresh a shop screen that is open, and a purchase from a furniture display in the world goes a different way and is not covered.
  - Technical: the display purchase is `InteractableFurnitureDisplay.Purchase`.
