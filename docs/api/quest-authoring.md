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

The game calls a quest a task. A task has a title, a description and a list of objectives, which are the steps the player works through. The player sees tasks in the task panel. This API lets your mod make a new task while the game runs, add objectives to it, move its counters and end it as finished or failed. A task made here is an ordinary game task, so the player sees it in the task panel, and `Quests.All`, `Quests.Get`, `Quests.TaskChanged` and `Quests.ObjectiveChanged` see it too. An objective can also ask the player for items, which the framework takes in one step, and a quest can name a follow-up quest that starts when it finishes.

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
- **Ending.** `Finish` and `Fail` need an Active task. The game marks each still-active objective Abandoned. `Finish` does not run the game's own follow-up tasks (see `Quests.SetTask`), but a finished quest does start the framework follow-up named by `Next`. Use `Quests.SetTask` with `Abandoned` to abandon a quest.
- **Item hand-in.** An objective can ask the player for items: set `ItemId` and `Count` on `QuestObjectiveDefinition`. `QuestAuthoring.HandIn` takes those items and moves that objective on by the count. The take is all or nothing, so a refused move gives the items back. See [Handing in items](#handing-in-items).
- **Follow-up quest.** Set `QuestDefinition.Next` to the key of another quest of yours and give that quest's definition in `QuestDefinition.NextDefinition`, and the framework starts that quest when the first one finishes. See [Follow-up quest](#follow-up-quest).
- **Testing.** With `Debug.QuestAuthoringCommand` on, the console commands `core_quest reset <taskId>` and `core_quest skip <taskId> [key]` start a quest over or finish a quest or one objective. See [Console commands](#console-commands).
- **Refusals.** `Create` returns `null` and sets `refusal`. The other writers return `false`. A write is also refused during a save or a load, while one of the game's quest hooks is running, and when the task panel or notifications are not loaded (outside a game scene). A refusal that is a bad argument or a conflict is logged once per reason.
- **Mod removed.** The quest stays in the save and in the task panel. While the framework is loaded and the quest is in the live task list, `Remove` deletes it, and the next game save no longer holds it; a save written after the framework is gone, or a quest already written before `Remove` was called, still has it until a later save drops it. Without the framework the `Compare` patch is gone too, so the game compares the leftover quest by title again. A game task with the same title (even one that was never loaded when the quest was made) can then land on that entry, and its objectives join the leftover quest. Give your quests titles that no game quest uses, and prefer a new key over reusing a title.

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

## Rewinding a quest

A finished, failed or abandoned quest of yours can go back to its start, be taken out of the game, or be relabelled. Every call needs your own quest that is in the live task list, is refused while a save or a load is running, and returns `false` when it cannot do what you asked.

```csharp
public static class QuestAuthoring
{
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

- **The reopen bug is not used.** The game only ever adds an id to its five task lists and its five objective lists. It never takes an id out of the finished, failed or abandoned list when a task starts again, so a task the game reopens is reported as both Active and Finished, the panel can draw it twice, and `TaskFinished("<taskId>")` stays true while the player plays it again. The framework never calls that path. Instead it writes the ten lists itself from the live task and objective states, so an id sits in exactly the lists its state belongs to and nowhere else. `Quests.SetTask` still refuses to reopen a task by itself: use `Restart` or `Reset`.
- **Restart.** Sends one of your finished, failed or abandoned quests back to its start. Every objective returns to its start: its state is Active, or Hidden for an objective created hidden, its counter is 0, and its text is the text from your definition, so a hidden objective shows the placeholder again. The quest is Active again, it returns to the active list, and it leaves the finished, failed and abandoned lists. `Restart` returns `false` while the quest is still Active, because there is nothing to restart.
- **Reset.** The same as `Restart`, from any state, including Active. Use it to start a quest over that the player has already begun.
- **JumpTo.** Rewinds the quest to one objective: the objectives before it are Finished and their counters sit at their target, the chosen objective is Active with its real text even when it was created hidden, the ones after it go back to their start, and the quest is Active. Returns `false` when the objective is not on the quest.
- **Remove.** Deletes one of your quests from the live task list, the task panel and the framework's record of its definition, so `Create` accepts the same key again in the same session. The next game save no longer holds it. `Remove` only sees the live task list, so it cannot delete a quest that is not there.
- **SetTitle and SetDescription.** Change the title of the quest, the title of one of its objectives, or the description of the quest, and refresh the task panel at once. A title is 1 to 100 characters and a description is at most 1000. The three calls return `false` on a bad length, a missing objective, or a quest that is not yours. Like `Reveal`, a failure of the panel refresh does not turn a successful write into `false`; it is logged on its own.
- **Checkpoint.** `Checkpoint(modId, taskId, name, restore)` keeps one named capture of the live state of the quest in memory when `restore` is false, and puts that state back when `restore` is true. A capture holds the quest state and every objective's state and count, a quest can hold 16 captures, and a name is 1 to 32 characters of letters, digits and `-`. Captures do not outlive the game session and `Remove` drops them. Returns `false` for a bad name, a quest that is not yours, or a name that was never captured when `restore` is true.
- **A checkpoint can run code when it is put back.** `Checkpoint(modId, taskId, name, restoreCallback)` keeps the capture exactly as the call above does, and remembers `restoreCallback` under that name. The callback runs once when that name is restored, after the state is back and after `Quests.Rewound` is raised for the restore, so a handler on the event sees the quest before the callback runs. An exception in the callback is logged once and swallowed, and the restore still returns `true`. The callback is kept, not used up, so restoring the same name again runs it again. Capturing the same name without a callback clears it, a second callback for that name replaces it, and a capture that is refused neither sets nor clears the callback remembered for that name. Callbacks are dropped with the captures at the framework's session point, so they do not outlive the game session either. The call returns `false` for the same reasons the capture does.

```csharp
string id = QuestAuthoring.TaskId(Mod.Id, "lost_key");

// The capture holds the quest and its objectives. The callback puts the rest back.
QuestAuthoring.Checkpoint(Mod.Id, id, "before_talk", () => PutSignBack());

// Later, when the player asks to undo the talk. PutSignBack runs at the end of this call.
QuestAuthoring.Checkpoint(Mod.Id, id, "before_talk", true);
```

- **What a rewind can restore.** The framework keeps its own record of every quest it created in a file of its own in the save folder, holding the start state of each objective: `Hidden`, `Target`, `Optional` and the text from your definition. `Create` fills the record and `AddObjective` adds to it. A quest that was in the save before this version, or one created outside `QuestAuthoring`, has no record, so a rewind starts each of its objectives Active with its counter at 0 and leaves its text alone. The framework never writes that record into the game's save.
- **The event.** `Quests.Rewound` reports every one of these calls after the change, with the task id and a `Reason` of `SetTitle`, `SetDescription`, `Restart`, `Reset`, `Remove`, `JumpTo` or `Checkpoint`. `From` and `To` are objective keys: the objective the quest was on before the call, and the one it is on after. `From` and `To` are `null` when no objective is `Active` at that moment: `From` when the quest had no active objective before the call, and `To` when it has none after it. Both are also `null` for `SetTitle`, `SetDescription` and `Remove`, which move no objective.
- **Reading one state.** `Quests.State(taskId, objectiveKey)` returns the state of a single objective, or `null` when the task or the objective is missing. It builds no objects, so it is safe to call every frame. `Quests.State(taskId)` still returns the quest state.
- **Refusals.** A call on a quest that is not yours returns `false` without a log, as the other writers do. A call on a quest that is not in the live task list, or a `JumpTo` on a missing objective, logs once and returns `false`.

## Handing in items

An objective can ask the player for items, as well as or instead of a counter. Set `ItemId` and `Count` on the objective, then call `QuestAuthoring.HandIn(modId, taskId, objectiveKey)` from your own code, for example when the player talks to the quest giver.

- **The pair.** `ItemId` 0 needs `Count` 0, and an `ItemId` above 0 needs a `Count` of 1 to 9999. Any other pair is refused with `BadInput` when the quest is created, and `AddObjective` returns `false` for it.
- **What it does.** `HandIn` needs your own Active quest, an Active objective on it that asks for items, and the player holding all of them. It takes the items in one step, then moves the objective on: by `Count` when the objective has a counter, and to Finished when it has none. It returns `true` when the objective moved.
- **All or nothing.** When the move fails or throws, the items are given back, so the player either keeps every item or loses exactly the ones the objective asked for. A failure of the give-back is logged once on its own.
- **The whole count is taken.** When the counter has less room left than the count, the counter stops at its target and the rest of the items are still taken. Keep `Count` the same as the room the objective has.
- **Refusals.** `HandIn` returns `false` when the quest is not yours, a save or a load is running, the quest is not Active, the objective is missing or not Active, the objective asks for no items, or the player does not hold all of them. The last three are logged once each.
- **The item id is the game's.** `ItemId` is the game's own item id, the number on the item's asset. The framework does not check that the item exists.
- **A load keeps the pair.** `ItemId` and `Count` are kept in the framework's own file in the save folder with the rest of the definition, so a hand-in still knows what to take after the save is loaded again.

The example below asks for the key back, and takes it only when the player says yes.

```csharp
using ObenseuerCore;

[ModInfo("example.keyrun", "Key Run", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class KeyRun : FrameworkMod
{
    protected override void OnLoad()
    {
        // Fires at each save load, once the task panel and the notifications are up.
        Quests.Ready += Register;
    }

    private void Register()
    {
        string id = QuestAuthoring.TaskId(Mod.Id, "lost_key");
        if (Quests.Get(id) != null) return;

        QuestDefinition quest = new QuestDefinition { Key = "lost_key", Title = "The lost key", Description = "Find the key and bring it back." };
        quest.Objectives.Add(new QuestObjectiveDefinition { Key = "search", Title = "Search the rooms", Target = 3 });
        quest.Objectives.Add(new QuestObjectiveDefinition { Key = "return", Title = "Bring the key back", ItemId = 1, Count = 1 });

        QuestRefusal refusal;
        if (QuestAuthoring.Create(Mod.Id, quest, out refusal) == null)
        {
            Log.Warn("The quest was refused: " + refusal);
        }
    }

    /// <summary>Call this when the player offers the key.</summary>
    public void HandInKey()
    {
        string id = QuestAuthoring.TaskId(Mod.Id, "lost_key");
        if (!QuestAuthoring.HandIn(Mod.Id, id, "return"))
        {
            Log.Warn("HandIn was refused for " + id);
        }
    }
}
```

## Follow-up quest

`QuestDefinition.Next` names the quest to start when this one finishes. It is a quest key of your own mod, with the same rules as `Key`: letters, digits, `_` and `-`, up to 48 characters, and no dot. A value that is not a valid key is refused with `BadInput` when the quest is created.

The framework starts that quest from its definition, so give it in `QuestDefinition.NextDefinition` on the same object: its `Key` is the quest key, and its `Title`, `Description` and `Objectives` are the ones the follow-up is made with, `Hidden`, `Target`, `ItemId` and `Count` included. When you set `NextDefinition` you can leave `Next` empty, because the framework then takes the key from `NextDefinition.Key`. When you set both they have to agree, or `Create` is refused with `BadInput`.

- **It is stored with the parent.** `Create` copies the follow-up's definition into the framework's own file in the save folder along with the parent's, so a save and a load keep it, and the follow-up does not depend on your mod calling `Create` for it later. A chain holds at most 8 quests: a longer chain is refused with `BadInput`, while a definition that names itself or that loops is accepted, because the rules below stop it.
- **When it starts.** When a quest of yours is finished, the framework builds the task id `modId + "." + Next` and starts that quest the way `Create` does, from the stored definition.
- **When it is skipped.** The framework does not start it when a task with that id already exists, when `Next` names the quest that just finished, when the same follow-up was already started in this session, when this session has already started 8 follow-ups, or when no definition for it was stored. A chain therefore cannot loop, and one quest is never added twice. The self reference, the repeat, the cap and a missing definition are logged once, and so is a refusal from `Create`; the other two skips are silent.
- **An older save.** A parent quest that an earlier version of the framework created has no stored follow-up definition, so nothing starts and the skip is logged. The same happens for a `Next` that names a key you gave no `NextDefinition` for.
- **Only a finished quest.** The follow-up starts from `Quests.TaskChanged` with `TaskState.Finished`. A failed or abandoned quest does not start one, and neither does a rewind, because `Restart`, `Reset`, `JumpTo`, `Remove`, the checkpoint restore and the relabel calls do not raise that event. Nothing starts a follow-up while a save or a load is running, or when quest authoring is not available.
- **The follow-up is a quest of its own.** It obeys every rule on this page, so it needs a title no other started task uses. Two quests with one title are refused with `DuplicateTitle`.

## Console commands

The framework registers these command forms when `Debug.QuestAuthoringCommand` is `true` in the framework's config and the game console is available.

| Command | What it does |
|---|---|
| `core_quest reset <taskId>` | Starts your quest over from any state. |
| `core_quest skip <taskId>` | Finishes the whole quest. |
| `core_quest skip <taskId> <key>` | Finishes the one objective named by `key`. |

- `reset` takes no key, and `skip` takes at most one. Anything else prints `core_quest reset <taskId> or core_quest skip <taskId> [key]`.
- `reset` needs a task id of one of your quests, and the quest must be in the live task list. The mod id is read from the task id.
- `skip` goes through the game's own writers, so `skip <taskId>` needs an Active quest and `skip <taskId> <key>` needs an Active objective.
- Every command answers with a line that says what happened.
- Both change the live save, so use a scratch save. The same config also registers `core_quests_check`, which creates, advances and finishes a test quest.

## Quick example

This mod registers one quest each time the game is ready, unless the save already has it.

```csharp
using ObenseuerCore;

[ModInfo("example.questy", "Quest Maker", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class QuestMaker : FrameworkMod
{
    protected override void OnLoad()
    {
        Quests.Ready += MakeQuest;
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

The check `Quests.Get(id) != null` matters. The game saves the task, so on the next start the task is already in the save, and `Create` would be refused. `Quests.Ready` fires at every save load and every new game, once the task panel and the notifications are up, so it is the place to register quests; the handler has to be safe to run again, and the check makes it so. `Log.Warn` prints the reason a create was refused, which is the first thing to look at when a quest does not appear. `Mod.Id` is your mod's own id; it becomes the start of the task id. See [Using events](../guides/getting-started/using-events.md).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>QuestDefinition</code> <span>the quest you want to make</span></p>

A quest has a key, a title, a description, an optional follow-up quest and 1 to 12 objectives.

<div class="api-pair" markdown="1">

```csharp
public sealed class QuestDefinition
{
    public string Key { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Next { get; set; }                                // the key of the quest to start when this one finishes
    public QuestDefinition NextDefinition { get; set; }             // that quest's definition, stored with this one
    public List<QuestObjectiveDefinition> Objectives { get; set; }
}
```

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>QuestObjectiveDefinition</code> <span>one step of a quest</span></p>

Each step in `Objectives` is one of these. `Target` gives the step a counter, and `ItemId` and `Count` make it ask the player for items.

<div class="api-pair" markdown="1">

```csharp
public sealed class QuestObjectiveDefinition
{
    public string Key { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int Target { get; set; }                                 // counter target, 0 for none, at most 9999
    public bool Optional { get; set; }
    public bool Hidden { get; set; }
    public int ItemId { get; set; }                                 // the item the objective asks for, 0 for none
    public int Count { get; set; }                                  // how many, 1 to 9999 when ItemId is above 0
}
```

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>QuestRefusal</code> <span>why a quest was not made</span></p>

`Create` sets this through its `out refusal` parameter. `None` means nothing was refused.

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

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>QuestAuthoring</code> <span>makes and changes your quests</span></p>

The calls that make a quest, change it, rewind it and take it out. Each one returns `false`, or `null` for `Create`, when it refuses, and the reasons are in the sections above.

<div class="api-pair" markdown="1">

```csharp
public static class QuestAuthoring
{
    public static bool IsAvailable { get; }
    public static string TaskId(string modId, string key);                                  // null when either is invalid
    public static string Create(string modId, QuestDefinition definition, out QuestRefusal refusal);
    public static bool AddObjective(string modId, string taskId, QuestObjectiveDefinition objective);
    public static bool SetObjective(string modId, string taskId, string objectiveKey, ObjectiveState state);
    public static bool SetCounter(string modId, string taskId, string objectiveKey, int amount);
    public static bool HandIn(string modId, string taskId, string objectiveKey);
    public static bool Finish(string modId, string taskId);
    public static bool Fail(string modId, string taskId);
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

</div>
</div>

- `Create` returns the task id, or `null` with the reason in `refusal`. `AlreadyExists` means the save already has a task with that id, for example from an earlier session, so call `Quests.Get` first and create only when it returns `null`. `Rejected` means the game refused the first objective and no task was made. `Failed` means a game call threw before the task was made. `PartiallyCreated` is the one refusal that comes with a non-null return: the task was made, but the game refused or threw on a later objective, so the quest exists with fewer objectives than the definition. Add the missing ones with `AddObjective`, or finish or fail the quest.
- `AddObjective` adds an Active objective to an Active quest of yours. It returns `false` when the key is already used in that quest or the quest already has 12 objectives.
- `SetObjective` accepts `Failed`, `Finished` and `Abandoned` for an Active objective, as `Quests.SetObjective` does.
- `SetCounter` returns `true` when the counter changed. An amount outside 0 to the target is refused. Setting the target finishes the objective.
- `Reveal` returns `true` when the objective is Active afterwards, even when the panel refresh after the activation throws; that failure is logged on its own. Its rules are in [Hidden objectives and reveal](#hidden-objectives-and-reveal).
- `HandIn` returns `true` when the objective moved. It takes the items the objective asks for first, and gives them back when the move fails, so a refused call never costs the player an item. Its rules are in [Handing in items](#handing-in-items).
- `Next`, `NextDefinition`, `ItemId` and `Count` are checked when the definition is created. A `Next` that is not a valid key, a `NextDefinition` whose `Key` does not match `Next`, a chain longer than 8 quests, or an item pair that does not fit, is refused with `BadInput`.
- `Restart`, `Reset`, `Remove`, `JumpTo`, `Checkpoint` and the two `SetTitle` calls, with `SetDescription`, are in [Rewinding a quest](#rewinding-a-quest).

## Complete example

This mod makes one quest with a counter, a hand-in and a follow-up quest. It also gives you keys to test the rewind calls. Every call that can refuse is checked, and the warning says why.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.lostbottle", "Lost Bottle", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class LostBottle : FrameworkMod
{
    private const string QuestKey = "lost_bottle";
    private const string ItemTitle = "Empty Glass Bottle";
    private string taskId;
    private ModKeybind searchKey;
    private ModKeybind handInKey;
    private ModKeybind jumpKey;
    private ModKeybind restartKey;
    private ModKeybind resetKey;
    private ModKeybind removeKey;

    protected override void OnLoad()
    {
        taskId = QuestAuthoring.TaskId(Mod.Id, QuestKey);
        searchKey = Mod.Key("Search", "Count one search", KeyCode.F6);
        handInKey = Mod.Key("HandIn", "Hand in the bottle", KeyCode.F7);
        jumpKey = Mod.Key("Jump", "Jump to the hand-in step", KeyCode.F8);
        restartKey = Mod.Key("Restart", "Restart a finished quest", KeyCode.F9);
        resetKey = Mod.Key("Reset", "Reset the quest", KeyCode.F10);
        removeKey = Mod.Key("Remove", "Remove the quest and make it again", KeyCode.F11);
        Quests.Ready += Register;
        Quests.TaskChanged += OnTaskChanged;
    }

    private void Register()
    {
        if (Quests.Get(taskId) != null) return;

        ItemDef bottle = Items.Find(ItemTitle);
        if (bottle == null)
        {
            Log.Warn("No item is named " + ItemTitle + ", so the quest was not made");
            return;
        }

        QuestDefinition quest = new QuestDefinition { Key = QuestKey, Title = "The lost bottle", Description = "Find the bottle and hand it back." };
        quest.Objectives.Add(new QuestObjectiveDefinition { Key = "search", Title = "Search the rooms", Target = 3 });
        quest.Objectives.Add(new QuestObjectiveDefinition { Key = "return", Title = "Hand in the bottle", ItemId = bottle.Id, Count = 1 });
        quest.NextDefinition = new QuestDefinition { Key = "reward", Title = "Collect the reward", Description = "The bottle was returned." };
        quest.NextDefinition.Objectives.Add(new QuestObjectiveDefinition { Key = "claim", Title = "Claim the reward" });

        QuestRefusal refusal;
        if (QuestAuthoring.Create(Mod.Id, quest, out refusal) == null) Log.Warn("The quest was refused: " + refusal);
    }

    private void Update()
    {
        if (UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (Pressed(searchKey)) Quests.AddProgress(taskId, "search", 1);
        if (Pressed(handInKey)) HandIn();
        if (Pressed(jumpKey)) Log.Info("JumpTo: " + QuestAuthoring.JumpTo(Mod.Id, taskId, "return"));
        if (Pressed(restartKey)) Log.Info("Restart: " + QuestAuthoring.Restart(Mod.Id, taskId));
        if (Pressed(resetKey)) Log.Info("Reset: " + QuestAuthoring.Reset(Mod.Id, taskId));
        if (Pressed(removeKey) && QuestAuthoring.Remove(Mod.Id, taskId)) Register();
    }

    private void HandIn()
    {
        if (!QuestAuthoring.HandIn(Mod.Id, taskId, "return"))
        {
            Log.Warn("The bottle was not handed in. The step must be active and the bottle must be in the inventory.");
            return;
        }
        if (Quests.State(taskId, "search") == ObjectiveState.Finished) QuestAuthoring.Finish(Mod.Id, taskId);
    }

    private void OnTaskChanged(TaskChangedArgs args)
    {
        if (args.TaskId.StartsWith(Mod.Id + ".")) Log.Info(args.TaskId + " is " + args.State);
    }

    private static bool Pressed(ModKeybind key)
    {
        return key != null && key.Down();
    }
}
```

The keys are listed in the Mods menu under Mod Keybinds. F6 counts one search, F7 hands in the bottle and finishes the quest once the search is done, F8 jumps to the hand-in step, F9 restarts a finished quest, F10 resets it from any state, and F11 removes it and makes it again. `Items.Find` returns `null` for a title the game does not have, so the quest is not made and the log says so. `Register` runs at every load, and the `Quests.Get` check keeps it from being made twice.

The follow-up quest is stored with the bottle quest through `NextDefinition`, so it starts by itself when the bottle quest finishes, even after a load. `Quests.TaskChanged` logs each change of your quests, which is how you see the follow-up start.

## Self-check

Set `Debug.QuestsSelfCheck` in the framework's config to log a check of the counter event rule and the ready retry bookkeeping at start. Set `Debug.QuestAuthoringSelfCheck` in the framework's config to log a check of the definition rules, the hidden objective text rule, the reveal state rule and the owner rule at start. Set `Debug.QuestAuthoringCommand` to register the `core_quests_check` console command, which creates, advances and finishes a test quest in the loaded save and logs the result, and the `core_quest reset` and `core_quest skip` commands in [Console commands](#console-commands). It adds a finished task to that save, so use a scratch save. The rule that rebuilds the game's ten id lists is checked offline in the framework's own test harness, before a release.
