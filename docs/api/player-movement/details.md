---
title: Details
parent: Player movement
grand_parent: Player life and skills
nav_order: 8
---

# Details

This page uses `Vector3` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`.

Everything not listed in this reference is internal and may change without notice.

- The framework changes some of the game's methods when it starts, so it can apply your multipliers and track the camera's look lock.
- This page calls that a feature group: a named set of the framework's changes for one feature. Its feature group is `obenseuer.moddingframework.playermovement`, the name you will see in the log.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- It is safe to call at any time, even on the main menu. With no player, reads return `false`, `Vector3.zero` or the stored multiplier, and `Lock` returns `false`.

## What to check for and what comes back

- `Lock`, `Unlock` and the other holding calls return `true` or `false`, and they do not throw.
- The exceptions are an `ArgumentException` for an invalid mod id, and for a `NaN` written to the `WalkSpeedMultiplier` or `JumpMultiplier` property. The `Set...Multiplier` methods return `false` for `NaN` or an infinite value instead.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the speed changes cannot be made, the multipliers stop applying. `PlayerMovement.IsAvailable` becomes `false`, the setters return `false`, and the framework writes error lines and a warning to the log. The reads, `Lock` and `Unlock` keep working.
- If the rotation changes cannot be made, `LockLook` stops working. It returns `false` and takes nothing.
