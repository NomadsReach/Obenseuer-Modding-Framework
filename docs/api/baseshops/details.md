---
title: Details
parent: Base shops
grand_parent: Money and shops
nav_order: 5
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. File: `src/Api/BaseShops.cs`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes no game method for this API, so it has no `IsAvailable` and no feature group. The name `obenseuer.moddingframework.baseshops` is the one you will see in the log.

## When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- It is safe to call any of it at any time. With no save loaded, `All` gives an empty list, `SetMoney` and `Restock` give `false`, and `Buy` and `Sell` give `Busy` or `Unavailable`.
- Your `Bought` and `Sold` handlers run one at a time. If one throws an error, the framework logs it and still runs the others.
- The framework does not hold back `Bought` and `Sold` while a save is loading or being written. A handler that should ignore those moments can check `SaveData.Busy`, which is `true` during a load or a save.

## What to check for and what comes back

- Nothing here throws an error. A null or empty id finds no shop. `SetMoney` and `Restock` give `false` while a save is loading or being written, and `Buy` and `Sell` give `Busy` then.
- If the game itself fails while the framework works, you get an empty answer or `Failed` instead of an error. The first failure of each member goes to the log as an error. Later failures of that member are not logged again.
