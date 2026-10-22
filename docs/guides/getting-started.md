---
title: Getting started
has_children: true
nav_order: 3
---

# Getting started

This section takes you from nothing to a mod you can share. You do not need to have made a mod before. Each page covers one job.

Install the game's mod tools and the framework first, using the [install page](../install.md). Then start with [Your first mod](getting-started/first-mod.md). It builds a small mod and shows a hint box and a log line in about five minutes. Every later page uses the same project, so keep it. To try a page's code, replace the code in `Plugin.cs` with the code on that page and build again. The build writes over the same DLL, so you do not get duplicate mods.

Read the pages in this order the first time:

1. [Your first mod](getting-started/first-mod.md): make, build and run a mod.
2. [Logging](getting-started/logging.md): write notes to your mod's log file.
3. [Config](getting-started/config.md): add settings players can change.
4. [Keybinds](getting-started/keybinds.md): react to a key.
5. [Using events](getting-started/using-events.md): react to the game clock, saves and levels.
6. [Save data](getting-started/save-data.md): remember things per save game.
7. [Patching](getting-started/patching.md): change a game method, as a last resort.
8. [Packaging](getting-started/packaging.md): make a zip to share.
9. [When things run](getting-started/startup-order.md): the order of the framework's calls, and what is safe at each one.

After that, the [Cookbook](cookbook/index.md) has short recipes for common jobs, such as giving an item or paying the player. When your mod works, read [Using the framework alongside other mods](multiple-mods.md) before you share it.
