---
title: Checking the trace itself
parent: Debug
grand_parent: Framework basics
nav_order: 6
---

# Checking the trace itself

- `[Debug] DebugTraceSelfCheck` makes the framework test its own trace code at start. It runs the line formatter, the rate limiter and the filter once and writes whether they passed to the log. Most modders never need it.
- The offline harness runs the same engine without the game. It prints one PASS or FAIL line for each check, and it exits with code 1 if any check fails.
- It reads `0Harmony.dll` from `BepInExCore`, which defaults to `GameDir\BepInEx\core`. Pass `-p:BepInExCore=<folder>` to use another folder.

To run it, use this command from the framework's source folder:

```
dotnet run --project tools/DebugTraceHarness -c Release
```
