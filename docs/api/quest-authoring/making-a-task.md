---
title: Making a task
parent: Quest authoring
grand_parent: Story
nav_order: 3
---

# Making a task

`QuestAuthoring.Create` makes a new task from a definition you build, and tells you why when it refuses.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>QuestAuthoring</code> <span>making a task</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class QuestAuthoring
{
    public static bool IsAvailable { get; }
    public static string TaskId(string modId, string key);
    public static string Create(string modId, QuestDefinition definition, out QuestRefusal refusal);
}
```

| Method | Returns | What it does |
|---|---|---|
| `TaskId(string modId, string key)` | `string` | The task id for this mod and key. It is `null` when either is not allowed. |
| `Create(string modId, QuestDefinition definition, out QuestRefusal refusal)` | `string` | Makes the task. It returns its id, or `null` with the reason in `refusal`. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when quest authoring is switched off. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>QuestDefinition</code> <span>the task you want to make</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class QuestDefinition
{
    public string Key { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<QuestObjectiveDefinition> Objectives { get; set; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Key` | `string` | The task's key. Your mod id is put in front of it. |
| `Title` | `string` | Up to 100 characters. |
| `Description` | `string` | Up to 1000 characters. Can be `null`. |
| `Objectives` | `List<QuestObjectiveDefinition>` | 1 to 12 steps. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>QuestObjectiveDefinition</code> <span>one step of a task</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class QuestObjectiveDefinition
{
    public string Key { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int Target { get; set; }
    public bool Optional { get; set; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Key` | `string` | The step's id inside the task. |
| `Title` | `string` | The text the player sees. |
| `Description` | `string` | More text. Can be `null`. |
| `Target` | `int` | The counter target. Use 0 for no counter, and at most 9999. |
| `Optional` | `bool` | `true` when the player does not have to do this step. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>QuestRefusal</code> <span>why a task was not made</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum QuestRefusal
{
    None,
    NotReady,
    SaveBusy,
    BadInput,
    DuplicateTitle,
    AlreadyExists,
    Rejected,
    Failed,
    PartiallyCreated
}
```

| Value | Meaning |
|---|---|
| `None` | Nothing was refused. |
| `NotReady` | The game is not ready: not in a game scene, or the task panel or notifications are not loaded. |
| `SaveBusy` | A save is loading or being written. |
| `BadInput` | The key, title, text, objectives or counters you gave are not allowed. |
| `DuplicateTitle` | Another task already has this title. |
| `AlreadyExists` | The save already has a task with this id. |
| `Rejected` | The game refused the first objective, so no task was made. |
| `Failed` | A game call threw an error before the task was made. |
| `PartiallyCreated` | The task was made, but a later objective was refused. |

</div>
</div>

## Things to know

- `Create` shows the player the "New Task" notice and adds the task to the task panel. Every objective in your definition starts active. A task has 1 to 12 objectives. <span class="tech">(`Create` runs the game's own create path, `TaskController.EditTask` with `createNewTask`. It queues `Quests.TaskChanged` and `Quests.ObjectiveChanged`.)</span>
- `Create` returns the task id, or `null` with the reason in `refusal`. A refusal for a bad input or a conflict is logged once for each reason.
- `AlreadyExists` means the save already has a task with that id. For example from an earlier session. Call `Quests.Get` first and create the task only when it returns `null`.
- `Rejected` means the game refused the first objective and no task was made. `Failed` means a game call threw an error before the task was made.
- `Create` refuses a title that another task already has, which the framework checks against the tasks that have been started and the task files that are loaded (`DuplicateTitle`). Two tasks with one title are confusing in the task panel.
- The game matches a saved task to a task by title, and the framework changes that for tasks made here. Without the change, a game task with the same title could join your task, or yours could take over a game task's objectives. For a task made here, the game now matches by id only. If this change cannot be made, quest authoring is switched off. <span class="tech">(The game finds a started task again with `TaskInfo.Compare`, which falls back to the title. The framework patches `Compare` so a task with an id of the form `modId.key` and no addressable path is compared by id only, against game tasks and other tasks alike. That includes the game's "new task" check and the task's status.)</span>

## Limits

- `PartiallyCreated` is the one refusal that still comes with a task id. The task was made, but the game refused or threw on a later objective, so the task has fewer objectives than your definition. Add the missing ones with `AddObjective`, or finish or fail the task.
- A game task that is not loaded and not started can't be seen by the duplicate check, so a clash with it is still possible. It only shows as two equal titles in the panel.
- Writes are refused during a save or a load. They are also refused while one of the game's quest hooks is running, and when the task panel or notifications are not loaded, which is outside a game scene.
