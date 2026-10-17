---
title: Buying from and selling to a trader
parent: Traders
grand_parent: People
nav_order: 5
---

# Buying from and selling to a trader

`Buy` and `Sell` do one whole trade for the player, without the trade screen. They use the game's own money, stock and price rules, so price factors and the `Shops.Bought`, `Shops.Sold` and `Shops.PriceQuery` events all apply. A trade is all or nothing: if it can't be done in full, nothing changes.

```csharp
public static class Traders
{
    public static bool Buy(string modId, string traderId, int itemId, int count, out TraderTradeRefusal refusal);    // the player buys count of an item; true when done
    public static bool Sell(string modId, string traderId, int itemId, int count, out TraderTradeRefusal refusal);   // the player sells count of an item; true when done
}

public enum TraderTradeRefusal    // why a trade was refused
{
    None,                 // the trade was done
    NotReady,             // the framework, the money or the inventory is not loaded yet
    BadInput,             // the mod id is not valid, the count is below 1, or the item id is unknown
    SaveBusy,             // a save is being written or loaded
    UnknownTrader,        // no trader has that shop id
    Closed,               // the trader is closed
    OutOfStock,           // the trader has fewer than count units that Buy can take
    NoRoom,               // the player has no room for the items (buy), or the trader has no room for them (sell)
    PlayerCannotAfford,   // the player can't pay the price (buy)
    TraderCannotAfford,   // the trader can't pay the price (sell)
    NotEnoughItems,       // the player holds fewer than count units that Sell can take
    NotAccepted,          // the trader's categories rule out items the player holds, so too few can be sold
    Failed,               // something went wrong in the middle and the trade was undone
    SpecialTrader,        // the trader gives and takes certificates; the framework does not trade with it
    LiquidItem,           // the item is a liquid, or holds one
    TradeBusy             // another trade is running, or the call came from a Shops.PriceQuery handler
}
```

- **`Buy` and `Sell` return `true` when the trade was done.** Otherwise they return `false` and `refusal` says why. A refusal before anything changed shows no message in the game. The log gets one line for each kind of trade and reason.
- **`Buy` gives the player the items first, then takes the units from the trader's stock, then takes the money.** The game's own `Trade.Buy` only moves the money, so the framework moves the items too.
- **`Buy` only takes ordinary stock.** That is units nobody owns, not stolen, and priced by the game as a new item. A stolen unit, or one the game prices differently (such as a worn one), stays on the shelf and is not counted. If there are fewer than `count` units that `Buy` can take, you get `OutOfStock`.
  - Technical: units with no owner and no stolen mark, which the game prices as a new item.
- **A bought unit keeps the shelf unit's owner and item data, such as its skin.** Its cold (refrigerated) state is not kept.
- **`Sell` takes `count` of the item from the player's stacks that the trader accepts.** It goes one stack at a time. It skips units that the trader doesn't accept: a stolen unit the trader doesn't buy, or a unit the trader's allowed and forbidden categories rule out. Those units stay with the player and are not sold.
- **`Sell` refuses and changes nothing when the accepted units are fewer than `count`.** You get `NotAccepted` when the trader's categories ruled out a unit the player holds, otherwise `NotEnoughItems`. It also refuses when the trader can't pay for the whole sale, or has no room for the items.
- **Each stack is priced on its own, so its wear, quality and stolen mark count.** A sold unit keeps its owner and item data in the trader's stock. The trader keeps it unless the trader destroys what it buys.
  - Technical: each stack is priced with its own owner and meta. A stolen unit sold to the trader is never one stolen from that trader, and it keeps its owner so the game's stolen price factor applies to the sale.
- **A trade is not a crime.** Taking a unit that someone owns, or selling stolen goods through `Sell`, does not commit a theft, the same as in the game's own trade screen.
- **Liquids can't be traded here.** An item that is a liquid, or holds one such as a filled bottle, is refused with `LiquidItem`, because the liquid can change when stacks merge and an undo could not restore that.
- **Special traders can't be traded with.** A trader that gives and takes certificates is refused with `SpecialTrader`.
  - Technical: these are `TradeSpecial`.
- **A trade can't start while another trade is running, or from inside a `Shops.PriceQuery` handler.** The call refuses with `TradeBusy`.
  - Technical: while a trade runs, a `Shops.PriceQuery` handler can still read and set the price. But these writes are refused: `Cash` writes (`Give`, `TrySpend`, and the banking and mail writes that check them), `PlayerInventory.Give` and `Take`, and `Traders.SetMoney` and `Restock`. Other framework writes, such as shop stock and equipment, are not blocked.
- **The trade screen does not need to be open.** A trade does not check whether the trade screen is open for that trader.
- **Events are held back until the trade is done.** Handlers for `Shops.Bought`, `Shops.Sold`, `PlayerInventory.ItemAdded`, `PlayerInventory.ItemRemoved` and `Cash.MoneyChanged` run after the trade is complete, so they never run in the middle of one. A buy raises one `Shops.Bought`. A sale raises one `Shops.Sold` for each stack. Item events come once per item with the total moved. A bought item's "New item" notice shows once with the total. A refused or undone trade raises none of these.
- **If a trade fails halfway, it is undone, and you get the reason.** Only the money the trade moved goes back, to the player and to the trader. The trader's stock and the player's inventory (the main, backpack and character slots) are written back slot by slot, into the same slots, with the same amount, owner, quality, liquid and other item data.
  - Technical: the undo writes slots back directly, so the game's take and drop callbacks do not run again, and the undo does not take back the game's own callbacks that ran during a step.
- **The undo has limits.** If a sale of a worn item (a hat, a backpack, a held item) fails after the item is taken, the item is back in its slot, but the game may not treat it as worn until the player moves it. In rare slot cases a purchase can put one unit on the floor, and the call then refuses and undoes the rest, leaving that unit on the floor. The player never loses money either way.
- **A finished trade is saved at the next save, like a trade on the trade screen.** That is the trader's money and trade balance, the trader's stock, and the player's inventory and money. The framework keeps no record of your trades, so keep your own in your own save data if you need one.
- **The game's price rules are not changed.** Item value, stolen goods and containers are priced as the game does. Price factors only scale the result.
