---
title: How several mods share the sky
parent: World sky
grand_parent: Places and travel
nav_order: 4
---

# How several mods share the sky

## Things to know

- When more than one mod sets the same property, the newest mod wins. When that mod clears its values, the one before it applies. When no mod has the property, the game's own value comes back. The framework writes one warning to the log that names both mods when two of them set the same property.
- `Set` and `Clear` work while a save is loading. They are not held back, so setting your values again from `Events.SceneLoaded` works. <span class="tech">(neither is gated on `SaveData.Busy`.)</span>
- Nothing is ever written to the save.

## Limits

- Your values do not stay when a save loads or the level changes. Set them again from `Events.SceneLoaded` or `Events.Loaded`. <span class="tech">(overrides are cleared when the game starts to load a save and on a `LoadSceneMode.Single` scene load. Fog and ambient values belong to the scene, so on a scene change they are not written back. A skybox material is written back.)</span>
