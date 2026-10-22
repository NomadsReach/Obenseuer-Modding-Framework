---
title: Player shops
parent: Economy
grand_parent: Money and shops
nav_order: 7
---

# Player shops

This page uses `Vector3` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

A player shop is a shop the player runs themselves, in a furniture area of their own. It has prices, a cash register, a till that holds the earnings, and sales. See the [glossary](../../glossary.md#player-shop). `PlayerShops` lets you list these shops, change their prices, open and close them, work with their till, and sell things to them.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerShops</code> <span>list, price, till and sell</span></p>

Call `All()` or `Get(shopId)` to read the player's shops, `SetPrice` to price an item, and `Sell` to make a sale through one.

<div class="api-pair" markdown="1">

```csharp
public static class PlayerShops
{
    public static bool IsAvailable { get; }
    public static event Action<SoldArgs> Sold;
    public static List<PlayerShopInfo> All();
    public static PlayerShopInfo Get(string shopId);
    public static List<StockItem> Stock(string shopId);
    public static bool CollectEarnings(string shopId);
    public static string Ensure(string managerGuid);
    public static bool Release(string managerGuid);
    public static bool SetOpen(string shopId, bool open);
    public static bool SetMarkup(string shopId, float factor);
    public static bool SetPrice(string shopId, int itemId, int price);
    public static bool ClearPrice(string shopId, int itemId);
    public static float GetPrice(string shopId, int itemId, object[] meta, out MoneyCurrency currency);
    public static bool AddEarnings(string shopId, MoneyCurrency currency, float amount);
    public static bool TakeEarnings(string shopId, MoneyCurrency currency, float amount);
    public static float Earnings(string shopId, MoneyCurrency currency);
    public static bool Sell(string shopId, ContainerInfo container, int slot, int expectedItemId, int count, bool byWorker);
    public static List<ContainerInfo> Storages(string shopId);
    public static List<Vector3> Registers(string shopId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<PlayerShopInfo>` | Every player shop in the scene you are in. |
| `Get(shopId)` | `PlayerShopInfo` | One shop. `null` when there is none. |
| `Stock(shopId)` | `List<StockItem>` | What the shop has for sale. |
| `CollectEarnings(shopId)` | `bool` | Pays the till out to the player. |
| `Ensure(managerGuid)` | `string` | Turns a furniture manager into a player shop. Gives its shop id. |
| `Release(managerGuid)` | `bool` | Removes a shop that `Ensure` made. |
| `SetOpen(shopId, open)` | `bool` | Opens or closes the shop. |
| `SetMarkup(shopId, factor)` | `bool` | Sets the general price factor. |
| `SetPrice(shopId, itemId, price)` | `bool` | Sets the price of one item. |
| `ClearPrice(shopId, itemId)` | `bool` | Goes back to the general factor for that item. |
| `GetPrice(shopId, itemId, meta, out currency)` | `float` | The price of one unit. Sets `currency` to the shop's currency. |
| `AddEarnings(shopId, currency, amount)` | `bool` | Puts money in the till. |
| `TakeEarnings(shopId, currency, amount)` | `bool` | Takes money out of the till. |
| `Earnings(shopId, currency)` | `float` | The money in the till. |
| `Sell(shopId, container, slot, expectedItemId, count, byWorker)` | `bool` | Sells a stack to the shop. |
| `Storages(shopId)` | `List<ContainerInfo>` | The storage furniture that `Sell` can take from. |
| `Registers(shopId)` | `List<Vector3>` | Where the shop's cash registers are. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the price change or the shop save data could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Sold` | `SoldArgs` | A sale was made through `PlayerShops.Sell`. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerShopInfo</code> <span>a snapshot of one player shop</span></p>

`PlayerShops.All()` and `Get(shopId)` give you these. `Open` is `true` only when the shop is open, has a cash register and is in a valid area. `UncollectedOc` and `UncollectedRm` are the earnings waiting in the till.

<div class="api-pair" markdown="1">

```csharp
public sealed class PlayerShopInfo
{
    public string Id { get; }
    public bool Open { get; }
    public bool HasCashRegister { get; }
    public bool ResidentsOnly { get; }
    public bool InvalidArea { get; }
    public MoneyCurrency Currency { get; }
    public float PriceFactor { get; }
    public int StockCount { get; }
    public int UncollectedOc { get; }
    public float UncollectedRm { get; }
    public string ManagerGuid { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | Names this shop. |
| `Open` | `bool` | `true` when the shop is open, has a cash register and is in a valid area. |
| `HasCashRegister` | `bool` | `true` when the shop has a cash register. |
| `ResidentsOnly` | `bool` | `true` when only residents can shop here. |
| `InvalidArea` | `bool` | `true` when the shop is not in a valid area. |
| `Currency` | `MoneyCurrency` | The money the shop charges in. |
| `PriceFactor` | `float` | The general price factor. Prices are multiplied by it. |
| `StockCount` | `int` | How many units are on sale. |
| `UncollectedOc` | `int` | The OC waiting in the till. |
| `UncollectedRm` | `float` | The RM waiting in the till. |
| `ManagerGuid` | `string` | The id of the shop's furniture manager, which `Ensure` takes. `null` when there is none. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SoldArgs</code> <span>what you get when a sale is made with PlayerShops.Sell</span></p>

`PlayerShops.Sold` gives you one of these for each sale made through `PlayerShops.Sell`. `Stolen` is `true` for goods that belong to someone else.

<div class="api-pair" markdown="1">

```csharp
public sealed class SoldArgs
{
    public string Shop { get; }
    public int ItemId { get; }
    public int Count { get; }
    public float Price { get; }
    public MoneyCurrency Currency { get; }
    public bool Stolen { get; }
    public bool ByWorker { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Shop` | `string` | The shop. |
| `ItemId` | `int` | The item sold. |
| `Count` | `int` | How many were sold. |
| `Price` | `float` | The money earned, in the shop's currency. |
| `Currency` | `MoneyCurrency` | The shop's currency. |
| `Stolen` | `bool` | `true` when the items are marked as someone else's (the stack's owner id is above 0). That is the game's test for stolen goods. |
| `ByWorker` | `bool` | `true` when a worker made the sale. |

</div>
</div>

## Things to know

- Player shops are found in the scene you are in only. `All` lists that scene's shops, including ones that are switched off.
- For a shop the framework already tracks, `SetOpen`, `SetMarkup`, `SetPrice`, `ClearPrice` and `AddEarnings` still update its record and return `true` from other scenes. The game's objects are updated the next time that shop is written, when a save finishes loading, or when its level becomes the scene you are in outside a load, as long as the shop is in that scene. `Earnings` reads the record of a tracked shop from any scene.
- `Ensure(managerGuid)` turns a furniture manager in the scene you are in into a player shop, and gives you the shop id, `<managerGuid>-shop`. A manager that already has a shop gives that shop's id. A new shop starts closed with a price factor of 1.2, and it only works with a cash register in the manager's furniture. The manager's area does not change. The framework saves which managers it turned into shops, and does it again after a load.
- `ManagerGuid` is the id that `Ensure` takes. `Ensure(info.ManagerGuid)` on a shop the game placed gives that shop's id, and a second call gives the same id.
- `Release(managerGuid)` removes a shop that `Ensure` made. It returns `false` while the till holds money (collect it first), for a shop `Ensure` did not make, and while a save is busy.
- A shop the game placed is taken over by the framework the first time you change it. The first `SetOpen`, `SetMarkup`, `SetPrice`, `ClearPrice`, `AddEarnings`, `TakeEarnings` or `Sell` on its id takes it over, and so does `Ensure` on its manager. From then on, the framework's record sets the open flag, the price factor and the till, and the framework's price replaces the game's price settings and factor. A game factor of 0 becomes 1.2.
- `Sell` takes the shop over before it checks the container and the stack, so a refused `Sell` can still take it over. `GetPrice` does not. The game's save keeps the open flag, the price factor and the till that the framework wrote, so they stay in the save even after the framework is removed. <span class="tech">(this is called adoption. It applies to a `PlayerShopManager` that `Ensure` did not make. The framework's price replaces the game's `SetPrices` and factor.)</span>
- `SetOpen` opens or closes a shop. `SetMarkup` sets the general price factor, which must be above 0. `SetPrice` sets one item's price, which replaces the factor for that item. The price uses the game's unit: an OC shop charges that many OC, and an RM shop charges the price times 0.1 RM, as the game does. `ClearPrice` puts the item back on the general factor.
- `GetPrice` gives the price of one unit. `meta` is the item's extra data, passed to the game's price method: pass `null` for an item without any. It does not take the shop over, so before that it gives the game's own price.
- `AddEarnings` puts money in the till. OC loses any fraction and RM is rounded, the same as `Cash` does. `Earnings` reads the till for one currency. `CollectEarnings` pays the till out to the player. <span class="tech">(the rounding is `Cash.TryQuantize`.)</span>
- `TakeEarnings` takes exactly `amount` from the till. Use it to pay a worker's wage out of the till. The amount is rounded as `AddEarnings` does. It returns `true` only when the till holds at least that much. A shop the game placed is taken over only when it is taken from and the till is enough, as with `AddEarnings`.
- `Sell(shopId, container, slot, expectedItemId, count, byWorker)` sells `count` items from a stack to the shop. The stack is in `slot` of `container`. The container must be one of `Storages(shopId)`, and the stack must still be `expectedItemId`. It takes the stack, adds the unit price times `count` to the till (for an RM shop, the RM total), and raises `Sold`. The stack keeps its owner (whose item it is) and its extra data. `byWorker` goes to `SoldArgs.ByWorker`. <span class="tech">(it takes the stack as `Containers.TakeStack` does.)</span>
- For example, this mod sells everything in every storage of every player shop once a save is loaded, and warns when a sale is refused. The limits are in the [Limits](#limits) section below.

  ```csharp
  using System.Collections.Generic;
  using ObenseuerCore;

  [ModInfo("example.selleverything", "Sell Everything", "0.1.0")]
  [ModDependency("obenseuer.moddingframework", "0.11.0")]
  public class SellEverything : FrameworkMod
  {
      protected override void OnLoad()
      {
          Events.WhenReady(() =>
          {
              foreach (PlayerShopInfo shop in PlayerShops.All())
              {
                  foreach (ContainerInfo container in PlayerShops.Storages(shop.Id))
                  {
                      foreach (ItemStackInfo info in Containers.GetItems(container))
                      {
                          if (PlayerShops.Sell(shop.Id, container, info.Slot, info.ItemId, info.Count, false))
                              Log.Info("Sold " + info.Count + " of item " + info.ItemId + " at " + shop.Id + ".");
                          else
                              Log.Warn("The sale was refused at " + shop.Id + ". See the limits below.");
                      }
                  }
              }
          });
      }
  }
  ```

- `Storages` lists the storage furniture under the shop's manager, which are the only containers `Sell` takes from. `Stock` lists the items for sale and the sales storages, not these. <span class="tech">(`Stock` lists `ItemsForSale` and the sales storages.)</span>
- `Registers` gives the positions of the shop's cash registers, read when you call it. A register belongs to the shop when the register's manager is the shop: the game sets that when the register starts, and the framework sets it for a shop it makes. When no register is linked, it lists the registers under the shop's furniture manager. The positions are the registers' own origins, not the spot a customer stands at. Choosing the spot in front is up to your mod.

  ```csharp
  using System.Collections.Generic;
  using ObenseuerCore;
  using UnityEngine;

  [ModInfo("example.registertills", "Register Tills", "0.1.0")]
  [ModDependency("obenseuer.moddingframework", "0.11.0")]
  public class RegisterTills : FrameworkMod
  {
      protected override void OnLoad()
      {
          Events.WhenReady(() =>
          {
              foreach (PlayerShopInfo shop in PlayerShops.All())
              {
                  List<Vector3> tills = PlayerShops.Registers(shop.Id);
                  if (tills.Count == 0)
                  {
                      Log.Warn("No register was found for " + shop.Id + ". See the limits below.");
                      continue;
                  }

                  Log.Info(shop.Id + " has " + tills.Count + " tills. The first stands at " + tills[0] + ".");
                  break;
              }
          });
      }
  }
  ```

- `Stock` shows unit prices with the framework's price change applied. <span class="tech">(it uses `PlayerShopData.GetItemPrice`.)</span>
- `Sold` is raised only by `Sell`. The game's own sale methods do nothing in this version of the game (see the next section).

## Limits

- `IsAvailable` is `false` when the price change or the shop save data could not be set up. The calls that change something then return `false` or `null`.
- `Ensure` returns `null` when the manager is not in the scene, a save is busy, or `IsAvailable` is `false`.
- `GetPrice` returns 0 on failure: an unknown item or shop, the framework unavailable, or a game error (logged once).
- `Registers` gives an empty list for an unknown shop, a shop not in the scene you are in, or a failure (logged once).
- A shop in a neighbouring level that the game loads in the background is not found: `Get`, `Stock`, `Storages`, `GetPrice`, `CollectEarnings`, `Sell` and `Ensure` do not reach it, and the framework does not take it over.
- Every change returns `false` (and `Ensure` returns `null`) while the game is saving or loading. That covers `SetOpen`, `SetMarkup`, `SetPrice`, `ClearPrice`, `AddEarnings`, `TakeEarnings`, `Sell`, `CollectEarnings`, `Ensure` and `Release`.
- `SetMarkup` needs a factor above 0. `SetPrice` returns `false` for an item id of 0 or less, an unknown item, or a price below 0.
- `AddEarnings` needs an amount above 0 and up to the money limit, and an amount that rounds to 0 returns `false`.
- `TakeEarnings` returns `false` and changes nothing when the till holds less than `amount`, the amount is not above 0 or not a real number, the amount rounds to 0, the shop is unknown, or the call is refused (a save is busy, or handlers are nested too deep).
- `Sell` refuses a unit price of 0 and a total that rounds to 0 before it takes anything. If writing to the till throws an error, the till and the stack are put back and `Sell` returns `false`.
- `Sell` also returns `false` for a null container, a count below 1, a slot that is out of range or empty, a stack smaller than `count`, a locked container, an NPC-owned container that does not allow taking, a shop that is not in the scene you are in, and a refused call (a save is busy, or handlers are nested too deep). `slot` is `ItemStackInfo.Slot` from `Containers.GetItems`, and -1 is refused.
- The game never reports a sale in a player shop, so there is nothing to watch. In this version, the player shop's sale code is empty: the methods that should make a sale do nothing, and the count of items sold is never written. There is no game method to attach to. <span class="tech">(`PlayerShopData.MakeSale` and `FindItemAndMakeSale` have empty bodies, `BrowseAndMakePurchase` returns `false`, and `itemsSold` is never written.)</span>
- `CollectEarnings` pays out the till through the game's own method, and raises `Cash.MoneyChanged`. It returns `false` when nothing is owed, while a save is loading or being written, and when it runs inside a handler nested deeper than 8 levels.
