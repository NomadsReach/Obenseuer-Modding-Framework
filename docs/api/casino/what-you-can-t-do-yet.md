---
title: What you can't do (yet)
parent: Casino
grand_parent: Money and shops
nav_order: 5
---

# What you can't do (yet)

- **You can't set a stake, a machine's money or the odds.** Mods can't change them, on purpose. The player does those things at the machine. Payout factors are the only write.
- **You can't change the odds of the slot machine.** Its win chance is a fixed number in the game's code, so changing it would mean rewriting that code. The scratchcard's odds come from the item data.
  - Technical: changing it needs a transpiler.
- **You can't change scratchcard prizes with a payout factor.** See above.
- **You can't read the player's gambling totals or addiction stats here.**
  - Technical: `PlayerStatsGambling` is not wrapped.
