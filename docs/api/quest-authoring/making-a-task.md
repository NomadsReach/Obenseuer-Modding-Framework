---
title: Making a task
parent: Quest authoring
grand_parent: Story
nav_order: 3
---

# Making a task

```csharp
public enum QuestRefusal
{
    None,               // nothing was refused
    NotReady,           // the game is not ready: not in a game scene, or the task panel or notifications are not loaded
    SaveBusy,           // a save is loading or being written
    BadInput,           // the key, title, text, objectives or counters you gave are not allowed
    DuplicateTitle,     // another task already has this title
    AlreadyExists,      // the save already has a task with this id
    Rejected,           // the game refused the first objective, so no task was made
    Failed,             // a game call threw an error before the task was made
    PartiallyCreated    // the task was made, but a later objective was refused
}

public sealed class QuestObjectiveDefinition    // one step of a task
{
    public string Key { get; set; }           // the step's id inside the task
    public string Title { get; set; }         // the text the player sees
    public string Description { get; set; }   // more text; can be null
    public int Target { get; set; }           // the counter target; 0 for no counter, at most 9999
    public bool Optional { get; set; }        // true when the player does not have to do this step
}

public sealed class QuestDefinition    // the task you want to make
{
    public string Key { get; set; }           // the task's key; your mod id is put in front of it
    public string Title { get; set; }         // up to 100 characters
    public string Description { get; set; }   // up to 1000 characters; can be null
    public List<QuestObjectiveDefinition> Objectives { get; set; }   // 1 to 12 steps
}

public static class QuestAuthoring
{
    public static bool IsAvailable { get; }    // false when quest authoring is switched off

    public static string TaskId(string modId, string key);   // the task id for this mod and key; null when either is not allowed
    public static string Create(string modId, QuestDefinition definition, out QuestRefusal refusal);   // makes the task; returns its id, or null with the reason in refusal
}
```

- **`Create` shows the player the "New Task" notice and adds the task to the task panel.** Every objective in your definition starts active. A task has 1 to 12 objectives.
  - Technical: `Create` runs the game's own create path, `TaskController.EditTask` with `createNewTask`. It queues `Quests.TaskChanged` and `Quests.ObjectiveChanged`.
- **`Create` returns the task id, or `null` with the reason in `refusal`.** A refusal for a bad input or a conflict is logged once for each reason.
- **`AlreadyExists` means the save already has a task with that id.** For example from an earlier session. Call `Quests.Get` first and create the task only when it returns `null`.
- **`Rejected` means the game refused the first objective and no task was made.** `Failed` means a game call threw an error before the task was made.
- **`PartiallyCreated` is the one refusal that still comes with a task id.** The task was made, but the game refused or threw on a later objective, so the task has fewer objectives than your definition. Add the missing ones with `AddObjective`, or finish or fail the task.
- **`Create` refuses a title that another task already has.** Two tasks with one title are confusing in the task panel. The framework checks the tasks that have been started and the task files that are loaded (`DuplicateTitle`). A game task that is not loaded and not started can't be seen, so a clash with it is still possible. It only shows as two equal titles in the panel.
- **The game matches a saved task to a task by title, and the framework changes that for tasks made here.** Without the change, a game task with the same title could join your task, or yours could take over a game task's objectives. For a task made here, the game now matches by id only. If this change cannot be made, quest authoring is switched off.
  - Technical: the game finds a started task again with `TaskInfo.Compare`, which falls back to the title. The framework patches `Compare` so a task with an id of the form `modId.key` and no addressable path is compared by id only, against game tasks and other tasks alike. That includes the game's "new task" check and the task's status.
- **Writes are refused during a save or a load.** They are also refused while one of the game's quest hooks is running, and when the task panel or notifications are not loaded, which is outside a game scene.
