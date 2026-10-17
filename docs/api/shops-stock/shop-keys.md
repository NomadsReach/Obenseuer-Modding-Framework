---
title: Shop keys
parent: Shop stock
grand_parent: Money and shops
nav_order: 2
---

# Shop keys

This page uses `GameObject` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ShopStock</code> <span>add and place stock in shops</span></p>

This is the class to start from. `AddTraderStock` and `Place` add stock to a trader's shop, `AddVendingEntry` adds an item for sale in a vending machine, and `Remove` takes back what your mod added for an item.

<div class="api-pair" markdown="1">

```csharp
public static class ShopStock
{
    public static bool IsAvailable { get; }
    public static bool AddTraderStock(string modId, string shopKey, int itemId, int amount);
    public static bool AddVendingEntry(string modId, string shopKey, int itemId, int stock, float price, MoneyCurrency currency);
    public static bool Remove(string modId, string shopKey, int itemId);
    public static bool Place(string modId, string shopKey, int itemId, int amount);
}
```

| Method | Returns | What it does |
|---|---|---|
| `AddTraderStock(modId, shopKey, itemId, amount)` | `bool` | Keeps this many of the item in a trader's shop. |
| `AddVendingEntry(modId, shopKey, itemId, stock, price, currency)` | `bool` | Adds the item for sale in a vending machine. |
| `Remove(modId, shopKey, itemId)` | `bool` | Takes back what your mod added for this item in this shop. |
| `Place(modId, shopKey, itemId, amount)` | `bool` | Puts a one-time stack in a trader's shop. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the framework could not set this feature up. |

</div>
</div>

## Things to know

- A shop key says which shop you mean. It can be the shop's id, its title, or, when the shop has no title, the name of its object. Use `Shops.All()` to see the ids.
- If the shop has an id, a key equal to that id always matches. A name that two shops share matches both of them, and `Place` uses the first one it finds. <span class="tech">(The id is the shop's GUID. The title is the shop's `title` field, or the GameObject name when the title is empty.)</span>
- `AddTraderStock` and `Place` work on traders, and `AddVendingEntry` works on vending machines. If your key matches no shop of that kind yet, your request is kept and waits: it is applied when such a shop loads. `Remove` clears both kinds for your mod, that shop and that item.

## Limits

- Bad arguments: a `modId` that is not 1 to 64 letters, digits, `.`, `_` or `-` throws an `ArgumentException`. An empty shop key, an `amount` or `stock` below 1, a price that is not a real number, or an unknown currency does not throw. The call returns `false` and writes one line to the log. Every other failure returns `false` too.
- Item ids 0 and 1 are refused. These are the coins, and `Containers.Give` refuses them as well. An item that is not in the game's item list is skipped: a vending entry writes one warning to the log, and a trader's top-up waits until the item exists.
