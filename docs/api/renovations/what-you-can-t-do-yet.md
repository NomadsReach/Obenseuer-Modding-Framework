---
title: What you can't do (yet)
parent: Renovations
grand_parent: Places and travel
nav_order: 6
---

# What you can't do (yet)

- **You can't pay the final payment or finish a renovation by hand.** That is the player's own action in the game's upgrade panel, and there is no safe call to make it.
  - Technical: it is `UpgradeUI.OnPressed`, and no public call is offered.
- **You can't set a build time or place a silent order.** `Start` only makes the game's normal order.
  - Technical: the framework has no call for `TenementUpgradeStart.Upgrade`.
- **You can't mark a renovation with the star.**
  - Technical: the framework has no call for `TenementController.MarkUpgrade`.
- **The condition and cleaning of apartments are somewhere else.** See the [Cleaning page](../cleaning.md).
