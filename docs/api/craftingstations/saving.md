---
title: Saving
parent: Crafting stations
grand_parent: Items and crafting
nav_order: 6
---

# Saving

- **Speed and quality factors are not saved, and every full scene load clears them.** Set them again from an `Events.SceneLoaded` handler, as the quick example does. Finished items keep their quality and a running craft keeps its time, because the game saves both.
  - Technical: every `LoadSceneMode.Single` scene load clears them.
