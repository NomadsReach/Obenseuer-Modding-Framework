---
title: What you can't do (yet)
parent: Contests
grand_parent: Player life and skills
nav_order: 4
---

# What you can't do (yet)

These things are not possible with contests today.

- You can't start a contest or score one. The game decides how the sauna tolerance falls and how many drinks count. The only way to end one early is `GiveUp()`, which makes the same call as holding interact.
- You can't place bets or collect winnings on a rat fight. That is the game's money path, so the framework does not wrap it. <span class="tech">(`PlaceOCBet`, `PlaceRMBet` and `CollectWinnings` are not wrapped.)</span>
- You can't change the odds of a rat fight. The fight itself decides the winner, when at most one rat is left, and the arena has no chance value to set.
- You can't use the contest's on-screen bar or the arena's betting screen. <span class="tech">(`ContestUI` and the arena's selection UI are not wrapped.)</span>
