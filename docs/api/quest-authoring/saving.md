---
title: Saving
parent: Quest authoring
grand_parent: Story
nav_order: 5
---

# Saving

These notes cover what happens to your tasks in the save file.

## Things to know

- The framework writes no save file for tasks, and the game saves your task like any other. The task list is saved with each task and step written out in full: the title, the description, the step ids, the counters and targets, and each status, together with the id lists the game's scripts read. <span class="tech">(`TaskController` serializes its whole task list, with each `TaskInfo` and `TaskObjective` written inline.)</span>
- After a load, your calls keep working. The game's task list then holds copies of your task, not the ones you made. The framework keeps no task between calls. Every call looks the task up by its id in the game's current list and uses the game's own object. So your calls keep working after a save and a load, and after loading a different save. A task that is not in the loaded save is simply unknown (`Quests.Get` returns `null`), and `Create` can make it again. <span class="tech">(The lookup also checks that the game's `Compare` maps the entry to itself.)</span>

## Limits

- If your mod is removed, the task stays in the save and in the task panel. The framework can't remove it, because the game has no call for that. Without the framework, the change to the matching is gone too, so the game matches the leftover task by title again. A game task with the same title can then land on that entry, and its objectives join the leftover task. This holds even for a game task that was never loaded when you made your task.
- Give your tasks titles that no game task uses, and prefer a new key over reusing a title.
- The text of a task stays in the save, even when your mod is removed. To change a task's text for saves that already have it, make a new task with a new key.
