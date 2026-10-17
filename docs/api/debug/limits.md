---
title: Limits
parent: Debug
grand_parent: Framework basics
nav_order: 4
---

# Limits

- **Some calls are missing from the trace.** A call the game's compiler folded into its caller before the trace started is not traced. A caller that ends by handing over to another method, called a tail call, disappears from the call stack, so its `[from X]` can be missing.
  - Technical: the first is a call the JIT inlined into a caller compiled before the trace started.
- **The values a call changed through `ref` or `out` are not shown.** The trace shows the arguments as they were when the call began.
- **Generic methods are not traced.**
- **Reading the loading flag in a snapshot can clear its pending flag.** This is the same as the framework's own reads do, so the snapshot does not change what your mod sees.
- **A line is written when its call ends.** So the calls nested inside a call print before the call that contains them.
- **The log file is cut at 4 MB.** When it reaches 4 MB the framework moves it to `ObenseuerCore.debug.log.1` and starts a new one. At the 1000 lines a second cap, 4 MB lasts about 40 seconds if lines average about 100 bytes. Longer lines fill it sooner. The old `.1` file is replaced each time.
