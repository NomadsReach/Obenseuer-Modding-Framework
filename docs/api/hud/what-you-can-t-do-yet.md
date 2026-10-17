---
title: What you can't do (yet)
parent: HUD
grand_parent: Screen and UI
nav_order: 8
---

# What you can't do (yet)

- You can't set the "press to use" text. The game rewrites it every frame for whatever the player is standing at, so your text would be overwritten at once. <span class="tech">(This is `UseText`. It is rewritten every frame by the interactable under the player.)</span>
- You can't show the tooltips for items and task items. `ShowTooltip` and `HideTooltip` only show your own title and text. The game fills its item and task item tooltips from the item data. <span class="tech">(`ShowTooltip` and `HideTooltip` call `ToolTip.Activate(string, string)` and `Deactivate()`. Item tooltips use `ToolTip.Activate(ItemData, ...)` and task-item tooltips use `ToolTip.Activate(TaskItem)`. The framework offers neither.)</span>
- You can't use the card game's tutorial messages. They belong to a card table and mean nothing outside a card game. <span class="tech">(This is `TutorialMessageManager`, a card-game tutorial queue bound to a card table.)</span>
