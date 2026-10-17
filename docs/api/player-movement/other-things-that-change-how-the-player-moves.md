---
title: Other things that change how the player moves
parent: Player movement
grand_parent: Player life and skills
nav_order: 7
---

# Other things that change how the player moves

- **The framework's own tweaks do not change how the player moves.** The tweaks cover needs, time, crafting, stacks, camera and renovations. Nothing else in the framework writes the walk speed, run speed or jump height.
  - Technical: `walkSpeed`, `runSpeed` and `baseJumpHeight`.
- **The mushroom effect check does not change speed.** `Effects.IsActive` with `Effect.Mushroom` only reports whether the effect is on.
- **The game's own orange mushroom effect adds to the run speed, and your walk multiplier does not touch it.** Sprint speed is only the game's value plus the mushroom boost. `WalkSpeedMultiplier` does not change the run speed, so the two do not combine.
  - Technical: the boost is added inside the `runSpeed` getter (`OrangeMushroomEffect.GetRunSpeedModifier`).
