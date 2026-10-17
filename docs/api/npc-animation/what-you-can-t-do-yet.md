---
title: What you can't do (yet)
parent: NPC animation
grand_parent: People
nav_order: 7
---

# What you can't do (yet)

- You can't use animation events inside the clip. The framework does not run them.
- You can't mask a clip to some bones, change its speed, or play a second clip on the same NPC.
- A clip makes no sound.
- You can't mix a clip with the game's animation triggers. The game's own animator keeps its state, and its values are not copied into your clip.
