---
title: Changing a task
parent: Quest authoring
grand_parent: Story
nav_order: 4
---

# Changing a task

These calls change a task your mod made.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>QuestAuthoring</code> <span>changing a running task</span></p>

Use these calls to add a step to a running task, change a step or its counter, or end the task.

<div class="api-pair" markdown="1">

```csharp
public static class QuestAuthoring
{
    public static bool AddObjective(string modId, string taskId, QuestObjectiveDefinition objective);
    public static bool SetObjective(string modId, string taskId, string objectiveKey, ObjectiveState state);
    public static bool SetCounter(string modId, string taskId, string objectiveKey, int amount);
    public static bool Finish(string modId, string taskId);
    public static bool Fail(string modId, string taskId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `AddObjective(modId, taskId, objective)` | `bool` | Adds a step to a running task. |
| `SetObjective(modId, taskId, objectiveKey, state)` | `bool` | Marks one step failed, finished or abandoned. |
| `SetCounter(modId, taskId, objectiveKey, amount)` | `bool` | Sets a step's counter to an exact number. |
| `Finish(modId, taskId)` | `bool` | Ends the task as finished. |
| `Fail(modId, taskId)` | `bool` | Ends the task as failed. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>QuestAuthoring</code> <span>hand in, reveal and rewind</span></p>

Use these calls for a step that asks for items, a hidden step, and the calls that send a task of yours back to its start or take it out.

<div class="api-pair" markdown="1">

```csharp
public static class QuestAuthoring
{
    public static bool HandIn(string modId, string taskId, string objectiveKey);
    public static bool Reveal(string modId, string taskId, string key, string title, string description);
    public static bool SetTitle(string modId, string taskId, string title);
    public static bool SetTitle(string modId, string taskId, string objectiveKey, string title);
    public static bool SetDescription(string modId, string taskId, string description);
    public static bool Restart(string modId, string taskId);
    public static bool Reset(string modId, string taskId);
    public static bool Remove(string modId, string taskId);
    public static bool JumpTo(string modId, string taskId, string objectiveKey);
    public static bool Checkpoint(string modId, string taskId, string name, bool restore);
    public static bool Checkpoint(string modId, string taskId, string name, Action restoreCallback);
}
```

| Method | Returns | What it does |
|---|---|---|
| `HandIn(modId, taskId, objectiveKey)` | `bool` | Takes the items a step asks for, then moves the step on. |
| `Reveal(modId, taskId, key, title, description)` | `bool` | Writes the real text of a hidden step and makes it active. |
| `SetTitle(modId, taskId, title)` | `bool` | Changes the title of the task. |
| `SetTitle(modId, taskId, objectiveKey, title)` | `bool` | Changes the title of one step. |
| `SetDescription(modId, taskId, description)` | `bool` | Changes the description of the task. |
| `Restart(modId, taskId)` | `bool` | Sends a finished, failed or abandoned task back to its start. |
| `Reset(modId, taskId)` | `bool` | Sends a task back to its start from any state, including active. |
| `Remove(modId, taskId)` | `bool` | Takes the task out of the task panel and the live list. |
| `JumpTo(modId, taskId, objectiveKey)` | `bool` | Makes one step the active step and sets the steps before it as finished. |
| `Checkpoint(modId, taskId, name, restore)` | `bool` | Keeps a named copy of the task's state, or puts a kept copy back. |
| `Checkpoint(modId, taskId, name, restoreCallback)` | `bool` | Keeps a named copy and runs the callback when that copy is put back. |

</div>
</div>

## Things to know

- `HandIn` needs an active step that asks for items and the player holding all of them. It gives the items back when the move fails. See [Handing in items](../quest-authoring.md#handing-in-items).
- `Reveal` shows the real title and text of a step that was created hidden. It needs an active task and a hidden step.
- `Restart`, `Reset`, `JumpTo`, `Remove`, `Checkpoint` and the `SetTitle` and `SetDescription` calls work only on a task of yours in the live task list, and not while a save or a load runs. `Restart` returns `false` while the task is still active. `Reset` works from any state.
- `Checkpoint` keeps up to 16 named copies for each task, and a name is 1 to 32 letters, digits or `-`. A restore needs a name that was kept first. Copies are lost when the game session ends.
- A copy kept with `restoreCallback` runs that callback once when the copy is put back, after the state is back and after `Quests.Rewound`. An exception in it is logged once and swallowed. Keeping a plain copy of the same name clears the callback.
- These calls raise `Quests.Rewound` with the task id and a reason, so a mod can react to them. See [Rewinding a quest](../quest-authoring.md#rewinding-a-quest).

## Things to know

- `AddObjective` adds an active step to a running task of yours. It returns `false` when the key is already used in that task or the task already has 12 objectives.
- `SetObjective` accepts `Failed`, `Finished` and `Abandoned` for an active objective. It works the same as `Quests.SetObjective`.
- A step with a `Target` above 0 has a counter. When the counter reaches the target, the game finishes the step. `SetCounter` sets the counter to an exact number from 0 to the target. For a change by a relative amount, use `Quests.AddProgress`.
- `SetCounter` returns `true` when the counter changed. A number outside 0 to the target is refused. Setting the target finishes the objective.
- `Finish` and `Fail` need a task that is still active. The game marks every step that is still active as abandoned.
- `Finish` does not start the follow-up tasks (see `Quests.SetTask`). To abandon a task, use `Quests.SetTask` with `Abandoned`.

## Limits

- Every change returns `false` when it is refused. That is for another mod's task or a game task, during a save or a load, and when quest authoring is switched off.
