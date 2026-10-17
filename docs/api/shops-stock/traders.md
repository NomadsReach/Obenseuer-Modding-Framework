---
title: Traders
parent: Shop stock
grand_parent: Money and shops
nav_order: 3
---

# Traders

- **`AddTraderStock` keeps `amount` of the item in the trader's shop.** The trader gets back whatever is missing every time the save loads and every time the shop restocks, but only into empty slots. It returns `true` when your request is kept, even if the shop is not loaded yet. Calling it again for the same mod, shop and item replaces the amount.
  - Technical: the framework counts that item in the trader's `Storage` and adds the missing amount, after `Storage.OnLoadingGame` and after `Storage.SpawnItems`.
- **If another mod keeps the same item in the same shop, both requests are kept and the larger amount wins.** Each one tops up to its own amount, and one warning names both mods. If you use two different keys that match the same shop (the id and the title, for example), the framework does not notice.
- **Stock goes only into empty slots.** If there is no empty slot, the rest is skipped and one warning goes to the log. The game limits each stack to the item's stack size.
- **`Place` puts `amount` of the item into the trader's empty slots once, for each save.** It returns `true` when at least one item was placed. A second call for the same mod, shop and item returns `false`, because the framework remembers it in the save. A shop without an id can't be used with `Place`.
  - Technical: the record is kept per save, keyed by the mod, the shop GUID and the item.
- **`Remove` takes back your request, but not the items already on the shelf.** It returns `true` when it found a trader request or a vending entry of yours for that shop and item. It also removes the vending entry from machines that are loaded right now.
