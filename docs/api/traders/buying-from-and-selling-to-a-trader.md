---
title: Buying from and selling to a trader
parent: Traders
grand_parent: People
nav_order: 5
---

# Buying from and selling to a trader

`Buy` and `Sell` do one whole trade for the player, without the trade screen. They use the game's own money, stock and price rules, so price factors and the `Shops.Bought`, `Shops.Sold` and `Shops.PriceQuery` events all apply. A trade is all or nothing: if it can't be done in full, nothing changes.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Traders</code> <span>buys and sells for the player</span></p>
Use `Buy` and `Sell` to make one trade for the player.

<div class="api-pair" markdown="1">

```csharp
public static class Traders
{
    public static bool Buy(string modId, string traderId, int itemId, int count, out TraderTradeRefusal refusal);
    public static bool Sell(string modId, string traderId, int itemId, int count, out TraderTradeRefusal refusal);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Buy(modId, traderId, itemId, count, out refusal)` | `bool` | The player buys `count` of an item. Returns `true` when done. |
| `Sell(modId, traderId, itemId, count, out refusal)` | `bool` | The player sells `count` of an item. Returns `true` when done. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TraderTradeRefusal</code> <span>why a trade was refused</span></p>
`Buy` and `Sell` give you this through `out refusal`. It is `None` when the trade went through, and otherwise says why it did not, such as `OutOfStock`. The game has no such list; the framework sets these values, with `Failed` when nothing more specific applies.

<div class="api-pair" markdown="1">

```csharp
public enum TraderTradeRefusal
{
    None,
    NotReady,
    BadInput,
    SaveBusy,
    UnknownTrader,
    Closed,
    OutOfStock,
    NoRoom,
    PlayerCannotAfford,
    TraderCannotAfford,
    NotEnoughItems,
    NotAccepted,
    Failed,
    SpecialTrader,
    LiquidItem,
    TradeBusy
}
```

| Value | Meaning |
|---|---|
| `None` | The trade was done. |
| `NotReady` | <details>{::nomarkdown}<summary>{:/nomarkdown}The game is not loaded far enough yet.{::nomarkdown}</summary>{:/nomarkdown}Trade once a save has loaded, for example from Events.WhenReady. Nothing was bought or sold.</details> |
| `BadInput` | <details>{::nomarkdown}<summary>{:/nomarkdown}Something you passed is not allowed.{::nomarkdown}</summary>{:/nomarkdown}Check the mod id, a count of at least 1, and an item id the game knows. Nothing was traded.</details> |
| `SaveBusy` | <details>{::nomarkdown}<summary>{:/nomarkdown}A save is being written or loaded.{::nomarkdown}</summary>{:/nomarkdown}Try the trade again once the save finishes. Nothing was traded.</details> |
| `UnknownTrader` | No trader has that shop id. |
| `Closed` | The trader is closed. |
| `OutOfStock` | <details>{::nomarkdown}<summary>{:/nomarkdown}The trader does not have that many.{::nomarkdown}</summary>{:/nomarkdown}Ask for fewer, or read the stock first. Nothing was bought.</details> |
| `NoRoom` | <details>{::nomarkdown}<summary>{:/nomarkdown}There is no room for the items.{::nomarkdown}</summary>{:/nomarkdown}Buying needs room in the player inventory, selling needs room on the trader. Free some space and try again. Nothing was traded.</details> |
| `PlayerCannotAfford` | <details>{::nomarkdown}<summary>{:/nomarkdown}The player cannot pay.{::nomarkdown}</summary>{:/nomarkdown}Ask for a quote before buying. No money moved.</details> |
| `TraderCannotAfford` | <details>{::nomarkdown}<summary>{:/nomarkdown}The trader cannot pay.{::nomarkdown}</summary>{:/nomarkdown}Traders carry limited money. Sell fewer units, or sell to another trader. Nothing was sold.</details> |
| `NotEnoughItems` | <details>{::nomarkdown}<summary>{:/nomarkdown}The player does not hold that many.{::nomarkdown}</summary>{:/nomarkdown}Some units cannot be sold, for example owned or witnessed stock, so count what the trader will take rather than what the player holds.</details> |
| `NotAccepted` | <details>{::nomarkdown}<summary>{:/nomarkdown}The trader does not deal in that item.{::nomarkdown}</summary>{:/nomarkdown}Each trader takes only certain categories. Check what it buys before offering. Nothing was sold.</details> |
| `Failed` | <details>{::nomarkdown}<summary>{:/nomarkdown}Something went wrong part way through.{::nomarkdown}</summary>{:/nomarkdown}The framework undoes the steps it had taken and logs the error once, so the player keeps their money and items.</details> |
| `SpecialTrader` | The trader gives and takes certificates. The framework does not trade with it. |
| `LiquidItem` | The item is a liquid, or holds one. |
| `TradeBusy` | <details>{::nomarkdown}<summary>{:/nomarkdown}Another trade is already running.{::nomarkdown}</summary>{:/nomarkdown}Do not trade from inside a price handler or from another trade. Wait for the first one to finish.</details> |

</div>
</div>

## Things to know

- `Buy` and `Sell` return `true` when the trade was done. Otherwise they return `false` and `refusal` says why. A refusal before anything changed shows no message in the game. The log gets one line for each kind of trade and reason.
- `Buy` gives the player the items first, then takes the units from the trader's stock, then takes the money. The game's own `Trade.Buy` only moves the money, so the framework moves the items too.
- `Buy` only takes ordinary stock: units nobody owns, not stolen, and priced by the game as a new item. A stolen unit, or one the game prices differently (such as a worn one), stays on the shelf and is not counted. <span class="tech">(units with no owner and no stolen mark, which the game prices as a new item)</span>
- A bought unit keeps the shelf unit's owner and item data, such as its skin.
- `Sell` takes `count` of the item from the player's stacks that the trader accepts, one stack at a time. It skips units that the trader doesn't accept: a stolen unit the trader doesn't buy, or a unit the trader's allowed and forbidden categories rule out. Those units stay with the player and are not sold.
- Each stack is priced on its own, so its wear, quality and stolen mark count. A sold unit keeps its owner and item data in the trader's stock, and the trader keeps it unless the trader destroys what it buys. <span class="tech">(each stack is priced with its own owner and meta; a stolen unit sold to the trader is never one stolen from that trader, and it keeps its owner so the game's stolen price factor applies to the sale)</span>
- A trade is not a crime. Taking a unit that someone owns, or selling stolen goods through `Sell`, does not commit a theft, the same as in the game's own trade screen.
- The trade screen does not need to be open. A trade does not check whether the trade screen is open for that trader.
- Events are held back until the trade is done. Handlers for `Shops.Bought`, `Shops.Sold`, `PlayerInventory.ItemAdded`, `PlayerInventory.ItemRemoved` and `Cash.MoneyChanged` run after the trade is complete, so they never run in the middle of one.
- A buy raises one `Shops.Bought`. A sale raises one `Shops.Sold` for each stack. Item events come once per item with the total moved. A bought item's "New item" notice shows once with the total. A refused or undone trade raises none of these.
- If a trade fails halfway, it is undone, and you get the reason. Only the money the trade moved goes back, to the player and to the trader.
- The trader's stock and the player's inventory (the main, backpack and character slots) are written back slot by slot, into the same slots, with the same amount, owner, quality, liquid and other item data. <span class="tech">(the undo writes slots back directly, so the game's take and drop callbacks do not run again, and the undo does not take back the game's own callbacks that ran during a step)</span>
- A finished trade is saved at the next save, like a trade on the trade screen. That is the trader's money and trade balance, the trader's stock, and the player's inventory and money. The framework keeps no record of your trades, so keep your own in your own save data if you need one.
- The game's price rules are not changed. Item value, stolen goods and containers are priced as the game does. Price factors only scale the result.

## Limits

- `OutOfStock` happens when there are fewer than `count` units that `Buy` can take.
- A bought unit's cold (refrigerated) state is not kept.
- `Sell` refuses and changes nothing when the accepted units are fewer than `count`. You get `NotAccepted` when the trader's categories ruled out a unit the player holds, otherwise `NotEnoughItems`. It also refuses when the trader can't pay for the whole sale, or has no room for the items.
- Liquids can't be traded here. An item that is a liquid, or holds one such as a filled bottle, is refused with `LiquidItem`, because the liquid can change when stacks merge and an undo could not restore that.
- Special traders can't be traded with. A trader that gives and takes certificates is refused with `SpecialTrader`. <span class="tech">(these are `TradeSpecial`)</span>
- A trade can't start while another trade is running, or from inside a `Shops.PriceQuery` handler. The call refuses with `TradeBusy`. <span class="tech">(while a trade runs, a `Shops.PriceQuery` handler can still read and set the price; but these writes are refused: `Cash` writes (`Give`, `TrySpend`, and the banking and mail writes that check them), `PlayerInventory.Give` and `Take`, and `Traders.SetMoney` and `Restock`; other framework writes, such as shop stock and equipment, are not blocked)</span>
- The undo has limits. If a sale of a worn item (a hat, a backpack, a held item) fails after the item is taken, the item is back in its slot, but the game may not treat it as worn until the player moves it.
- In rare slot cases a purchase can put one unit on the floor, and the call then refuses and undoes the rest, leaving that unit on the floor. The player never loses money either way.
