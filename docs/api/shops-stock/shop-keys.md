---
title: Shop keys
parent: Shop stock
grand_parent: Money and shops
nav_order: 2
---

# Shop keys

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public static class ShopStock
{
    public static bool IsAvailable { get; }    // false when the framework could not set this feature up
    public static bool AddTraderStock(string modId, string shopKey, int itemId, int amount);   // keeps this many of the item in a trader's shop
    public static bool AddVendingEntry(string modId, string shopKey, int itemId, int stock, float price, MoneyCurrency currency);   // adds the item for sale in a vending machine
    public static bool Remove(string modId, string shopKey, int itemId);   // takes back what your mod added for this item in this shop
    public static bool Place(string modId, string shopKey, int itemId, int amount);   // puts a one-time stack in a trader's shop
}
```

- **A shop key says which shop you mean.** It can be the shop's id, its title, or, when the shop has no title, the name of its object. If the shop has an id, a key equal to that id always matches. A name that two shops share matches both of them, and `Place` uses the first one it finds. Use `Shops.All()` to see the ids.
  - Technical: the id is the shop's GUID. The title is the shop's `title` field, or the GameObject name when the title is empty.
- **`AddTraderStock` and `Place` work on traders, and `AddVendingEntry` works on vending machines.** If your key matches no shop of that kind yet, your request is kept and waits: it is applied when such a shop loads. `Remove` clears both kinds for your mod, that shop and that item.
- **Bad arguments.** A `modId` that is not 1 to 64 letters, digits, `.`, `_` or `-` throws an `ArgumentException`. An empty shop key, an `amount` or `stock` below 1, a price that is not a real number, or an unknown currency does not throw: the call returns `false` and writes one line to the log. Every other failure returns `false` too.
- **Item ids 0 and 1 are refused.** These are the coins, and `Containers.Give` refuses them as well. An item that is not in the game's item list is skipped: a vending entry writes one warning to the log, and a trader's top-up waits until the item exists.
