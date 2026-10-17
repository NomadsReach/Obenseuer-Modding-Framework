---
title: Bad values
parent: NPC animation
grand_parent: People
nav_order: 6
---

# Bad values

- **An invalid mod id throws an error.** The mod id must be 1 to 64 characters of letters, digits, `.`, `_` or `-`. The error is an `ArgumentException`.
- **A `null` clip throws an `ArgumentNullException` named `clip`.**
- **A legacy clip throws an `ArgumentException` named `clip`.** A legacy clip is one made for Unity's old animation system.
  - Technical: it is a clip where `AnimationClip.legacy` is true.
- **A fade that is below 0, not a number, or infinite throws an `ArgumentException` named `fadeSeconds`.**
- **Anything else the game refuses gives `false` and does not throw.** The first failure of each call is logged as an error.
