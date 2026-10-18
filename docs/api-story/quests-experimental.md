---
title: Quests (experimental)
parent: Quests, dialogue and law
grand_parent: Story
nav_order: 3
---

# Quests (experimental)

The game gives the player tasks, and each task has steps, called objectives. `Quests` lets your mod list them, check how far the player is, move a task or a step on, and add progress to a step that counts things (for example "collect 5 cans"). It tells you when a task or step changes.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Quests</code> <span>list, check and move tasks and steps</span></p>

Use `All` and `Get` to read the tasks, `Reached` to check a step, and `SetTask`, `SetObjective` or `AddProgress` to move them on. `TaskChanged` and `ObjectiveChanged` tell you when they change.

<div class="api-pair" markdown="1">

```csharp
public static class Quests
{
    public static event Action<TaskChangedArgs> TaskChanged;
    public static event Action<ObjectiveChangedArgs> ObjectiveChanged;
    public static bool IsAvailable { get; }
    public static List<QuestInfo> All();
    public static QuestInfo Get(string taskId);
    public static bool Reached(string taskId, string objectiveId);
    public static bool SetTask(string taskId, TaskState state);
    public static bool SetObjective(string taskId, string objectiveId, ObjectiveState state);
    public static bool AddProgress(string taskId, string objectiveId, int amount);
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when the Quests API works. |

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<QuestInfo>` | Every task the player has started. |
| `Get(taskId)` | `QuestInfo` | One task. `null` when none matches. |
| `Reached(taskId, objectiveId)` | `bool` | `true` when the player has got to this step. |
| `SetTask(taskId, state)` | `bool` | Closes a task. `true` when it has that state afterwards. |
| `SetObjective(taskId, objectiveId, state)` | `bool` | Closes a step. `true` when it has that state afterwards. |
| `AddProgress(taskId, objectiveId, amount)` | `bool` | Adds to a step's counter. |

| Event | Gives you | When it fires |
|---|---|---|
| `TaskChanged` | `TaskChangedArgs` | A task started or changed state. `args.TaskId` and `args.State` tell you which, and to what. |
| `ObjectiveChanged` | `ObjectiveChangedArgs` | A step started or changed state. `args.TaskId`, `args.ObjectiveId` and `args.State` tell you which, and to what. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>QuestInfo</code> <span>a task</span></p>

`All` and `Get` give you one of these for each task. Its `Objectives` lists the steps, as `ObjectiveInfo` values.

<div class="api-pair" markdown="1">

```csharp
public sealed class QuestInfo
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public TaskState State { get; }
    public ObjectiveInfo[] Objectives { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The task's id (see Ids below). |
| `Title` | `string` | The task's name. |
| `Description` | `string` | The text the player reads. |
| `State` | `TaskState` | Where the task is. |
| `Objectives` | `ObjectiveInfo[]` | The steps of the task. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ObjectiveInfo</code> <span>one step of a task</span></p>

Each step of a `QuestInfo` is one of these. `Current` and `Target` show its counter, and `AddProgress` adds to it. `Target` is `0` when the step has no counter.

<div class="api-pair" markdown="1">

```csharp
public sealed class ObjectiveInfo
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public ObjectiveState State { get; }
    public bool Optional { get; }
    public int Current { get; }
    public int Target { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The step's id (see Ids below). |
| `Title` | `string` | The step's name. |
| `Description` | `string` | The text the player reads. |
| `State` | `ObjectiveState` | Where the step is. |
| `Optional` | `bool` | `true` when the step is not needed to finish the task. |
| `Current` | `int` | The counter now. |
| `Target` | `int` | The counter the step needs. `0` when the step has no counter. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TaskState</code> <span>where a task is</span></p>

`QuestInfo.State` holds this. `SetTask` accepts `Failed`, `Finished` and `Abandoned`.

<div class="api-pair" markdown="1">

```csharp
public enum TaskState
{
    Active = 0,
    Failed = 1,
    Finished = 2,
    Abandoned = 3
}
```

| Value | Meaning |
|---|---|
| `Active` | The player is working on it. |
| `Failed` | The player failed it. |
| `Finished` | The player completed it. |
| `Abandoned` | The task was dropped. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ObjectiveState</code> <span>where one step of a task is</span></p>

`ObjectiveInfo.State` holds this. `SetObjective` accepts `Failed`, `Finished` and `Abandoned`, and the game sets `Hidden`.

<div class="api-pair" markdown="1">

```csharp
public enum ObjectiveState
{
    Active = 0,
    Failed = 1,
    Finished = 2,
    Abandoned = 3,
    Hidden = 4
}
```

| Value | Meaning |
|---|---|
| `Active` | The player is working on it. |
| `Failed` | The player failed it. |
| `Finished` | The player completed it. |
| `Abandoned` | The step was dropped. |
| `Hidden` | The game has set the step to hidden. |

</div>
</div>

## Putting it together

Logs a line each time a task changes, then closes a step. The ids here are placeholders, so on a real save the call returns `false` and nothing changes. Use ids from the game.

<div class="api-example" markdown="1">

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

| Step | What it does |
|---|---|
| `Quests.TaskChanged += ...` | Logs the task id and its new state each time a task changes. |
| `Events.WhenReady(...)` | Waits until a save is loaded before it runs the rest. |
| `Quests.SetObjective("<taskId>", "<objectiveId>", ObjectiveState.Finished)` | Closes one step as finished, if the ids are real. |
| `Log.Info("finished the step: " + done)` | Logs whether the step was closed. |

</div>

## Things to know

### Ids

- A task id is the game's own id for the task. It is the task's `id` field. If that is empty it is the task's addressable asset path, and if that is empty too it is the task's title. A step id works the same way. `QuestInfo.Id` and `ObjectiveInfo.Id` give you these values. <span class="tech">(They are `TaskInfo.Id` and `TaskObjective.Id`.)</span>
- An id must match exactly one task (or one step). If it matches none or several, the call is refused. A task whose title is the same as another task's title is refused as well, because the game treats such tasks as the same task. <span class="tech">(The game compares tasks with `TaskInfo.Compare`.)</span>

### Reading

- `All()` lists every task the player has started, including finished and failed ones. It never calls the game's own lookup that finishes a step as a side effect, so reading cannot change the player's progress. <span class="tech">(It never calls `GetTaskAndObjective`, which finishes the objective it looks up.)</span>
- `Get(taskId)` returns `null` for an empty, unknown or ambiguous id, and when the API is not available.
- `Reached(taskId, objectiveId)` is `true` when the player has got to that step. That means the game recorded the step as active, failed or abandoned, or as finished with its completion requirements met. The record stays after the step closes. It is `false` for an empty id, an unknown id, and when the API is not available.
- `Reached` does not check that the ids point to one task and one step. It reads the game's own record, which stores the pair as `taskId + "_" + objectiveId`, so ids that contain `_` can collide. Avoid `_` in ids you choose yourself.

### Changing

- `SetTask(taskId, state)` closes a task. It accepts `Failed`, `Finished` and `Abandoned`, and the task must be active. It returns `true` when the task has the new state after the call.
- `SetTask(taskId, Finished)` does not run the task's rewards or start the tasks that follow it. It only marks the task finished and moves it to the finished list. Each step that is still active is marked abandoned, and the game then runs those steps' own "abandoned" follow-ups. <span class="tech">(The game's `TaskController.EditTaskStatus` only changes the task's status and moves it to the finished list. Its follow-ups for an abandoned step are the objective's abandon actions.)</span>
- `SetObjective(taskId, objectiveId, state)` closes a step. It accepts `Failed`, `Finished` and `Abandoned`, and the step must be active. `Finished` also needs the game's completion requirements for that step to pass. It returns `true` when the step has the new state after the call.
- `AddProgress(taskId, objectiveId, amount)` adds to a step's counter. `amount` must be above 0, and the step must be active and have a counter. The amount is cut to the room left on the counter, so the counter never goes past its target. When the add reaches the target, the step completes through the game, unless it is set not to complete by its counter. In that case its completion requirements must pass first, or the call returns `false` and changes nothing. It returns `true` when the counter or the status changed.
- Your event handlers run after the game's change has finished, so they may call writers.

### Events

- `TaskChanged` fires when a new task starts (`Active`), or a task moves to `Failed`, `Finished` or `Abandoned`. The same goes for `ObjectiveChanged` and steps: it fires when a step starts (`Active`) or its state changes.
- Events come in the order the game made the changes, the nested ones first. For a new task with a step, `ObjectiveChanged(Active)` comes before `TaskChanged(Active)`. A task closed by `SetTask` raises one `TaskChanged` with the closed state. A task that is created and closed in one game call raises only the closing event.
- An event carries the state at the moment of the change. Call `Get` to see the state now.

## Limits

- Writers are refused while the game is itself changing a task. If you call a writer from inside one of the game's own task changes, including one made from another mod's patch, you get `false`. It is logged once as "called while a quest hook is running" (the log's name for a task change the game is making). <span class="tech">(The game calls are `EditTask`, `EditTaskStatus` and `EditTaskObjective`.)</span>
- Writers also need the task panel and the notifications to exist. The game uses them when a task starts. <span class="tech">(They need `TaskPanel.instance` and `Notifications.instance`.)</span>
