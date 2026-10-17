---
title: What the trace shows
parent: Debug
grand_parent: Framework basics
nav_order: 3
---

# What the trace shows

- **It shows the calls your mod makes to the framework.** Public methods and setters of the framework's public types are traced. Getters are only traced on static classes. Types whose names end in `Info` or `Args`, and methods that take a type parameter (generic methods), are skipped.
- **It shows the framework's own changes to the game.** Each time the framework changes a game method so it can tell your mod about something, the trace notes it. Changes made after the trace started are traced too, and so are the framework's helper methods for them. Changes made by other mods are not traced.
  - Technical: this covers the framework's `CorePatcher` handlers, including ones installed after the trace started, and the static methods of types named `*Patches`. Handlers from other mods are not traced.
- **It shows the framework's event and save plumbing.** The internal `Events` and `SaveData` methods and the flags for saving and loading are traced.
- **Each call is one line.** The line reads `Type.Method(args) -> result (0.123 ms)`. A call made inside another call is indented two spaces for each level. A call that throws an error is written as `!! Type.Method threw ...`.
  - Technical: a call from a public API at depth 0 ends with `[from X]`, where X is the assembly that made the call. A call from another thread starts with `[thread N]`.
- **Only the first 20 calls of each method are written in full.** After that the trace writes one summary line for each method every 10 seconds, with how many calls there were and the last call. This stops the log from filling with the same line.
- **The trace also writes lines for the framework's own events.** These are each change the framework installs in a game method (which game method, what kind of change and whether it worked), every changed method when the trace starts, the reason a feature group was turned off when the failure gives one, scene loads, and the plugins that depend on the framework. At each save and load, the trace writes a snapshot: the scene, the game time, the player position and the saving and loading flags.
- **While the trace is on, every error is logged.** Normally the framework logs only the first failure of each member. With the trace on it logs every failure.
- **The trace writes at most 1000 lines a second.** Lines over the limit are counted and summarised in one line.
