---
title: Turning it on and off
parent: Debug
grand_parent: Framework basics
nav_order: 2
---

# Turning it on and off

This is the full list of what the trace does and what you can change. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

There are three ways to switch the trace, and they all change the same saved setting.

- **The Mods menu has a switch called `[Debug] Master`, and it is off by default.** The change applies at once, with no restart.
- **The console command `core_debug` turns it on or off and shows its state.** Use `core_debug on`, `core_debug off` or `core_debug status`. Any other word prints a short usage line.
  - Technical: `status` prints on or off, the number of game methods the trace is watching, and the 10 methods called most often.
- **The setting is saved in the framework's config file, so the trace stays on after a restart.** Turn it off when you are done, or every start of the game will be slow.
  - Technical: the file is `BepInEx\config\obenseuer.moddingframework.cfg`, section `Debug`.
- **`[Debug] TraceFilter` limits the trace to some types.** It is a list of type name beginnings separated by commas, for example `PlayerState,GameTime`. Leave it empty to trace everything. If you change it while the trace is on, the trace restarts with the new list.
