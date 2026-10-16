---
title: Debug
parent: API reference
nav_order: 113
---

# Debug

Experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).

The framework can trace what it does and write the trace to `BepInEx\logs\ObenseuerCore.debug.log`. Leave it off for normal play. It slows the game.

## Turn it on

- Mods menu: `[Debug] Master`, default off. The change applies at once.
- Console: `core_debug on`, `core_debug off`, `core_debug status`. Status prints on or off, the number of patched methods, and the 10 most-called methods.
- The setting is saved in the framework config, so it stays on after a restart. Turn it off when you are done.

## What the trace shows

- Public methods and setters of public framework types. Getters are traced only on static classes. `*Info` and `*Args` types and generic members are skipped.
- Framework CorePatcher handlers, including ones installed after the trace started, and the static methods of `*Patches` types. Handlers from other mods are not traced.
- Internal `Events` and `SaveData` methods, and the saving and loading flags.
- One line per call: `Type.Method(args) -> result (0.123 ms)`. Nested calls are indented two spaces per level. A call that throws is written as `!! Type.Method threw ...`. A call from a public API at depth 0 ends with `[from X]`, where X is the calling assembly. A call from another thread starts with `[thread N]`.
- The first 20 calls of each method are written in full. After that, each method gets one summary line every 10 seconds with the count and the last call.
- Framework events: each CorePatcher install (owner, target, patch kinds, result), every patched method at enable, `DisableOwner` errors (the reason is shown when the failure gives one), scene loads, save and load snapshots (scene, game time, player position, saving and loading flags), and the plugins that depend on the framework.
- While the switch is on, every error is logged, and the framework logs every failure instead of only the first. Debug lines are limited to 1000 per second. Lines over the limit are counted and summarised.

## TraceFilter

`[Debug] TraceFilter` is a comma-separated list of type name prefixes, for example `PlayerState,GameTime`. Empty traces every target. Changing it while the switch is on restarts the trace.

## Limits

- A call the JIT inlined into a caller that was compiled before the trace started is not traced.
- A caller that ends in a tail call disappears from the stack, so its `[from X]` can be missing.
- Arguments are shown as they were on entry. The values a `ref` or `out` parameter holds when the call returns are not shown.
- Generic methods are not traced.
- Reading the loading flag in a snapshot can clear its pending flag, the same as the framework's own reads do.
- Each line is written when its call returns, so the calls nested inside a call print before it.
- Only the framework's own handlers are traced. This includes handlers installed after the trace started, and excludes handlers from other mods.
- The debug log moves to `ObenseuerCore.debug.log.1` at 4 MB. At the 1000 lines per second cap, 4 MB lasts about 40 seconds if lines average about 100 bytes. Longer lines fill it sooner.

## Self-check

`[Debug] DebugTraceSelfCheck` runs the formatter, limiter and filter self-check at start and logs the result.

## Offline harness

`tools/DebugTraceHarness` runs the trace engine against HarmonyX without the game:

```
dotnet run --project tools/DebugTraceHarness -c Release
```

It prints one PASS or FAIL line per check, and exits 1 if any check fails. It reads `0Harmony.dll` from `BepInExCore`, which defaults to `GameDir\BepInEx\core`. Pass `-p:BepInExCore=<folder>` to use another folder.

## Log file

`ObenseuerCore.debug.log` is moved to `ObenseuerCore.debug.log.1` when the log is first opened in a game session, and again each time it reaches 4 MB. Each line has the frame number, the time and the level.
