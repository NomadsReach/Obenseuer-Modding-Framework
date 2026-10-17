---
title: Rules for moving an NPC
parent: NPC movement
grand_parent: People
nav_order: 3
---

# Rules for moving an NPC

- **Only NPCs that are really in the scene can be moved.** Every call that changes an NPC works only on one that is loaded, switched on and alive. An NPC that is not in the scene returns `false`, and the call does not touch the game's NPC records.
  - Technical: the one exception is a claim whose NPC goes out of the scene while a patrol or stand your mod started is running. The claim is dropped, the activity is stopped, and the saved activity is cleared (`state.CurrentActivity = null`, when it is still that activity).
- **A claim belongs to your mod.** Every call takes the `modId` of your mod. An invalid `modId` throws an `ArgumentException`, as the other calls that take a `modId` do.
- **Failures return `false` and do not throw.** The game's own behaviour never throws an error to your code. The first failure of each member is logged as an error.
- **Calls that change an NPC return `false` while a save is loading or being written.**
- **The schedule always comes back.** When the last hold on an NPC goes, the NPC's schedule goes on again. If the NPC cannot take it up at that moment (a save is loading or being written, or the NPC is switched off), the release waits and is tried again each frame. If the NPC's controller is destroyed, the claim is dropped and a patrol or stand your mod started on it is stopped.
- **A scene load releases your claims.** A single-scene load releases every claim, one at a time, using the same rule. An additive load (a neighbouring scene loaded alongside) does not. A patrol or stand that your mod started is stopped, and the NPC goes back to its schedule. Claim again from your own `Events.SceneLoaded` handler if you still need it.
