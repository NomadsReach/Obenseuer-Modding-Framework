---
title: The character panel rows
parent: Custom skills
grand_parent: Player life and skills
nav_order: 3
---

# The character panel rows

Each registered skill shows up in the skills list on the player's character panel.

## Things to know

- Your skill's row looks like the game's own rows. It shows the display name as the title, `LVL` and the experience text, and the game's ten level bars. The bars and the maximum marker follow the value divided by `maxValue`, and the `LVL` and experience text use your skill's own value. <span class="tech">(A row is an instance of the game's `CharacterPanelSkill` prefab, added under `skillList` after the game's rows.)</span>
- Your skill's row always shows, even at 0. The game hides its own skills until they are above 1. When one of your rows is shown, the whole skills group is shown too, even when the player has no learned game skills yet.
- Rows do not pile up. Each time the panel refreshes, the framework finds your row again instead of adding a new one. The row of a skill you cleared is hidden. <span class="tech">(The change is a postfix on `CharacterPanel.UpdateSkills`. A row is found by its name, `CustomSkill:` plus the skill's key, under `skillList`, and created only when none is there. `OnEnable` runs `Init` once and then `UpdateSkills`, so the rows exist before the first refresh.)</span>
