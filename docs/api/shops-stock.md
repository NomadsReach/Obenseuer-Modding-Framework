---
title: Obenseuer Modding Framework shop stock API (ShopStock)
parent: API reference
nav_order: 72
---

# Obenseuer Modding Framework shop stock API (ShopStock)

Experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.shopstock`. Logs go to `BepInEx\logs\obenseuer.moddingframework.shopstock.log`.

Everything not listed here is internal. Main thread only.

Scope: adds stock to two kinds of shop. A trader (`Trade`) gets a fixed stack of a mod item in its empty slots. A vending machine gets one more entry in its `sellableItems` list. A trader's stock is not a price table: traders still price by item value, so there is no per-item trader price.

## Rules

- **Shop key.** A key matches a shop when it equals the shop's GUID, or its title, or its GameObject name when it has no title. The GUID wins when it is set. A name shared by two shops matches both, and the first one found is used by `Place`. Use `Shops.All()` to find the GUIDs.
- **Kinds.** `AddTraderStock` and `Place` act on traders. `AddVendingEntry` acts on vending machines. A key that matches no shop of that kind is kept and waits: it is applied when such a shop loads. `Remove` clears both kinds for that mod, shop and item.
- **Owners.** Patches use `obenseuer.moddingframework.shopstock`. They hook `VendingMachine.Start`, `VendingMachine.OnLoadingGame` and `Storage.OnLoadingGame`, and they extend the framework's existing `Storage.SpawnItems` postfix, which the LootPools patches register under `obenseuer.moddingframework.loot`. A failed hook disables the owner that registered it, and no other. The trader top-up is the one part that runs inside the LootPools postfix, so with the LootPools patches off a trader's stock is topped up only by `AddTraderStock` on a shop that is already loaded, not on a load or a respawn. Vending entries and `Place` do not use that postfix.
- **Ids.** Ids 0 and 1 are refused, as `Containers.Give` refuses them. An item that is not in the item database is skipped: a vending entry logs one warning, a trader top-up waits until the item exists.
- **Empty slots only.** A trader's stock is placed only in empty slots. If there is no empty slot the rest is skipped and one warning is logged. A stack is clamped to the item's stack size by the game.
- **Vending entries.** An entry is added only when the machine does not already sell that item, from the game or from another mod. A skipped entry is logged once, for the first machine that refuses it, naming the skipped mod and the mod that sells the item, or the game. Its price is the given `price` in `currency` when `price` is above 0, otherwise the game's price formula. Its stock is `stock` when the entry is added and at each refill. On a machine with a GUID the stock left in the entry is kept: it is saved with each game save and restored on load, so a reload does not refill it. A machine without a GUID restocks to `stock` on each load.
- **Bad arguments.** An invalid mod id throws `ArgumentException`. An empty shop key, `amount` or `stock` below 1, a non-finite price, or an unknown currency returns `false` and is logged once. Everything else returns the fallback.
- **Save and load.** Writes are refused while `SaveData.Busy` is true, so the public calls return `false` during a load or a save. The top-up on load and the vending append run inside the game's own load and spawn code, so they are not refused.

## API

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

- `IsAvailable` is `installed && !CorePatcher.IsDisabled(owner)`. The other three methods return `false` when it is false.
- `AddTraderStock` keeps `amount` of the item in the trader's storage. It is kept for each load and each respawn: the trader gets back the missing count into empty slots. It returns true when the entry is kept, even if the shop is not loaded yet. A call for the same mod, shop and item replaces the amount. When another mod already keeps the same item under the same shop key, both are kept, each tops up to its own amount so the larger wins, and one warning names both mods. Two different keys that match one shop are not detected.
- `AddVendingEntry` appends one entry to the machine's list. It returns true when the entry is kept. A call for the same mod, shop and item replaces the stock, price and currency. `price` above 0 is an absolute price in `currency`. `price` 0 or below uses the game's formula, which is the item's sale price times 1.2 for a machine.
- `Remove` drops that mod's trader stock and vending entry for the shop and item, and removes the vending entry from loaded machines. It returns true when either was found. It does not take back items that a trader already holds.
- `Place` puts `amount` of the item into a trader's empty slots once. It returns true when at least one item was placed. It is recorded per save, keyed by the mod, the shop GUID and the item, so a second call for the same key returns `false`. A shop without a GUID cannot be used with `Place`.

## Removal safety

- **Trader stock (`AddTraderStock`).** After the mod is removed nothing is topped up. Stacks already in the trader's slots keep the mod's item id, and the game shows them as "Missing item" placeholders, the same as any removed mod's item. The player should sell or take them first.
- **Placed stock (`Place`).** The stack stays in the trader's storage and is kept in the save. With the mod removed it turns into a "Missing item" placeholder. Players should sell or take it before the mod is removed. Its save record is ignored when the mod is gone.
- **Vending entries (`AddVendingEntry`).** They are held in memory and added again on each start, so with the mod removed they are simply absent, and no orphan item reaches the player. A stock count saved for a removed entry stays in the save record, keyed by machine and item, and is applied only if that entry is added again.

## Lifetime

- Trader stock and vending entries are kept in memory by the framework, not saved. A mod registers them again on each start, the same as `LootPools`.
- `Place` writes its records per save into the save's `mods` folder, `obenseuer.moddingframework.shopstock.json`, with the vending stock. The file is cleared on the Main Menu, like other mod save data.
- Vending stock is written at each game save from the live machines, keyed by machine GUID and item id. Machines not loaded at that save keep their last value. Each purchase from a mod entry on a GUID machine also updates its value at once, so a machine that unloads (leaving its scene) before a save keeps the count, and a return visit does not refill it. A respawn refill of the machine drops its saved counts, so the full stock stands after a scene change. It is applied when the save finishes loading, so it reaches a machine whatever order its start and load run in.
