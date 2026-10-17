---
title: Shops
parent: Base shops
grand_parent: Money and shops
nav_order: 2
---

# Shops

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public static class BaseShops
{
    public static List<BaseShopInfo> All();                   // every shop in the scene you are in
    public static bool SetMoney(string id, int oc, int rm);   // sets the money a shop holds; true when it was set
    public static bool Restock(string id);                    // makes a shop restock; true when it was told to
    public static BaseShopBuyResult Buy(string id, string furnitureId, int count);     // buys furniture from a shop for the player
    public static BaseShopSellResult Sell(string id, string furnitureId, int count);   // sells furniture from the player to a shop
    public static event Action<BaseShopBoughtArgs> Bought;    // the player bought furniture through Buy
    public static event Action<BaseShopSoldArgs> Sold;        // the player sold furniture through Sell
}

public sealed class BaseShopInfo    // a snapshot of one shop
{
    public string Id { get; }           // the shop's id; pass it to the other calls
    public string Kind { get; }         // the type of shop, such as FurnitureShop or TrainShop
    public int Oc { get; }              // how many OC the shop holds
    public int Rm { get; }              // how many RM the shop holds
    public bool RestockDaily { get; }   // true when the shop restocks every day; false when it restocks on a timer
}
```

- **`All()` lists the shops that are switched on in the scene you are in.** A shop in another scene, or one the game has hidden, is not listed. With no save loaded you get an empty list.
  - Technical: it searches the active objects of the active scene for `BaseShop`. `Kind` is the C# type name.
- **A shop id is a piece of text that names one shop.** `All()` gives it to you. It is stable as long as the scene does not change. A null or empty id finds nothing.
  - Technical: the id is the shop's save GUID when it has one, otherwise its scene path.
- **`SetMoney(id, oc, rm)` sets how much money the shop holds.** The shop pays the player out of this money when it buys furniture, so the new amounts limit what it can pay. It returns `false` for a negative amount, for an unknown shop, and while a save is loading or being written. A shop screen that is already open can show the old amounts until it refreshes.
  - Technical: it writes `currentOcMoney` and `currentRmMoney` directly, because the game has no setter. The sale checks read those fields.
- **`Restock(id)` makes the shop restock now.** It draws new amounts of money for the shop and refreshes its stock. It does not reset the shop's restock timer, so the next restock the game planned still happens when it was planned. It returns `false` for an unknown shop and while a save is loading or being written.
  - Technical: it calls the game's `Restock()`, which draws new OC and RM amounts in the shop's ranges and then calls `UpdateShopItems()`, which a subclass overrides. `timeToRestock` and `restockedDay` keep their values. For a furniture shop this runs the framework's furniture changes on `UpdateShopItems`.
- **`RestockDaily` tells you when the shop restocks by itself.** When it is `true`, the shop restocks at the start of each new day. Otherwise it restocks on its own timer, after a time chosen between its smallest and largest restock time.
  - Technical: the daily restock happens on the first minute tick of a new day. The timer runs between `minRestockingTime` and `maxRestockingTime` seconds.
- **Some things about shops are not part of this API.** The list of what a shop has in stock belongs to each kind of shop, and furniture stock is on the [furniture page](../furniture.md). The price percentages and the stock amount ranges are not available.
  - Technical: each subclass owns its stock list, for example `FurnitureShop.availableFurnitures`. The percentages are `sellPercent` and `buyPercent`.
