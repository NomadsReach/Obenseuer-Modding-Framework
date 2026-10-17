---
title: The states
parent: Renovations
grand_parent: Places and travel
nav_order: 3
---

# The states

- **`Installed` means the upgrade is already in the building.**
  - Technical: `TenementUpgrade.IsUpgraded` is true.
- **`InProgress` means an order is being built.**
- **`AwaitingFinalPayment` means the building work is done, and the player has to pay the final payment in RM.** The player does that in the game's upgrade panel. This API does not pay it.
  - Technical: the order has `upgradeReady` set.
- **`Available` means the player can order it now.** Everything it needs is already installed, and no other renovation in the same line is being built or waiting for its final payment. A line is a group of upgrades that replace one another, such as the electricity of one apartment. The game's upgrade panel locks a line the same way.
  - Technical: `TenementUpgrade.CanUpgrade` is true without counting items, and no other upgrade in the same `TenementUpgradeGroup` is `InProgress` or `AwaitingFinalPayment`.
- **`Locked` means anything else.** This includes a renovation whose line has another one ordered or waiting for its final payment.
