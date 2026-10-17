---
title: What you can't do (yet)
parent: NPC appearance
grand_parent: People
nav_order: 7
---

# What you can't do (yet)

- You can't change the bone sizes, eye placement or height of a character with a baked look. The game reads those once, when it creates the character, and this API does not check the character's bones.
- You can't change the skin of a slider character. A slider body always uses the game's first body texture.
- You can't add new blend shapes, meshes or textures. Models and textures are not part of this API.
