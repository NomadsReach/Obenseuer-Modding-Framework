---
title: Save manifest
parent: Registry
grand_parent: Items and crafting
nav_order: 7
---

# Save manifest

The framework keeps a list of the items and recipes that mods have added, in each save, so it can tell which ones are missing when a mod is removed. You never call it.

- **Each save writes a list of your mod's items and recipes.** The list also holds every entry from the loaded save whose mod is not registered now (carried forward). When a save loads, each saved entry that is not registered in this session writes a warning to the log. It names the title, the mod, and the call that clears it. It appears once per session for each entry, so changing maps does not repeat it. The warning comes after the game has loaded the save. The stacks already show a placeholder by then (see [Missing items](missing-items.md#missing-items)).
  - Technical: the file is `mods/obenseuer.moddingframework.registry.json` in the save folder. An entry holds `Id`, `Mod`, `Key` and `Title`.
- **Entries of a missing mod are carried forward.** The entries of the last loaded save that are not registered now are written into every save made from that load, including a save under a new name. They are dropped when the player leaves to the main menu and starts or loads another save, and when the mod is back (the entry is then written as a registered one). A key that a mod deliberately removes stays in the list, so the warning stays.
- **`ItemRegistry.RemoveMissingItems` drops the carried entries of that mod at once.** That means its recipe entries, and each item entry whose id no scanned stack holds after the clear.
- **The list is the framework's save format, not part of the API.** Mods never call it.
