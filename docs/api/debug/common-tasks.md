---
title: Common tasks
parent: Debug
grand_parent: Framework basics
nav_order: 1
---

# Common tasks

Check whether the trace is on, and which framework methods are called the most:

```
core_debug status
```

Turn on the trace only for the parts you care about. Open the Mods menu, find `[Debug] TraceFilter` and enter type names separated by commas, for example `PlayerState,GameTime`. Only types whose names start with one of those words are traced.

Keep the trace on across restarts. Open the Mods menu and switch `[Debug] Master` on. The setting is saved, so the trace stays on the next time you start the game. Switch it off again when you are done.
