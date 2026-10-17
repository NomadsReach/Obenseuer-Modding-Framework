---
title: Look and shape
parent: Mod NPCs
grand_parent: People
nav_order: 5
---

# Look and shape

A mod NPC's look is its own private copy of the character's look: the body dials, the clothes and the blend shapes (the sliders that change the face and body). The framework stores only what is different from the source. The next time the same `npcId` is added, it applies the stored look before the game's own start-up code runs, so the person starts with the changed look.

## Things to know

- A look is saved when the person is removed and when the game saves. When the person is released (a scene change, `Despawn` or `Clear`) and at each game save, the framework stores what differs from the source.
- For a `SpawnSaved` person, the look comes back after a load. It is stored in the framework's file for the save. For a `Spawn` person, it is kept for the game session only. `Clear(modId)` forgets it.
- The look is the body dials and the clothes of a slider person, and the blend shapes of any mod NPC. The person keeps the source's gender, because gender is not part of the look.
- A saved file may hold a gender or an advanced flag. The framework reads those and ignores them.
- `Restore` and `Clear` on `NpcAppearance` also forget the saved look, when your mod calls them for a person in the loaded scene. The person's next spawn then starts from the source's look.
- A slider person's body reloads at once. A person that uses blend shapes keeps its current weights until it is added again.
- A look is written only to your copy. A person from the game's own cast is not affected.

## Limits

- A look holds at most 256 blend shapes for each person. When the file loads, names are checked and numbers must be real numbers. Anything else is dropped, and the rest of the look is kept.
