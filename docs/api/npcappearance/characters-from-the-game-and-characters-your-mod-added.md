---
title: Characters from the game and characters your mod added
parent: NPC appearance
grand_parent: People
nav_order: 6
---

# Characters from the game and characters your mod added

- **A change to a character from the game lasts for this play session only.** The framework changes the parts that are on screen. It never edits the game's shared character data, so nothing carries over to another spawn of the same character, to another scene or to a save. The game sets a character's looks again when the character is created, so a change made in the same frame the scene loads can be lost.
- **A change to a mod NPC can last longer.** The change is made on the parts on screen and on the mod NPC's own copy of the character data, so it survives the game setting the looks again. `Restore` and `Clear` undo both. The look and blend shape changes are kept with the character. For a mod NPC added with `SpawnSaved`, they are saved with the game and come back after a load. For one added with `Spawn`, they last for the session. Changes to parts and materials last for the session only. See [Look and shape](../modnpcs/look-and-shape.md#look-and-shape).
- **`Restore` and `Clear` while a save is loading or being written do not undo looks and blend shapes.** Your look and blend shape changes stay, and a later call undoes them. The part and material changes are undone at once.
