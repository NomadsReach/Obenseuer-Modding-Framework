---
title: What you can't do (yet)
parent: Levels
grand_parent: Places and travel
nav_order: 3
---

# What you can't do (yet)

- **You can't pick locks, use keys or use keypads here.** Those are on the [Interaction and locks](../interaction.md) page (`Locks`).
- **You can't make a door relock itself after use.** The game does that inside its own level change.
  - Technical: this is `LockAfterUse`.
- **You can't control the door's animation or sound timing.**
- **You can't find out when a level has finished loading with this API.** Use `Events.SceneLoaded` for that.
