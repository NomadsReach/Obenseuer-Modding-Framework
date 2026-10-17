---
title: Details
parent: NPC appearance
grand_parent: People
nav_order: 8
---

# Details

This page uses `Mesh` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.npcappearance.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes no game method for this API. It only reads and changes the parts that are on screen, so it has no `IsAvailable` and no feature group. The name `obenseuer.moddingframework.npcappearance` is the one you will see in the log.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- The reading calls are safe at any time. `Meshes`, `MaterialNames`, `ShapeKind`, `GetLook` and `BlendShapes` give an empty list, `None` or `null` when there is nothing to read.

## What to check for and what comes back

- A bad argument throws an error. `modId` must be 1 to 64 characters of letters, digits, `.`, `_` or `-`, and `mesh` and `shape` must not be empty.
- `materialNames` must hold at least one name, and no name may be null or empty. `weight` must be a real number. Any of these throws an `ArgumentException`, and a `null` look throws an `ArgumentNullException`.
- If the game itself fails while the framework works, you get an empty answer instead of an error. The answer is the plain default (an empty list, `None`, `null` or `false`), and the first failure of each member goes to the log as an error. Later failures of that member are not logged again.
