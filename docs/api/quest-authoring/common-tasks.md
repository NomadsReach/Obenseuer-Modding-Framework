---
title: Common tasks
parent: Quest authoring
grand_parent: Story
nav_order: 1
---

# Common tasks

Every example on this page is a whole mod. Each one makes its own quest the first time the game is ready, and then changes it when you press a key. Replace the mod id and the keys with your own. Every call that can refuse is checked, and the warning says what was refused.

Count progress on an objective, then finish the task. The counter of `search` reaches its target of 3, which finishes that objective, and `Finish` ends the task:

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.countfinish", "Count And Finish", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class CountAndFinish : FrameworkMod
{
    private const string Key = "count_demo";
    private ModKeybind finishKey;

    protected override void OnLoad()
    {
        finishKey = Mod.Key("Finish", "Finish the count demo", KeyCode.F6);
        Quests.Ready += Make;
    }

    private void Make()
    {
        if (Quests.Get(QuestAuthoring.TaskId(Mod.Id, Key)) != null) return;
        QuestDefinition quest = new QuestDefinition { Key = Key, Title = "Count demo", Description = "Search three rooms." };
        quest.Objectives.Add(new QuestObjectiveDefinition { Key = "search", Title = "Search the rooms", Target = 3 });
        QuestRefusal refusal;
        if (QuestAuthoring.Create(Mod.Id, quest, out refusal) == null) Log.Warn("The quest was refused: " + refusal);
    }

    private void Update()
    {
        if (UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (finishKey == null || !finishKey.Down()) return;
        string id = QuestAuthoring.TaskId(Mod.Id, Key);
        if (!QuestAuthoring.SetCounter(Mod.Id, id, "search", 3)) Log.Warn("The counter was not set");
        if (!QuestAuthoring.Finish(Mod.Id, id)) Log.Warn("The quest was not finished, it may not be active");
    }
}
```

Add another step to a task that is still running. The key adds the step `lock` to the task, and the step is active from the moment it is added:

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.stepdemo", "Step Demo", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class StepDemo : FrameworkMod
{
    private const string Key = "step_demo";
    private ModKeybind addKey;

    protected override void OnLoad()
    {
        addKey = Mod.Key("AddStep", "Add a step to the demo", KeyCode.F7);
        Quests.Ready += Make;
    }

    private void Make()
    {
        if (Quests.Get(QuestAuthoring.TaskId(Mod.Id, Key)) != null) return;
        QuestDefinition quest = new QuestDefinition { Key = Key, Title = "Step demo", Description = "Add a step to this task." };
        quest.Objectives.Add(new QuestObjectiveDefinition { Key = "first", Title = "The first step" });
        QuestRefusal refusal;
        if (QuestAuthoring.Create(Mod.Id, quest, out refusal) == null) Log.Warn("The quest was refused: " + refusal);
    }

    private void Update()
    {
        if (UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (addKey == null || !addKey.Down()) return;
        QuestObjectiveDefinition extra = new QuestObjectiveDefinition { Key = "lock", Title = "Lock the door again" };
        if (!QuestAuthoring.AddObjective(Mod.Id, QuestAuthoring.TaskId(Mod.Id, Key), extra)) Log.Warn("The step was not added, it may already exist");
    }
}
```

Mark one objective as done, or end the task as failed. F8 marks the step `return` as finished, and F9 ends the task as failed:

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.faildemo", "Fail Demo", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class FailDemo : FrameworkMod
{
    private const string Key = "fail_demo";
    private ModKeybind doneKey;
    private ModKeybind failKey;

    protected override void OnLoad()
    {
        doneKey = Mod.Key("Done", "Mark the return step done", KeyCode.F8);
        failKey = Mod.Key("Fail", "Fail the demo", KeyCode.F9);
        Quests.Ready += Make;
    }

    private void Make()
    {
        if (Quests.Get(QuestAuthoring.TaskId(Mod.Id, Key)) != null) return;
        QuestDefinition quest = new QuestDefinition { Key = Key, Title = "Fail demo", Description = "Mark a step, then fail the task." };
        quest.Objectives.Add(new QuestObjectiveDefinition { Key = "return", Title = "Bring it back" });
        QuestRefusal refusal;
        if (QuestAuthoring.Create(Mod.Id, quest, out refusal) == null) Log.Warn("The quest was refused: " + refusal);
    }

    private void Update()
    {
        if (UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        string id = QuestAuthoring.TaskId(Mod.Id, Key);
        if (doneKey != null && doneKey.Down() && !QuestAuthoring.SetObjective(Mod.Id, id, "return", ObjectiveState.Finished)) Log.Warn("The step was not marked done");
        if (failKey != null && failKey.Down() && !QuestAuthoring.Fail(Mod.Id, id)) Log.Warn("The task was not failed, it may not be active");
    }
}
```
