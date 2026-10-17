---
title: Details
parent: Breakables
grand_parent: Objects and devices
nav_order: 5
---

# Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`.

Everything not listed in this reference is internal and may change without notice.

The framework changes some of the game's methods when it starts, so it can tell your mod when something breaks. This page calls each group of those changes a feature group. Each group has a name that shows up in the log. The page's own group is `obenseuer.moddingframework.breakables`, and it is split into four smaller groups: `.break` (the `Broken` event), `.repair` (the `Repaired` event), `.lock` (the `LockBroken` event) and `.smash` (the `Smashed` event).

## When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)). That includes the code that handles every event.
- It is safe to call any of it at any time, even on the main menu or while a save is loading. If there is nothing to read yet, you get an empty list, `null` or `false`.
- Nothing here throws an error to your code. An id that is unknown, null or empty gives `null` or `false`. A change that cannot be made gives `false`.
- `Break`, `Repair` and `BreakLock` do nothing while a save is loading or being written. They return `false`. Try again later, for example from a timer. <span class="tech">(They check `SaveGate.Busy`.)</span>
- Your event handlers run inside the game's own call, one at a time. If one handler throws an error, the framework logs it and still runs the other handlers. Do not change scenes or break more objects from inside a handler. Wait until the game has finished its work, for example with a timer. <span class="tech">(`Broken`, `Repaired`, `LockBroken` and `Smashed` are raised synchronously from a Harmony postfix on the main thread.)</span>
- No event fires while a save is loading. That includes a repair timer that expired while the game was closed. <span class="tech">(The events check `SaveController.Loading`.)</span>

## If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- Each event depends on its own small feature group. `Broken` needs `obenseuer.moddingframework.breakables.break`, `Repaired` needs `.repair`, `LockBroken` needs `.lock` and `Smashed` needs `.smash`. If one cannot be set up, only that group is turned off, `Breakables.IsAvailable` becomes `false`, and the framework writes error lines and a warning to the log. The other events and all the calls keep working: `All`, `Get`, `IsBroken`, `Break`, `Repair`, `Locks` and `BreakLock`. <span class="tech">(The game methods involved are `Breakable.Break` (break), `Breakable.Repair` and `Breakable.OnLoadingGame` (repair), `BreakableLock.Break` (lock) and `SpawnItemOnBreak.Damage` (smash). The log line reads "breakable hooks incomplete, IsAvailable is false, the events whose hooks are missing are disabled".)</span>
