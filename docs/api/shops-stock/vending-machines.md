---
title: Vending machines
parent: Shop stock
grand_parent: Money and shops
nav_order: 4
---

# Vending machines

- **`AddVendingEntry` adds one more item for sale in the machine.** It returns `true` when your request is kept. Calling it again for the same mod, shop and item replaces the stock, price and currency.
- **The machine only gets the entry if it does not already sell that item.** If the game or another mod already sells it, your entry is skipped, and one line in the log names your mod and who sells the item. The log line comes from the first machine that refuses it.
- **A `price` above 0 is the price in the currency you give. A `price` of 0 or less uses the game's own price for the machine.**
  - Technical: the game's price formula for a machine is the item's sale price times 1.2.
- **`stock` is how many the machine has when the entry is added and each time it refills.** On a machine that has an id, the stock left is saved with each game save and restored when the save loads, so reloading does not refill it. A machine without an id refills to `stock` every time it loads.
  - Technical: the stock is written at each game save from the live machines, keyed by machine GUID and item id. Machines that are not loaded at that save keep their last value. Each purchase of a mod entry on a machine with an id also updates the count at once, so a machine that unloads before the next save keeps its count. When the machine refills, its saved counts are dropped, so the full stock stands after a scene change. The counts are applied when the save finishes loading, so they reach a machine whatever order its start and load run in.
