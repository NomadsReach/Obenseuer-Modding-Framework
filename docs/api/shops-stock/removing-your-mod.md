---
title: Removing your mod
parent: Shop stock
grand_parent: Money and shops
nav_order: 5
---

# Removing your mod

- **Trader stock (`AddTraderStock`).** Once your mod is removed, nothing is topped up any more. Stacks already on the trader's shelves keep your item's id, and the game shows them as "Missing item", the same as for any removed mod's item. The player should sell or take them first.
- **Placed stock (`Place`).** The stack stays in the trader's shop and in the save. With your mod removed, it turns into a "Missing item" too. The player should sell or take it first. The save record of the placing is ignored once the mod is gone.
- **Vending entries (`AddVendingEntry`).** They are only held in memory and added again on each start, so once your mod is removed they are simply gone, and no leftover item reaches the player. A stock count saved for a removed entry stays in the save, and is used only if the entry is added again.
