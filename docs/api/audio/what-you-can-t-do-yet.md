---
title: What you can't do (yet)
parent: Audio
grand_parent: Screen and UI
nav_order: 9
---

# What you can't do (yet)

- **You can't play or stop the game's music tracks.** The game has no music or radio player that you could talk to. Each radio in the world holds its own sound and plays it by itself, so there is no one place to say "play this track".
  - Technical: the `Radio` class is one per prop and holds its own `SoundInfo`.
- **You can't play the click and hover sounds of the game's buttons.** The game stores each button's sound on the button itself, with no sound name you could pass in. For a notification sound, use `Hud.Notify` (see [HUD](../hud.md)): the notification box plays its own sound.
  - Technical: the button sounds are a `SoundInfo` on each button prefab.
- **There are no other controls for the ambient sound.** You can read which one is playing and switch it, as described above, and that is all.
