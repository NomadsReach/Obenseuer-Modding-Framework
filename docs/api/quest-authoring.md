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
- **Ids.** A quest has a key you choose (letters, digits, `_` and `-`, up to 48 characters, no dot). The task id is `modId + "." + key`, so two mods never share an id. `QuestAuthoring.TaskId(modId, key)` builds it. An objective id is its key, and it must be unique inside the quest. The game's Lua functions (`TaskActive("<taskId>")`, `ObjectiveActive`, and the others) use these ids.
- **Owner.** Only the mod whose id is the prefix of the task id can change a quest through `QuestAuthoring`. `AddObjective`, `SetObjective`, `SetCounter`, `Finish` and `Fail` return `false` for another mod's quest or for a game quest. The owner is read from the id, so it still works after a restart or a load.
- **Titles.** The game finds a started task again with `TaskInfo.Compare`, which falls back to the title. The framework patches `Compare` so a quest made here (an id of the form `modId.key` and no addressable path) is compared by id only, against game tasks and other quests alike. A game task with the same title therefore never joins your quest, and your quest never takes over a game task's objectives, status or "new task" check. `Create` still refuses a title that another started task or a loaded task asset has (`DuplicateTitle`), because two quests with one title are confusing in the task panel. A game task that is not loaded and not started cannot be seen, so a clash with it is possible. It only shows as two equal titles in the panel. If the patch cannot be installed, quest authoring is switched off.
- **Create.** `Create` runs the game's own create path, `TaskController.EditTask` with `createNewTask`. The game shows the "New Task" notification, adds the task to the task panel, and queues `Quests.TaskChanged` and `Quests.ObjectiveChanged`. Every objective in the definition starts Active, unless it sets `Hidden` (see [Hidden objectives and reveal](#hidden-objectives-and-reveal)). A quest has 1 to 12 objectives.
- **Counters.** An objective with `Target` above 0 has a counter. When the counter reaches the target, the game finishes the objective. `SetCounter` sets an exact value from 0 to the target. For a relative change use `Quests.AddProgress`. A counter that reaches its target while the objective is still hidden does not finish anything: the game's finish call does nothing for a non-Active objective, so the counter sits at the target. `Reveal` finishes such an objective itself (see [Hidden objectives and reveal](#hidden-objectives-and-reveal)).
- **Ending.** `Finish` and `Fail` need an Active task. The game marks each still-active objective Abandoned. `Finish` does not run follow-up tasks (see `Quests.SetTask`). Use `Quests.SetTask` with `Abandoned` to abandon a quest.
- **Refusals.** `Create` returns `null` and sets `refusal`. The other writers return `false`. A write is also refused during a save or a load, while one of the game's quest hooks is running, and when the task panel or notifications are not loaded (outside a game scene). A refusal that is a bad argument or a conflict is logged once per reason.
- **Mod removed.** The quest stays in the save and in the task panel. The framework cannot remove it, because the game has no call for that. Without the framework the `Compare` patch is gone too, so the game compares the leftover quest by title again. A game task with the same title (even one that was never loaded when the quest was made) can then land on that entry, and its objectives join the leftover quest. Give your quests titles that no game quest uses, and prefer a new key over reusing a title.

## Hidden objectives and reveal

An objective can start hidden and show its real text later.

```csharp
public sealed class QuestObjectiveDefinition
{
    public bool Hidden { get; set; }
}
```

```csharp
public static class QuestAuthoring
{
    public static bool Reveal(string modId, string taskId, string key, string title, string description);
}
```

- **A placeholder is created, not your text.** A hidden objective is created with the title `???` and an empty description, whatever `Title` and `Description` hold, and its text is validated as usual but not written. This is deliberate: the game saves each objective's title and description inside the save file, so a spoiler written at creation would sit in the save from the moment the quest starts.
- **The player sees the placeholder.** The game's task panel lists every objective of the task, hidden or not, with its title, and it adds each objective's description under the task description. `Hidden` only keeps the objective out of the game's Lua status lists, so `ObjectiveActive` and `Quests.Reached` do not report it. The framework does not patch the panel.
- **Reveal writes the text and activates the objective.** `Reveal` needs your own Active quest and a hidden objective. It writes the real title and description onto the objective and then activates it through the game's `TaskController.EditTaskObjective`, which shows the "New Objective" notification with the real title and adds the objective to the game's active list. `EditTask` cannot do this, because it refuses `Active` for an objective that already exists. The reveal is the moment the real text reaches the save. Two things follow from the game's activation call. The "New Objective" notification always reads `0/<target>` even when the counter is already higher, because the game builds that text from the target and not from the current count. And when the counter is already at or past the target and the objective is not marked to skip self completion by counter, `Reveal` finishes the objective immediately after the activation, through the same game call the counter completion uses, so a revealed objective can never stay Active at n of n. `Reveal` then runs the task's own finish rule with the game's `FinishIfNoActiveObjectives`, the call the counter completion makes next, so a task with `finishTaskWhenTheresNoActiveTasks` finishes at that moment instead of staying Active.
- **Panel refresh.** `Reveal` calls the game's `TaskPanel.UpdateCurrentTaskInfo` after the activation, the same refresh `EditTask` makes, so the revealed text shows at once instead of on the next quest change.
- **Refusals.** `Reveal` returns `false` when the quest is not yours, a save or load is running, the quest is not Active, the objective is missing or is not hidden (already revealed, finished, failed or abandoned), when the title is empty or over 100 characters, or when the description is over 1000 characters. A refused or throwing activation puts the placeholder back, so a hidden objective never keeps spoiler text. A failure of the panel refresh does not turn a successful reveal into a `false`: once the objective is Active, `Reveal` returns `true` and logs that failure on its own.
- **Creation raises the event.** A hidden objective raises `Quests.ObjectiveChanged` with `ObjectiveState.Hidden` when it is created, and `Reveal` raises another with `Active` and the live counts.
- **Counters keep working while hidden.** A counter change on a hidden objective raises `Quests.ObjectiveChanged` with `ObjectiveState.Hidden` and the new `Current` and `Target`, which is how a mod can show progress before the reveal.
- **No re-hiding.** `SetObjective` does not accept `Hidden`, and a revealed objective cannot be hidden again. The game's own status writers only move an objective forward.

## Reopening or restarting a quest is not supported

There is no reopen call, and `Finish` or `Fail` cannot be undone. The game's own path for putting a task back to Active adds the id to the active list but never removes it from the finished, failed or abandoned list, and those lists accept the same id twice. A reopened quest is therefore reported as both Active and Finished: the task panel draws it twice, the game's own status checks answer for both, and a dialogue condition such as `TaskFinished("<taskId>")` stays true while the player is doing the quest again. The framework refuses to set a task back to `Active` for that reason. A quest that must be playable again has to be a new quest with a new key, and the old one stays in the save as finished.

## Saving

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
    public string Key { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int Target { get; set; }         // counter target, 0 for none, at most 9999
    public bool Optional { get; set; }
    public bool Hidden { get; set; }        // starts the objective hidden with a placeholder title, see below
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

```csharp
public static class QuestAuthoring
{
    public static bool IsAvailable { get; }
    public static string TaskId(string modId, string key);                                  // null when either is invalid
    public static string Create(string modId, QuestDefinition definition, out QuestRefusal refusal);
    public static bool AddObjective(string modId, string taskId, QuestObjectiveDefinition objective);
    public static bool SetObjective(string modId, string taskId, string objectiveKey, ObjectiveState state);
    public static bool SetCounter(string modId, string taskId, string objectiveKey, int amount);
    public static bool Finish(string modId, string taskId);
    public static bool Fail(string modId, string taskId);
    public static bool Reveal(string modId, string taskId, string objectiveKey, string title, string description);
}
```

- `Create` returns the task id, or `null` with the reason in `refusal`. `AlreadyExists` means the save already has a task with that id, for example from an earlier session, so call `Quests.Get` first and create only when it returns `null`. `Rejected` means the game refused the first objective and no task was made. `Failed` means a game call threw before the task was made. `PartiallyCreated` is the one refusal that comes with a non-null return: the task was made, but the game refused or threw on a later objective, so the quest exists with fewer objectives than the definition. Add the missing ones with `AddObjective`, or finish or fail the quest.
- `AddObjective` adds an Active objective to an Active quest of yours. It returns `false` when the key is already used in that quest or the quest already has 12 objectives.
- `SetObjective` accepts `Failed`, `Finished` and `Abandoned` for an Active objective, as `Quests.SetObjective` does.
- `SetCounter` returns `true` when the counter changed. An amount outside 0 to the target is refused. Setting the target finishes the objective.
- `Reveal` returns `true` when the objective is Active afterwards, even when the panel refresh after the activation throws; that failure is logged on its own. Its rules are in [Hidden objectives and reveal](#hidden-objectives-and-reveal).

## Example

```csharp
QuestDefinition quest = new QuestDefinition { Key = "lost_key", Title = "The lost key", Description = "Find the key." };
quest.Objectives.Add(new QuestObjectiveDefinition { Key = "search", Title = "Search the rooms", Target = 3 });
quest.Objectives.Add(new QuestObjectiveDefinition { Key = "return", Title = "Bring it back" });

string id = QuestAuthoring.TaskId("example.mymod", "lost_key");
if (Quests.Get(id) == null)
{
    QuestRefusal refusal;
    QuestAuthoring.Create("example.mymod", quest, out refusal);
}
QuestAuthoring.SetCounter("example.mymod", id, "search", 3);
QuestAuthoring.Finish("example.mymod", id);
```

## Self-check

Set `Debug.QuestsSelfCheck` in the framework's config to log a check of the counter event rule and the ready retry bookkeeping at start. Set `Debug.QuestAuthoringSelfCheck` in the framework's config to log a check of the definition rules, the hidden objective text rule, the reveal state rule and the owner rule at start. Set `Debug.QuestAuthoringCommand` to register the `core_quests_check` console command, which creates, advances and finishes a test quest in the loaded save and logs the result. It adds a finished task to that save, so use a scratch save.
