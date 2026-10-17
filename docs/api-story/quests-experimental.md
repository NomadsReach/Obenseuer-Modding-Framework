---
title: Quests (experimental)
parent: Quests, dialogue and law
grand_parent: Story
nav_order: 3
---

# Quests (experimental)

The game gives the player tasks, and each task has steps, called objectives. `Quests` lets your mod list them, check how far the player is, move a task or a step on, and add progress to a step that counts things (for example "collect 5 cans"). It tells you when a task or step changes.

```csharp
public enum TaskState        // where a task is
{
    Active = 0,      // the player is working on it
    Failed = 1,      // the player failed it
    Finished = 2,    // the player completed it
    Abandoned = 3    // the task was dropped
}

public enum ObjectiveState   // where one step of a task is
{
    Active = 0,      // the player is working on it
    Failed = 1,      // the player failed it
    Finished = 2,    // the player completed it
    Abandoned = 3,   // the step was dropped
    Hidden = 4       // the game has set the step to hidden
}

public sealed class QuestInfo    // a task
{
    public string Id { get; }                   // the task's id (see Ids below)
    public string Title { get; }                // the task's name
    public string Description { get; }          // the text the player reads
    public TaskState State { get; }             // where the task is
    public ObjectiveInfo[] Objectives { get; }  // the steps of the task
}

public sealed class ObjectiveInfo    // one step of a task
{
    public string Id { get; }                   // the step's id (see Ids below)
    public string Title { get; }                // the step's name
    public string Description { get; }          // the text the player reads
    public ObjectiveState State { get; }       // where the step is
    public bool Optional { get; }               // true when the step is not needed to finish the task
    public int Current { get; }                 // the counter now
    public int Target { get; }                  // the counter the step needs; 0 when the step has no counter
}

public static class Quests
{
    public static event Action<TaskChangedArgs> TaskChanged;           // a task started or changed state; args.TaskId, args.State
    public static event Action<ObjectiveChangedArgs> ObjectiveChanged; // a step started or changed state; args.TaskId, args.ObjectiveId, args.State
    public static bool IsAvailable { get; }                            // true when the Quests API works
    public static List<QuestInfo> All();                               // every task the player has started
    public static QuestInfo Get(string taskId);                        // one task; null when none matches
    public static bool Reached(string taskId, string objectiveId);     // true when the player has got to this step
    public static bool SetTask(string taskId, TaskState state);        // close a task; true when it has that state afterwards
    public static bool SetObjective(string taskId, string objectiveId, ObjectiveState state);  // close a step; true when it has that state afterwards
    public static bool AddProgress(string taskId, string objectiveId, int amount);             // add to a step's counter
}
```

**Ids.**

- **A task id is the game's own id for the task.** It is the task's `id` field. If that is empty it is the task's addressable asset path, and if that is empty too it is the task's title. A step id works the same way. `QuestInfo.Id` and `ObjectiveInfo.Id` give you these values.
  - Technical: they are `TaskInfo.Id` and `TaskObjective.Id`.
- **An id must match exactly one task (or one step).** If it matches none or several, the call is refused. A task whose title is the same as another task's title is refused as well, because the game treats such tasks as the same task.
  - Technical: the game compares tasks with `TaskInfo.Compare`.

**Reading.**

- **`All()` lists every task the player has started, including finished and failed ones.** It never calls the game's own lookup that finishes a step as a side effect, so reading cannot change the player's progress.
  - Technical: it never calls `GetTaskAndObjective`, which finishes the objective it looks up.
- **`Get(taskId)` returns `null` for an empty, unknown or ambiguous id, and when the API is not available.**
- **`Reached(taskId, objectiveId)` is `true` when the player has got to that step.** That means the game recorded the step as active, failed or abandoned, or as finished with its completion requirements met. The record stays after the step closes. It is `false` for an empty id, an unknown id, and when the API is not available.
- **`Reached` does not check that the ids point to one task and one step.** It reads the game's own record, which stores the pair as `taskId + "_" + objectiveId`, so ids that contain `_` can collide. Avoid `_` in ids you choose yourself.

**Changing.**

- **`SetTask(taskId, state)` closes a task.** It accepts `Failed`, `Finished` and `Abandoned`, and the task must be active. It returns `true` when the task has the new state after the call.
- **Experimental: `SetTask(taskId, Finished)` does not run the task's rewards or start the tasks that follow it.** It only marks the task finished and moves it to the finished list. Each step that is still active is marked abandoned, and the game then runs those steps' own "abandoned" follow-ups.
  - Technical: the game's `TaskController.EditTaskStatus` only changes the task's status and moves it to the finished list. Its follow-ups for an abandoned step are the objective's abandon actions.
- **`SetObjective(taskId, objectiveId, state)` closes a step.** It accepts `Failed`, `Finished` and `Abandoned`, and the step must be active. `Finished` also needs the game's completion requirements for that step to pass. It returns `true` when the step has the new state after the call.
- **`AddProgress(taskId, objectiveId, amount)` adds to a step's counter.** `amount` must be above 0, and the step must be active and have a counter. The amount is cut to the room left on the counter, so the counter never goes past its target. When the add reaches the target, the step completes through the game, unless it is set not to complete by its counter. In that case its completion requirements must pass first, or the call returns `false` and changes nothing. It returns `true` when the counter or the status changed.
- **Writers are refused while the game is itself changing a task.** If you call a writer from inside one of the game's own task changes, including one made from another mod's patch, you get `false`, and it is logged once as "called while a quest hook is running" (the log's name for a task change the game is making). Your event handlers run after the game's change has finished, so they may call writers.
  - Technical: the game calls are `EditTask`, `EditTaskStatus` and `EditTaskObjective`.
- **Writers also need the task panel and the notifications to exist.** The game uses them when a task starts.
  - Technical: they need `TaskPanel.instance` and `Notifications.instance`.

**Events.**

- **`TaskChanged(args)` fires when a new task starts (`Active`), or a task moves to `Failed`, `Finished` or `Abandoned`.** The same goes for `ObjectiveChanged(args)` and steps: it fires when a step starts (`Active`) or its state changes.
- **Events come in the order the game made the changes, the nested ones first.** For a new task with a step, `ObjectiveChanged(Active)` comes before `TaskChanged(Active)`. A task closed by `SetTask` raises one `TaskChanged` with the closed state. A task that is created and closed in one game call raises only the closing event.
- **An event carries the state at the moment of the change.** Call `Get` to see the state now.

```csharp
using ObenseuerCore;

[ModInfo("example.questwatch", "Quest Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class QuestWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Quests.TaskChanged += args => Log.Info(args.TaskId + " is " + args.State);
        Events.WhenReady(() =>
        {
            bool done = Quests.SetObjective("<taskId>", "<objectiveId>", ObjectiveState.Finished);
            Log.Info("finished the step: " + done);
        });
    }
}
```

The ids here are placeholders, so on a real save the call returns `false` and nothing changes. Use ids from the game.
