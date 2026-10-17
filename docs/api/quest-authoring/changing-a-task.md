---
title: Changing a task
parent: Quest authoring
grand_parent: Story
nav_order: 4
---

# Changing a task

```csharp
public static class QuestAuthoring
{
    public static bool AddObjective(string modId, string taskId, QuestObjectiveDefinition objective);                 // adds a step to a running task
    public static bool SetObjective(string modId, string taskId, string objectiveKey, ObjectiveState state);          // marks one step failed, finished or abandoned
    public static bool SetCounter(string modId, string taskId, string objectiveKey, int amount);                       // sets a step's counter to an exact number
    public static bool Finish(string modId, string taskId);   // ends the task as finished
    public static bool Fail(string modId, string taskId);     // ends the task as failed
}
```

- **`AddObjective` adds an active step to a running task of yours.** It returns `false` when the key is already used in that task or the task already has 12 objectives.
- **`SetObjective` accepts `Failed`, `Finished` and `Abandoned` for an active objective.** It works the same as `Quests.SetObjective`.
- **A step with a `Target` above 0 has a counter.** When the counter reaches the target, the game finishes the step. `SetCounter` sets the counter to an exact number from 0 to the target. For a change by a relative amount, use `Quests.AddProgress`.
- **`SetCounter` returns `true` when the counter changed.** A number outside 0 to the target is refused. Setting the target finishes the objective.
- **`Finish` and `Fail` need a task that is still active.** The game marks every step that is still active as abandoned. `Finish` does not start the follow-up tasks (see `Quests.SetTask`). To abandon a task, use `Quests.SetTask` with `Abandoned`.
- **Every change returns `false` when it is refused.** That is for another mod's task or a game task, during a save or a load, and when quest authoring is switched off.
