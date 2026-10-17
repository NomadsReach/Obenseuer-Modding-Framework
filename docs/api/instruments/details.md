---
title: Details
parent: Instruments
grand_parent: Objects and devices
nav_order: 6
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.instruments.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts, so it can tell your mod when a strum or a comment happens. This page calls that a feature group: its name is `obenseuer.moddingframework.instruments`, the name you will see in the log.

## When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)). This goes for the code that handles the events too.
- **Reading is safe at any time, even on the main menu or while a save is loading.** With no scene to look in, `All()` gives an empty list and `Get` gives `null`.
- **The calls that make a guitar sound are refused while a save is loading or being written, and while the player cannot use items.** A song's strum that falls in that time is skipped.

## What to check for and what comes back

- **`Instruments.IsAvailable` tells you whether the events work.** It is `false` when the framework could not set up the events, or when the feature group was turned off. `PlayNote`, `PlaySong` and the reads keep working.
- **A mod id that is not valid throws an `ArgumentException`.** A mod id is valid when it is 1 to 64 letters, digits, dots, underscores or dashes. `Mod.Id` always is.

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the framework cannot change one of the game's guitar methods, the whole feature group turns off.** The events stop, error lines and a warning go to the log, and the rest of the API keeps working.
  - Technical: the framework changes four things: `WieldableGuitar.onPrimaryUse` (before and after), `NPCRandomTalk.OnGuitarPlayed` (after), `WieldableGuitar.Update` (before and after), and `CharacterBarkStarter.TryToComment` (after).
