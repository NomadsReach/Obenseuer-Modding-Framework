---
title: Self-check
parent: Game save data
grand_parent: Framework basics
nav_order: 3
---

# Self-check

`SaveData.SelfCheck()` runs a built-in test and returns `game save data self-check ok`, or the first case that failed. It needs a loaded save, because it works on the player's inventory.

**It changes the live inventory.** It applies the inventory's own entry back to it, which rebuilds its slots. Run it on a scratch save, not one you care about.

How a tester runs it, with no test mod: set `[Debug] SaveDataSelfCheck = true` in `BepInEx\config\obenseuer.moddingframework.cfg` and start the game. The framework then adds the console command `core_savedata_check`. Load a scratch save, open the developer console and type `core_savedata_check`. The result goes to `BepInEx\logs\ObenseuerCore.log`, as `game save data self-check ok` or the first failing case. If the console is not available at start, the framework logs `core_savedata_check not registered, the console is not available`. The default is `false`, so nothing is added.

What the test does, step by step:

1. It captures the `Inventory` entry, applies it, and captures again. The two texts must be equal.
2. It applies the entry by GUID.
3. It checks that a `Transform` (not savable), an entry with another GUID, and broken JSON are refused and change nothing.
4. It checks the limits: a 10,000-deep `[` string, the comment inputs `[/*"*/` and `[/*` (with 20,000 `]` before `*/`), each followed by 10,000 `[`, and a 257-deep entry through `Apply` are refused with no crash and no change. A 256-deep entry, a slash inside a string and the captured entry are accepted. A `]` with no opener is refused. It also checks the allowlist overload: an empty list applies nothing, and a listed GUID applies.
5. It applies the captured entry with a root `$type` of `UnityEngine.GameObject`, which is overwritten, and the inventory is unchanged. It checks that a nested `UnityEngine.GameObject` `$type`, a nested `Type` and `Data` pair, a nested unknown type, and a nested `UnityEngine.GameObject[]`, `List<GameObject>` and `BehaviorDesigner.Runtime.SharedGameObjectList` `$type` are refused with no change. It fails if the game cannot resolve any of the last three names, because then the refusal would not test the array, generic and base type checks.
