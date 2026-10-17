---
title: Ids
parent: Quest authoring
grand_parent: Story
nav_order: 2
---

# Ids

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

- **A task has a key you choose, and the task id is your mod id, a dot and the key.** The key is letters, digits, `_` and `-`, up to 48 characters, with no dot. Two mods can never share a task id, because the mod id comes first. `QuestAuthoring.TaskId(modId, key)` builds the id for you.
- **An objective id is its key, and it must be unique inside its task.** The game's scripts use these ids when they ask whether a task or objective is active.
  - Technical: the game's Lua functions are `TaskActive("<taskId>")`, `ObjectiveActive` and the others.
- **Only the mod whose id starts the task id can change the task through `QuestAuthoring`.** `AddObjective`, `SetObjective`, `SetCounter`, `Finish` and `Fail` return `false` for another mod's task or for one of the game's own. This works after a restart or a load, because the framework reads the mod from the id.
