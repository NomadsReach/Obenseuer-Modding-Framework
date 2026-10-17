---
title: What you can't do (yet)
parent: Instruments
grand_parent: Objects and devices
nav_order: 5
---

# What you can't do (yet)

- **You can't change how the player's own long note works.** The game starts and stops that one from the key. A note from `HoldNote` is only a plain play of one long sound, so it makes no comments.
  - Technical: whether the clip loops and how loud it is are set on the guitar prefab, not in code, and the asset data was not read.
- **You can't choose notes or pitches.** A guitar has one strum sound and a set of long sounds. A song is only a timed series of strums.
  - Technical: the game code has no note table, and the clip contents are asset data that was not read.
- **There are no other instruments.** The game has no piano, drum or other instrument.
