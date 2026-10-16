---
title: Story API (Quests, Dialogue, Law)
parent: API reference
nav_order: 94
---

# Story API (Quests, Dialogue, Law)

Experimental.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono.
Namespace: `ObenseuerCore`. Log owners: `obenseuer.moddingframework.quests`, `obenseuer.moddingframework.dialogue`, `obenseuer.moddingframework.law`. Logs go to `BepInEx\logs\<owner>.log`.

Everything not listed here is internal. Items marked **experimental** may change.

## Rules for all three facades

- **Availability.** `IsAvailable` is false when the facade's patches were not installed, when `Core.PatchingAllowed` is false (unsupported build), or when its owner was disabled after a failed core patch. Reads then return the fallback (`""`, `0`, `false`, `null`, or an empty array). Writers return `false`.
- **Bad arguments.** Registration arguments throw `ArgumentException` or `ArgumentNullException`, before the availability check: a null or empty `modId`, a null handler or method, an invalid Lua name, or a target that is not an instance of the method's class. Game-data arguments (ids, names, values, amounts) never throw. An invalid one makes the call return `false`, `null` or the fallback, and the refusal is logged once per owner, member and reason.
- **Game failures.** When a call into the game throws, the call returns its fallback. The first throw of each member is logged. Later throws of that member are not logged again.
- **Events are deferred.** A story event is queued during the game's own call and raised from `LateUpdate` of a `DontDestroyOnLoad` host object that the facades create. Handlers run outside the game's call, so they may call writers. Events are raised in the order they were queued. An event queued while events are being raised waits for the next frame. A handler that throws is logged, and the other handlers still run.
- **Intercepts are synchronous.** `Intercept` handlers run inside the game's call, in registration order, and may cancel or redirect it. A call made from inside an intercept handler skips the intercept handlers, so that game call goes through unchanged. A handler that throws is logged and skipped.
- **After a load.** Loading a save replaces the dialogue variable table. A mod that needs Lua variables must set them again on `Events.Loaded`, which fires when a save finishes loading. Lua functions registered with `RegisterFunction` live in memory, so register them again after each start.
- **Removing a mod.** `RemoveAll(modId)` removes that mod's intercepts and its responses from `AddResponse`. Dialogue also unregisters that mod's Lua functions.
- **Saves.** Write calls in Money, Inventory, Containers, Quests, Dialogue, Law and the world-object APIs return false while the game is loading or saving. A story event queued before a load starts is dropped when the load is of another character, when the main menu opens, or when a new game starts. A level change of the same character keeps it, because the game saves and reloads the same character's save. Story events queued while a save or load runs are raised once it ends.

## Quests (experimental)

```csharp
public enum TaskState { Active = 0, Failed = 1, Finished = 2, Abandoned = 3 }
public enum ObjectiveState { Active = 0, Failed = 1, Finished = 2, Abandoned = 3, Hidden = 4 }

public sealed class QuestInfo
{
    public string Id { get; }                   // see Ids below
    public string Title { get; }
    public string Description { get; }
    public TaskState State { get; }
    public ObjectiveInfo[] Objectives { get; }
}

public sealed class ObjectiveInfo
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public ObjectiveState State { get; }
    public bool Optional { get; }
    public int Current { get; }                 // counter value
    public int Target { get; }                  // counter target, 0 when the objective has no counter
}

public static class Quests
{
    public static event Action<TaskChangedArgs> TaskChanged;                    // args.TaskId, args.State
    public static event Action<ObjectiveChangedArgs> ObjectiveChanged; // args.TaskId, args.ObjectiveId, args.State
    public static bool IsAvailable { get; }
    public static List<QuestInfo> All();
    public static QuestInfo Get(string taskId);
    public static bool Reached(string taskId, string objectiveId);
    public static bool SetTask(string taskId, TaskState state);
    public static bool SetObjective(string taskId, string objectiveId, ObjectiveState state);
    public static bool AddProgress(string taskId, string objectiveId, int amount);
}
```

**Ids.** A task id is the game's `TaskInfo.Id`: its `id` field, or its addressable asset path when `id` is empty, or its title when both are empty. An objective id is `TaskObjective.Id`, with the same order. `QuestInfo.Id` and `ObjectiveInfo.Id` use these values.

A task id or objective id must match exactly one entry. Zero or several matches are refused. A task whose title matches another task is refused as well, because the game treats such tasks as the same task (`TaskInfo.Compare`).

**Reads.**
- `All()` returns every started task, finished and failed ones included. It never calls the game's `GetTaskAndObjective`, which finishes the objective it looks up.
- `Get(taskId)` returns `null` for an empty, unknown or ambiguous id, and when the facade is not available.
- `Reached(taskId, objectiveId)` returns `true` when the game recorded the objective as Active, Failed or Abandoned, or as Finished with its completion requirements met. The record stays after the objective closes. It returns `false` for an empty id, an unknown id, and when the facade is not available. It reads the game's own record and does not check that the ids match one task and one objective. The game stores the pair as `taskId + "_" + objectiveId`, so ids that contain `_` can collide.

**Writes.**
- `SetTask(taskId, state)` accepts `Failed`, `Finished` and `Abandoned`. The task must be Active. Returns `true` when the task has the new state after the call.
- **Experimental: `SetTask(taskId, Finished)` does not run the game's own completion effects or the task's follow-up tasks.** The game's `TaskController.EditTaskStatus` only changes the task's status and moves it to the finished list. It marks each still-active objective Abandoned, which runs that objective's abandon follow-ups.
- `SetObjective(taskId, objectiveId, state)` accepts `Failed`, `Finished` and `Abandoned`. The objective must be Active. `Finished` also needs the game's complete requirements to pass. Returns `true` when the objective has the new state after the call.
- `AddProgress(taskId, objectiveId, amount)` needs `amount` above 0 and an Active objective with a counter. The amount is clamped to the room left on the counter, so the counter never passes its target. When the add reaches the target, the objective completes through the game unless it is set not to complete by its counter. In that case the complete requirements must pass first, or the call returns `false` and changes nothing. Returns `true` when the counter or the status changed.
- Writers refuse while a quest hook is running. A quest hook is one of the game's own `EditTask`, `EditTaskStatus` or `EditTaskObjective` calls, including calls made from another mod's Harmony patch. The refusal returns `false` and is logged once as "called while a quest hook is running". Event handlers run after the hook, so they may call writers.
- Writers also need `TaskPanel.instance` and `Notifications.instance`, which the game uses when a task starts.

**Events.** Fired from the game's own changes, queued and raised as described above.
- `TaskChanged(args)`: a new task started (`Active`), or a task's status moved to `Failed`, `Finished` or `Abandoned`.
- `ObjectiveChanged(args)`: an objective started (`Active`), or an objective's status changed.
- Order: the game's nested changes are queued first. For a new task with an objective, `ObjectiveChanged(Active)` comes before `TaskChanged(Active)`. A task closed by `SetTask` raises one `TaskChanged` with the closed state. A task created and closed in one game call raises only the closing event.
- An event carries the state recorded when the change happened. Call `Get` for the state at handling time.

Example (ids come from the game; the ids here are placeholders):

```csharp
Quests.TaskChanged += args => CoreLog.For("mymod").Info(args.TaskId + " is " + args.State);
bool done = Quests.SetObjective("<taskId>", "<objectiveId>", ObjectiveState.Finished);
```

## Dialogue

```csharp
public delegate void ConversationHandler(ConversationArgs args);

public sealed class ConversationArgs
{
    public string Title { get; set; }           // set to redirect the conversation
    public Transform Actor { get; }
    public Transform Conversant { get; }
    public bool Cancel { get; set; }            // true stops the conversation (experimental)
}

public static class Dialogue
{
    public static event Action<DialogueArgs> Started; // args.Title, conversation title
    public static event Action<DialogueArgs> Ended;   // args.Title, conversation title
    public static bool IsAvailable { get; }
    public static bool IsActive { get; }        // a conversation is running
    public static string CurrentTitle { get; }  // "" when no conversation is running
    public static bool Exists(string title);    // the title is in the master database
    public static bool HasVariable(string name);
    public static string GetVariableString(string name, string fallback);
    public static float GetVariableNumber(string name, float fallback);
    public static bool GetVariableBool(string name, bool fallback);
    public static bool SetVariable(string name, object value);
    public static string GetActorField(string actor, string field, string fallback);
    public static string RegisterFunction(string modId, string name, object target, MethodInfo method);
    public static bool Start(string title, Transform actor, Transform conversant);   // experimental
    public static void Stop();                                                       // experimental
    public static bool Narrate(string modId, string text);                           // experimental, see dialogue authoring
    public static void Intercept(string modId, ConversationHandler handler);
    public static string AddResponse(string modId, string conversationTitle, string anchorText, string playerText, string npcReply, string luaCondition, Action onPicked);   // experimental
    public static string AddResponse(string modId, string conversationTitle, int anchorId, string playerText, string npcReply, string luaCondition, Action onPicked);   // experimental
    public static bool RemoveResponse(string modId, string key);   // experimental
    public static void RemoveAll(string modId);
}
```

**Variables.** `HasVariable`, `GetVariableString`, `GetVariableNumber` and `GetVariableBool` read the Lua variables. A typed read returns the fallback when the variable is missing or holds another Lua type, so `GetVariableString` returns the fallback for a number. `GetActorField` returns the field only when it holds a string.

`SetVariable(name, value)` returns `true` when the variable was written. It returns `false`, and logs once, when:
- `name` does not match `^[A-Za-z_][A-Za-z0-9_]*$`. A trailing newline is refused too;
- `name` is a Lua keyword: `and break do else elseif end false for function if in local nil not or repeat return then true until while`;
- `name` is written by the game: `Actor`, `ActorIndex`, `Alert`, `Conversant`, `ConversantIndex`, `Conversation`, `Converrsant` (the game's spelling), `Currency`, `Day`, `PlayerAge`, `PlayerFirstName`, `PlayerGender`, `PlayerLastName`, `PoliceIdle`, `Price`, `SavedLevelName`, `Speaker`, `WakeUpReason`, or it starts with `DREAM_`;
- `value` is a string that contains a backslash or a control character (`char.IsControl`: includes NUL, CR, LF and TAB). Double quotes are accepted;
- `value` is NaN or infinite, or a double too large for a float;
- `value` is not a string, bool, int, float or double.

The game saves dialogue variables in `Globals.dialogue` as Lua text, and a bad name or value there makes the whole save fail to load. Numbers are stored as Lua floats, so integers above 2^24 lose precision. Prefix variable names with your mod id, for example `mymod_visits`.

**Conversations.**
- `Start(title, actor, conversant)` (experimental) returns `false` when a conversation is running, `title` is empty, or the game is loading or saving. It starts the conversation through `InteractableTalk.StartDialogue` when the conversant has that component, and through `DialogueManager.StartConversation` otherwise. It returns whether a conversation is running after the call, because the game can refuse to start one, for example when no dialogue UI is present.
- `Stop()` (experimental) ends all running conversations through the Dialogue System's `StopAllConversations`, also while a save loads or saves (`SaveGate.Busy`), so it is never refused. It may leave the conversant mid-dialogue: the state that `InteractableTalk.CheckStartDialogueInterrupt` set when the talk started (`PlayerStats.Active`, the control locks, the dialogue registration) is cleared only when the game's close callback (`DialogueController.OnDialogueClosed`) runs.
- `Intercept(modId, handler)` runs before every conversation start, whether the game or this API starts it. The handler runs in a prefix on `DialogueSystemController.StartConversation`, before the conversation starts, so database edits made in the handler are in place when the conversation begins. `args.Title` redirects the conversation, and `args.Cancel` stops it. A redirect is used only when `Exists(title)` is true. Otherwise the original title is kept and the refusal is logged once. Handlers run in registration order. Any `Cancel` wins: once a handler sets it, the later handlers still run and see `Cancel` as `true`, and setting it back to `false` has no effect. `Title` is last-writer-wins and is ignored once cancelled. Registering the same handler for the same mod again does nothing.
- **Cancel (experimental).** A cancelled start skips the game's `StartConversation`, so the code that asked for the conversation gets none. Known risks: a BehaviorDesigner task that waits for `waitForConversationCompletion` can stall; cutscene starters that wait for a conversation can freeze; `InteractableTalk` changes its own state before it starts a conversation (the game's `InteractableTalk` class), and its `DelayCheck` repairs that state. Cancel only where you know the starter.
- `Started(title)` and `Ended(title)` come from the game's `OnConversationStart` and `OnConversationEnd` messages. They fire for every conversation, including those the game starts itself. A message can reach the object twice in some setups. `CurrentTitle` is `""` whenever `IsActive` is false.

**Lua functions.** `RegisterFunction` registers the Lua global `Mod_<prefix>_<name>` and returns that name. `prefix` is the mod id with each character that is not an ASCII letter or digit replaced by `_`. When that replacement changes the id, the prefix also gets `_` and an 8-digit hex FNV-1a hash of the original mod id. So `a.b` and `a_b` never collide, and the name does not depend on which mod loads first. `name` must be a Lua identifier. Parameters must be `float`, `string` or `bool`, because Lua numbers arrive as `float`. `target` is `null` for a static method, and an instance of the method's class otherwise. Registering a name again replaces the earlier function. Returns `null` when the facade is not available.

The framework registers five functions when Dialogue is available, at startup and after each scene load. They are not in the `Mod_` namespace.
- `Core_QuestState(taskId)` returns `"Active"`, `"Failed"`, `"Finished"`, `"Abandoned"` or `""`.
- `Core_ObjectiveState(taskId, objectiveId)` returns the objective state name, or `""`.
- `Core_CrimeTotal()` returns `Law.Total`.
- `Core_CrimeAgainst(npcId)` returns `Law.GetAgainst(npcId)`. `npcId` is a number.
- `Core_ResponsePicked(key)` runs the `onPicked` of the response with that key. The framework's response lines call it, so a mod does not.

Example:

```csharp
Dialogue.Intercept("mymod", args => { if (args.Title == "mymod/Greeting") args.Title = "mymod/Greeting2"; });
Dialogue.SetVariable("mymod_visits", 3);
string lua = Dialogue.RegisterFunction("mymod", "HasKey", null, typeof(MyMod).GetMethod("HasKey"));
// in a condition: Mod_mymod_HasKey("<taskId>")
```

## Law (experimental for Arrest)

```csharp
public enum CrimeKind
{
    Unspecified = 0, Theft = 1, Violence = 2, Fraud = 3, PrisonEscape = 4, Administrative = 5,
    AnimalCruelty = 6, Trespassing = 7, Burglary = 8, None = 9, Harassment = 10, Vandalism = 11, Terrorism = 12
}

public delegate void CrimeHandler(CrimeArgs args);

public sealed class CrimeArgs
{
    public CrimeKind Kind { get; set; }
    public int Severity { get; set; }
    public int VictimId { get; }                // victim NPC id, -1 when there is no victim
    public bool Silent { get; set; }            // true suppresses the crime notification
    public bool Cancel { get; set; }            // true drops the crime
}

public sealed class CommittedArgs
{
    public CrimeKind Kind { get; }
    public int Severity { get; }                // recorded severity, at least 1
    public int VictimId { get; }                // -1 when there is no victim
}

public static class Law
{
    public static event Action<CommittedArgs> Committed;
    public static event Action Spotted;         // a crime witness saw the player, at most once per frame
    public static event Action<FineArgs> Fined;      // args.Amount: RM due before payment, rounded
    public static event Action Arrested;        // the player was sent to prison
    public static event Action Released;        // the player left prison
    public static bool IsAvailable { get; }
    public static float Total { get; }          // all crimes, including carried stolen goods
    public static float TotalFromRecords { get; } // crime records only
    public static int FineRM { get; }           // RM fine, rounded
    public static int SentenceHours { get; }    // sentence length, rounded
    public static int TimesInJail { get; }
    public static bool IsArrestInProgress { get; }
    public static bool HasStolenGoods { get; }  // carried stolen goods, false when the inventory is not loaded
    public static int StolenGoodsValue { get; } // carried stolen goods minus permitted items, 0 to int.MaxValue
    public static float GetGeneric(CrimeKind kind);  // amount of the generic records of that kind
    public static float GetAgainst(int npcId);       // amount recorded against one NPC, 0 for ids of 0 or less
    public static bool Commit(CrimeKind kind, int severity, int victimNpcId, bool silent);
    public static bool Clear();                 // all crime records
    public static bool Clear(int victimNpcId);  // one victim's records, across every crime type
    public static bool Clear(CrimeKind kind);   // the first generic record of that kind
    public static bool Clear(int victimNpcId, CrimeKind kind);  // the first record of that kind against one victim
    public static bool Arrest(float severityFactor);  // experimental
    public static void Intercept(string modId, CrimeHandler handler);
    public static void RemoveAll(string modId);
}
```

**Reads.** Reads return 0 or false when the crime system is not loaded (main menu) or the call fails.
- `Total` includes carried stolen goods. `TotalFromRecords` counts crime records only. `FineRM` is the fine, rounded.
- `HasStolenGoods` and `StolenGoodsValue` read the stolen goods the player carries. `StolenGoodsValue` leaves out goods with a permitted owner, and it is 0 when the inventory is not loaded.
- `SentenceHours` is the game's sentence formula, computed from the crime fields: the crime count without stolen goods divided by `crimePerPrisonHour`, rounded, then clamped to 1 through 100 plus 12 for each earlier jail term. `TimesInJail` counts the earlier jail terms.

**Crimes.**
- `Commit(kind, severity, victimNpcId, silent)` records a crime through the game's `CommitCrime`, which every crime goes through. It returns `true` when the crime was recorded. It returns `false` and records nothing when the crime system is not loaded, `kind` is `None` or undefined, `severity` is outside 1 through 1000000, the achievements are not loaded, an intercept cancels the crime, or the game call throws. `victimNpcId` above 0 names the victim. 0 means no victim.
- `Clear()` clears all crime records. `Clear(victimNpcId)` clears one victim's records across every crime type, and returns `false` for an id of 0 or less. Both return `false` while an arrest is in progress or a level change is active.
- `Clear(kind)` clears the first generic record of that kind, the one `GetGeneric` sums, and leaves victim records alone. `Clear(victimNpcId, kind)` clears the first record of that kind against that victim, and leaves generic records alone. Both return `false` for a `None` or undefined kind, and `Clear(victimNpcId, kind)` also returns `false` for an id of 0 or less. Both refuse under the same conditions as `Clear()`. They reset the game's bribe flag even when no record matches, so `true` means the game call ran, not that a record was cleared.
- `Intercept(modId, handler)` runs for every crime, before it is recorded. In `CrimeArgs`, `Kind` can be changed (a `None` or undefined value keeps the game's kind), `Severity` is clamped to 1 through 1000000 only when the handler changed it, and `Silent` and `Cancel` can be set. A cancelled crime is not recorded, and `Committed` does not fire. Handlers run in registration order. Any `Cancel` wins: once a handler sets it, the later handlers still run and see `Cancel` as `true`, and setting it back to `false` has no effect. `Kind`, `Severity` and `Silent` are last-writer-wins and are ignored once cancelled. Registering the same handler for the same mod again does nothing.
- `Committed(crime)` fires for each crime that was recorded. Its `Severity` is the recorded value, at least 1.

**Arrest (experimental).** `Arrest(severityFactor)` sends the player to prison through the game's `Crime.GoToPrison`. `severityFactor` must be above 0 and at most 100. It returns `false` and does nothing when:
- an arrest is in progress, or a level change is active;
- a conversation is open (`DialogueManager.isConversationActive` or `DialogueController.instance.DialogueActive`);
- the player is already in prison (`Prison.inPrison`, read through reflection);
- the achievements, player stats, waiting controller or inventory are not loaded;
- `Total` times `severityFactor` would not fit an int.

A successful call has these effects, all from the game's `GoToPrison`:
- a Theft record of 10% of the stolen goods the player carries is added, when there are any;
- every crime record's amount becomes `severityFactor` times its value, rounded. This is permanent;
- `timesInJail` increases by one, which raises the sentence cap by 12 hours;
- police pursuits stop, the `ARRESTED` achievement is triggered, tiredness is set to at least 10, the player passes out, and the arrest sleep event is scheduled 10 seconds later.

If the game throws during the call, the framework clears the arrest-in-progress flag, and `Arrest` returns `false`.

**Events.**
- `Spotted` fires when `Witness.IsWitnessed` returns true for a crime witness, only for calls with `invoke` set, at most once per frame.
- `Fined` (payload `FineArgs`, `Amount`) fires when the game's `PayFine` succeeds, with the rounded amount that was due before payment.
- `Arrested` fires when the game sends the player to prison (`Crime.OnGoingToPrison`).
- `Released` fires when the player leaves prison.

Example:

```csharp
Law.Intercept("mymod", args => { if (args.Kind == CrimeKind.Theft && args.Severity < 3) args.Cancel = true; });
bool recorded = Law.Commit(CrimeKind.Vandalism, 5, 0, false);
int owed = Law.FineRM;
```

## Not in this version

- Quests and dialogue authoring are not on this page. See [Quest authoring](api/quest-authoring.md) and [Dialogue authoring](api/dialogue-authoring.md).
- Law: `SetFineMultiplier`, `SetPrisonDrainPerHour`. They would write game fields whose lifecycle is unverified.
