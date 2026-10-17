---
title: Testing your tasks
parent: Quest authoring
grand_parent: Story
nav_order: 6
---

# Testing your tasks

- **The config entry `Debug.QuestAuthoringSelfCheck` logs a check of the definition rules and the ownership rule at start.** Set it in the framework's config.
- **The config entry `Debug.QuestAuthoringCommand` adds the console command `core_quests_check`.** It makes, advances and finishes a test task in the loaded save and logs the result. It adds a finished task to that save, so use a scratch save.
