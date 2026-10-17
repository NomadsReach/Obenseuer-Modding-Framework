---
title: Player shops
parent: Economy
grand_parent: Money and shops
nav_order: 7
---

# Player shops

A player shop is a shop the player runs themselves, in a furniture area of their own. It has prices, a cash register, a till that holds the earnings, and sales. See the [glossary](../../glossary.md#player-shop). `PlayerShops` lets you list these shops, change their prices, open and close them, work with their till, and sell things to them.

```csharp
public sealed class PlayerShopInfo    // a snapshot of one player shop
{
    public string Id { get; }                // names this shop
    public bool Open { get; }                // true when the shop is open, has a cash register and is in a valid area
    public bool HasCashRegister { get; }     // true when the shop has a cash register
    public bool ResidentsOnly { get; }       // true when only residents can shop here
    public bool InvalidArea { get; }         // true when the shop is not in a valid area
    public MoneyCurrency Currency { get; }   // the money the shop charges in
    public float PriceFactor { get; }        // the general price factor; prices are multiplied by it
    public int StockCount { get; }           // how many units are on sale
    public int UncollectedOc { get; }        // the OC waiting in the till
    public float UncollectedRm { get; }      // the RM waiting in the till
    public string ManagerGuid { get; }       // the id of the shop's furniture manager, which Ensure takes; null when there is none
}

public sealed class SoldArgs    // what you get when a sale is made with PlayerShops.Sell
{
    public string Shop { get; }              // the shop
    public int ItemId { get; }               // the item sold
    public int Count { get; }                // how many were sold
    public float Price { get; }              // the money earned, in the shop's currency
    public MoneyCurrency Currency { get; }   // the shop's currency
    public bool Stolen { get; }              // true when the items are marked as someone else's (the stack's owner id is above 0), which is the game's test for stolen goods
    public bool ByWorker { get; }            // true when a worker made the sale
}

public static class PlayerShops
{
    public static bool IsAvailable { get; }             // false when the price change or the shop save data could not be set up
    public static event Action<SoldArgs> Sold;          // a sale was made through PlayerShops.Sell

    public static List<PlayerShopInfo> All();                       // every player shop in the scene you are in
    public static PlayerShopInfo Get(string shopId);                // one shop; null when there is none
    public static List<StockItem> Stock(string shopId);             // what the shop has for sale
    public static bool CollectEarnings(string shopId);              // pays the till out to the player

    public static string Ensure(string managerGuid);                // turns a furniture manager into a player shop; gives its shop id
    public static bool Release(string managerGuid);                 // removes a shop that Ensure made
    public static bool SetOpen(string shopId, bool open);           // opens or closes the shop
    public static bool SetMarkup(string shopId, float factor);      // sets the general price factor
    public static bool SetPrice(string shopId, int itemId, int price);   // sets the price of one item
    public static bool ClearPrice(string shopId, int itemId);       // goes back to the general factor for that item
    public static float GetPrice(string shopId, int itemId, object[] meta, out MoneyCurrency currency);   // the price of one unit
    public static bool AddEarnings(string shopId, MoneyCurrency currency, float amount);    // puts money in the till
    public static bool TakeEarnings(string shopId, MoneyCurrency currency, float amount);   // takes money out of the till
    public static float Earnings(string shopId, MoneyCurrency currency);                    // the money in the till
    public static bool Sell(string shopId, ContainerInfo container, int slot, int expectedItemId, int count, bool byWorker);   // sells a stack to the shop
    public static List<ContainerInfo> Storages(string shopId);      // the storage furniture that Sell can take from
    public static List<Vector3> Registers(string shopId);           // where the shop's cash registers are
}
```

- **`IsAvailable` is `false` when the price change or the shop save data could not be set up.** The calls that change something then return `false` or `null`.
- **Player shops are found in the scene you are in only.** `All` lists that scene's shops, including ones that are switched off. A shop in a neighbouring level that the game loads in the background is not found: `Get`, `Stock`, `Storages`, `GetPrice`, `CollectEarnings`, `Sell` and `Ensure` do not reach it, and the framework does not take it over.
- **For a shop the framework already tracks, some calls still work from other scenes.** `SetOpen`, `SetMarkup`, `SetPrice`, `ClearPrice` and `AddEarnings` still update its record and return `true`. The game's objects are updated the next time that shop is written, when a save finishes loading, or when its level becomes the scene you are in outside a load, as long as the shop is in that scene. `Earnings` reads the record of a tracked shop from any scene.
- **`Ensure(managerGuid)` turns a furniture manager in the scene you are in into a player shop, and gives you the shop id, `<managerGuid>-shop`.** A manager that already has a shop gives that shop's id. It returns `null` when the manager is not in the scene, a save is busy, or `IsAvailable` is `false`. A new shop starts closed with a price factor of 1.2, and it only works with a cash register in the manager's furniture. The manager's area does not change. The framework saves which managers it turned into shops, and does it again after a load.
- **`ManagerGuid` is the id that `Ensure` takes.** `Ensure(info.ManagerGuid)` on a shop the game placed gives that shop's id, and a second call gives the same id. It is `null` when the shop has no furniture manager.
- **`Release(managerGuid)` removes a shop that `Ensure` made.** It returns `false` while the till holds money (collect it first), for a shop `Ensure` did not make, and while a save is busy.
- **Every change returns `false` (and `Ensure` returns `null`) while the game is saving or loading.** That covers `SetOpen`, `SetMarkup`, `SetPrice`, `ClearPrice`, `AddEarnings`, `TakeEarnings`, `Sell`, `CollectEarnings`, `Ensure` and `Release`.
- **A shop the game placed is taken over by the framework the first time you change it.** The first `SetOpen`, `SetMarkup`, `SetPrice`, `ClearPrice`, `AddEarnings`, `TakeEarnings` or `Sell` on its id takes it over, and so does `Ensure` on its manager. From then on, the framework's record sets the open flag, the price factor and the till, and the framework's price replaces the game's price settings and factor. A game factor of 0 becomes 1.2. `Sell` takes the shop over before it checks the container and the stack, so a refused `Sell` can still take it over. `GetPrice` does not. The game's save keeps the open flag, the price factor and the till that the framework wrote, so they stay in the save even after the framework is removed.
  - Technical: this is called adoption. It applies to a `PlayerShopManager` that `Ensure` did not make. The framework's price replaces the game's `SetPrices` and factor.
- **`SetOpen` opens or closes a shop.** `SetMarkup` sets the general price factor, which must be above 0. `SetPrice` sets one item's price, which replaces the factor for that item. The price uses the game's unit: an OC shop charges that many OC, and an RM shop charges the price times 0.1 RM, as the game does. `SetPrice` returns `false` for an item id of 0 or less, an unknown item, or a price below 0. `ClearPrice` puts the item back on the general factor.
- **`GetPrice` gives the price of one unit and sets `currency` to the shop's currency.** `meta` is the item's extra data, passed to the game's price method: pass `null` for an item without any. It returns 0 on failure: an unknown item or shop, the framework unavailable, or a game error (logged once). It does not take the shop over, so before that it gives the game's own price.
- **`AddEarnings` puts money in the till.** The amount must be above 0 and up to the money limit. OC loses any fraction and RM is rounded, the same as `Cash` does, and an amount that rounds to 0 returns `false`. `Earnings` reads the till for one currency. `CollectEarnings` pays the till out to the player.
  - Technical: the rounding is `Cash.TryQuantize`.
- **`TakeEarnings` takes exactly `amount` from the till.** Use it to pay a worker's wage out of the till. The amount is rounded as `AddEarnings` does. It returns `true` only when the till holds at least that much. It returns `false` and changes nothing when the till holds less, the amount is not above 0 or not a real number, the amount rounds to 0, the shop is unknown, or the call is refused (a save is busy, or handlers are nested too deep). A shop the game placed is taken over only when it is taken from and the till is enough, as with `AddEarnings`.
- **`Sell(shopId, container, slot, expectedItemId, count, byWorker)` sells `count` items from a stack to the shop.** The stack is in `slot` of `container`. The container must be one of `Storages(shopId)`, and the stack must still be `expectedItemId`. It refuses a unit price of 0 and a total that rounds to 0 before it takes anything. Then it takes the stack, adds the unit price times `count` to the till (for an RM shop, the RM total), and raises `Sold`. If writing to the till throws an error, the till and the stack are put back and `Sell` returns `false`. The stack keeps its owner (whose item it is) and its extra data. `byWorker` goes to `SoldArgs.ByWorker`. It also returns `false` for a null container, a count below 1, a slot that is out of range or empty, a stack smaller than `count`, a locked container, an NPC-owned container that does not allow taking, a shop that is not in the scene you are in, and a refused call (a save is busy, or handlers are nested too deep). `slot` is `ItemStackInfo.Slot` from `Containers.GetItems`, and -1 is refused.
  - Technical: it takes the stack as `Containers.TakeStack` does.
- **For example, to sell everything in every storage of a shop:**

  ```csharp
  foreach (ContainerInfo container in PlayerShops.Storages(shopId))
      foreach (ItemStackInfo info in Containers.GetItems(container))
          PlayerShops.Sell(shopId, container, info.Slot, info.ItemId, info.Count, false);
  ```

- **`Storages` lists the storage furniture under the shop's manager, which are the only containers `Sell` takes from.** `Stock` lists the items for sale and the sales storages, not these.
  - Technical: `Stock` lists `ItemsForSale` and the sales storages.
- **`Registers` gives the positions of the shop's cash registers, read when you call it.** A register belongs to the shop when the register's manager is the shop: the game sets that when the register starts, and the framework sets it for a shop it makes. When no register is linked, it lists the registers under the shop's furniture manager. It gives an empty list for an unknown shop, a shop not in the scene you are in, or a failure (logged once). The positions are the registers' own origins, not the spot a customer stands at. Choosing the spot in front is up to your mod.

  ```csharp
  List<Vector3> tills = PlayerShops.Registers(shopId);
  ```

- **`Sold` is raised only by `Sell`.** The game's own sale methods do nothing in this version of the game (see the next bullet).
- **The game never reports a sale in a player shop, so there is nothing to watch.** In this version, the player shop's sale code is empty: the methods that should make a sale do nothing, and the count of items sold is never written. There is no game method to attach to.
  - Technical: `PlayerShopData.MakeSale` and `FindItemAndMakeSale` have empty bodies, `BrowseAndMakePurchase` returns `false`, and `itemsSold` is never written.
- **`CollectEarnings` pays out the till through the game's own method, and raises `Cash.MoneyChanged`.** It returns `false` when nothing is owed, while a save is loading or being written, and when it runs inside a handler nested deeper than 8 levels.
- **`Stock` shows unit prices with the framework's price change applied.**
  - Technical: it uses `PlayerShopData.GetItemPrice`.
