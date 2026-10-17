---
title: Details
parent: Framework core classes
grand_parent: Framework basics
nav_order: 17
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Each part writes to its own log in `BepInEx\logs`. The framework's own lines go to `ObenseuerCore.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes some of the game's methods when it starts, so it can tell your mod when things happen. This page calls each group of those changes a feature group. Each feature group has a name that shows up in the log. This page uses these: `obenseuer.moddingframework.survival` (and the parts `.consumables`, `.needs`, `.death`, `.damage`, `.breakdown` and `.depression`), `obenseuer.moddingframework.tweaks`, `obenseuer.moddingframework.ui`, `obenseuer.moddingframework.computerapps` and `obenseuer.moddingframework.playerlook`.

## When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** That means `OnLoad`, `Awake`, `Start`, `Update` and your event handlers. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- **Your event handlers run inside the game's own call, one at a time.** If one throws an error, the framework logs it and still runs the other handlers.
- **Calls that change the game are refused while a save is loading or saving.** You get `false`, or nothing happens. Try again later, for example from `Events.Loaded` or `Events.WhenReady`.

## What to check for and what comes back

- **Check `IsAvailable` when you need to know whether a feature works.** `Survival.IsAvailable`, `ComputerApps.IsAvailable`, `PlayerLook.IsAvailable` and `CameraView.IsAvailable` are `false` when the feature could not be set up, or when its feature group was turned off.
- **Bad arguments throw, and the exceptions are listed in each section.** An empty or null mod id throws `ArgumentException`. A null callback or handler throws `ArgumentNullException`.
- **If the game itself fails while the framework reads, you get a plain default instead of an error.** That is `0`, `false`, `null` or an empty list, and the failure is written to the log.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **`Survival.IsAvailable` is `false` until its changes are made, and stays `false` if they fail or are refused.** It is also `false` when `Core.PatchingAllowed` is false, or when the feature group is disabled. Calls that need a missing change return their default (`0`, `false` or nothing happens).
- **A call that needs the change gives you `false` while an unrelated one keeps working.** For example, if the damage change fails, `InterceptDamage` returns `false`, but `Get` and `Set` still work.
