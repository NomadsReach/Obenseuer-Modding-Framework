---
title: Quest authoring
parent: Story
grand_parent: API reference
has_children: true
nav_order: 2
---

# Quest authoring

Status: experimental.

## What it is

The game calls a quest a task. A task has a title, a description and a list of objectives, which are the steps the player works through. The player sees tasks in the task panel. This API lets your mod make a new task while the game runs, add objectives to it, move its counters and end it as finished or failed. A task made here is an ordinary game task, so the player sees it in the task panel, and `Quests.All`, `Quests.Get`, `Quests.TaskChanged` and `Quests.ObjectiveChanged` see it too.

Reading and changing the game's own tasks stays on the `Quests` page, in the [Story API](../api-story.md). You can only change the tasks your own mod made through `QuestAuthoring`.

## When to use it

- Give the player a new task from your mod, with a title, a description and steps.
- Count progress on a step, for example "find 3 keys", and finish the task when it is done.
- End a task as finished or failed from your own code.

## Quick example

This mod waits for a save to load. Then it makes a task called "The lost key" with two objectives, unless the save already has it.

```csharp
using ObenseuerCore;

[ModInfo("example.questy", "Quest Maker", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class QuestMaker : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(MakeQuest);
    }

    private void MakeQuest()
    {
        string id = QuestAuthoring.TaskId(Mod.Id, "lost_key");
        if (Quests.Get(id) != null) return;

        QuestDefinition quest = new QuestDefinition { Key = "lost_key", Title = "The lost key", Description = "Find the key." };
        quest.Objectives.Add(new QuestObjectiveDefinition { Key = "search", Title = "Search the rooms", Target = 3 });
        quest.Objectives.Add(new QuestObjectiveDefinition { Key = "return", Title = "Bring it back" });

        QuestRefusal refusal;
        if (QuestAuthoring.Create(Mod.Id, quest, out refusal) == null) Log.Warn("The quest was refused: " + refusal);
    }
}
```

The check `Quests.Get(id) != null` matters. The game saves the task, so on the next start the task is already in the save, and `Create` would be refused. `Mod.Id` is your mod's own id; it becomes the start of the task id. See [Using events](../guides/getting-started/using-events.md) for more on `WhenReady`.
