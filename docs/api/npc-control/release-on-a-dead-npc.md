---
title: Release on a dead NPC
parent: NPC control
grand_parent: People
nav_order: 6
---

# Release on a dead NPC

When the NPC is dead at release, the framework puts back the fade distance only. It leaves the walking switch as the game holds it. A dead NPC is not held: `Sight`, `Talk` and `Barks` stop applying as soon as the NPC dies, so the game's own behaviour applies to it.
