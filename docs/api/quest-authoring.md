---
title: Obenseuer Modding Framework quest authoring API (QuestAuthoring)
parent: API reference
nav_order: 101.2
---

# Obenseuer Modding Framework quest authoring API (QuestAuthoring)

Experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.questauthoring`. Logs go to `BepInEx\logs\obenseuer.moddingframework.questauthoring.log`.
Files: `src/Story/QuestAuthoring.cs`. It builds on `Quests` (`src/Story/Quests.cs`). Every writer refuses while `QuestAuthoring.IsAvailable` is false, which also needs `Quests.IsAvailable`.

Everything not listed here is internal. Main thread only.

Scope: a mod creates a task (a quest) with objectives at runtime, and changes its objectives, counters and end state. Reading and changing the game's own tasks stays in `Quests` (see [Story API](../api-story.md)). A quest made here is an ordinary game task, so `Quests.All`, `Quests.Get`, `Quests.TaskChanged` and `Quests.ObjectiveChanged` see it.

## Rules

- **Ids.** A quest has a key you choose (letters, digits, `_` and `-`, up to 48 characters, no dot). The task id is `modId + "." + key`, so two mods never share an id. `QuestAuthoring.TaskId(modId, key)` builds it. An objective id is its key, and it must be unique inside the quest. The game's Lua functions (`TaskActive("<taskId>")`, `ObjectiveActive`, and the others) use these ids.
- **Owner.** Only the mod whose id is the prefix of the task id can change a quest through `QuestAuthoring`. `AddObjective`, `SetObjective`, `SetCounter`, `Finish` and `Fail` return `false` for another mod's quest or for a game quest. The owner is read from the id, so it still works after a restart or a load.
- **Titles.** The game finds a started task again with `TaskInfo.Compare`, which falls back to the title. The framework patches `Compare` so a quest made here (an id of the form `modId.key` and no addressable path) is compared by id only, against game tasks and other quests alike. A game task with the same title therefore never joins your quest, and your quest never takes over a game task's objectives, status or "new task" check. `Create` still refuses a title that another started task or a loaded task asset has (`DuplicateTitle`), because two quests with one title are confusing in the task panel. A game task that is not loaded and not started cannot be seen, so a clash with it is possible. It only shows as two equal titles in the panel. If the patch cannot be installed, quest authoring is switched off.
- **Create.** `Create` runs the game's own create path, `TaskController.EditTask` with `createNewTask`. The game shows the "New Task" notification, adds the task to the task panel, and queues `Quests.TaskChanged` and `Quests.ObjectiveChanged`. Every objective in the definition starts Active. A quest has 1 to 12 objectives.
- **Counters.** An objective with `Target` above 0 has a counter. When the counter reaches the target, the game finishes the objective. `SetCounter` sets an exact value from 0 to the target. For a relative change use `Quests.AddProgress`.
- **Ending.** `Finish` and `Fail` need an Active task. The game marks each still-active objective Abandoned. `Finish` does not run follow-up tasks (see `Quests.SetTask`). Use `Quests.SetTask` with `Abandoned` to abandon a quest.
- **Refusals.** `Create` returns `null` and sets `refusal`. The other writers return `false`. A write is also refused during a save or a load, while one of the game's quest hooks is running, and when the task panel or notifications are not loaded (outside a game scene). A refusal that is a bad argument or a conflict is logged once per reason.
- **Mod removed.** The quest stays in the save and in the task panel. The framework cannot remove it, because the game has no call for that. Without the framework the `Compare` patch is gone too, so the game compares the leftover quest by title again. A game task with the same title (even one that was never loaded when the quest was made) can then land on that entry, and its objectives join the leftover quest. Give your quests titles that no game quest uses, and prefer a new key over reusing a title.

## Saving

The framework writes no save file for quests. The game saves the task itself: `TaskController` serializes its whole task list, with each `TaskInfo` and `TaskObjective` written inline (title, description, objective ids, counters and targets, and each status), together with the id lists the Lua functions read. A quest made here is saved and loaded like any game quest.

After a load, the game's task list holds copies of your task and objective objects, not the ones you created. The game finds a task again with `TaskInfo.Compare` (by id for a quest made here, see Titles). `QuestAuthoring` keeps no object between calls. Every call looks the task up by its id in the game's current list, checks that the game's `Compare` maps the entry to itself, and uses the game's own object. So calls keep working after a save and a load, and after loading a different save. A quest that is not in the loaded save is simply unknown (`Quests.Get` returns `null`), and `Create` can make it again.

Because the data is in the game's save, the quest text is kept even when the mod is removed. To change a quest's text for existing saves, create a new quest with a new key.

## API

```csharp
public enum QuestRefusal { None, NotReady, SaveBusy, BadInput, DuplicateTitle, AlreadyExists, Rejected, Failed, PartiallyCreated }

public sealed class QuestObjectiveDefinition
{
    public string Key { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int Target { get; set; }         // counter target, 0 for none, at most 9999
    public bool Optional { get; set; }
}

public sealed class QuestDefinition
{
    public string Key { get; set; }
    public string Title { get; set; }       // up to 100 characters
    public string Description { get; set; } // up to 1000 characters, may be null
    public List<QuestObjectiveDefinition> Objectives { get; set; }
}

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
}
```

- `Create` returns the task id, or `null` with the reason in `refusal`. `AlreadyExists` means the save already has a task with that id, for example from an earlier session, so call `Quests.Get` first and create only when it returns `null`. `Rejected` means the game refused the first objective and no task was made. `Failed` means a game call threw before the task was made. `PartiallyCreated` is the one refusal that comes with a non-null return: the task was made, but the game refused or threw on a later objective, so the quest exists with fewer objectives than the definition. Add the missing ones with `AddObjective`, or finish or fail the quest.
- `AddObjective` adds an Active objective to an Active quest of yours. It returns `false` when the key is already used in that quest or the quest already has 12 objectives.
- `SetObjective` accepts `Failed`, `Finished` and `Abandoned` for an Active objective, as `Quests.SetObjective` does.
- `SetCounter` returns `true` when the counter changed. An amount outside 0 to the target is refused. Setting the target finishes the objective.

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

Set `Debug.QuestAuthoringSelfCheck` in the framework's config to log a check of the definition rules and the owner rule at start. Set `Debug.QuestAuthoringCommand` to register the `core_quests_check` console command, which creates, advances and finishes a test quest in the loaded save and logs the result. It adds a finished task to that save, so use a scratch save.
