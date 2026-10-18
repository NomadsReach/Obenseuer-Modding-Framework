---
title: Debug
parent: Framework basics
grand_parent: API reference
has_children: true
nav_order: 2
---

# Debug

Status: experimental.

## What it is

The framework can keep a diary of what it does while the game runs. This is the debug trace. When you turn it on, the framework writes one line to a log file for each framework call it makes. It also logs each change it has made to a game method, each event it raises, and each save and load. Each line says what was called, what came back and how long it took. You do not write any code to use it. It is a switch.

The trace slows the game down, so leave it off for normal play.

## When to use it

- Work out why one of your calls returned `false` or did nothing.
- Check which framework events fire, and in what order, when something happens in the game.
- Collect a log to send with a bug report, so the cause can be found without guessing.
- See which framework changes to the game turned on at start, and whether a game update broke one.

## Quick example

This turns the trace on, reproduces a problem and turns the trace off again. You need no code and no build.

1. Start the game and load a save.
2. Open the game's developer console and type `core_debug on`.
3. Do the thing that goes wrong in the game.
4. Type `core_debug off`.
5. Open `BepInEx\logs\ObenseuerCore.debug.log` in the game folder and read the lines from the time you did the thing.

The console prints a line such as `debug on, 120 methods patched` when you turn it on, so you know the trace is running. In the log, each framework call your mod makes shows up as a line such as `Type.Method(args) -> result (0.123 ms)`.
