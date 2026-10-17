---
title: Dialogue
parent: Quests, dialogue and law
grand_parent: Story
nav_order: 4
---

# Dialogue

This page uses `Transform` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

The game's conversations are run by a dialogue system that keeps variables and runs small scripts written in Lua (a scripting language). `Dialogue` lets your mod read and set those variables, start and stop a conversation, redirect or cancel a conversation when it starts, add answer lines, and add your own functions that conversation scripts can call.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Dialogue</code> <span>variables, conversations and answer lines</span></p>

Use `Start` to begin a conversation, `SetVariable` and the `GetVariable` calls to work with the Lua variables, and `Intercept` to run your code before each conversation starts.

<div class="api-pair" markdown="1">

```csharp
public static class Dialogue
{
    public static event Action<DialogueArgs> Started;
    public static event Action<DialogueArgs> Ended;
    public static bool IsAvailable { get; }
    public static bool IsActive { get; }
    public static string CurrentTitle { get; }
    public static bool Exists(string title);
    public static bool HasVariable(string name);
    public static string GetVariableString(string name, string fallback);
    public static float GetVariableNumber(string name, float fallback);
    public static bool GetVariableBool(string name, bool fallback);
    public static bool SetVariable(string name, object value);
    public static string GetActorField(string actor, string field, string fallback);
    public static string RegisterFunction(string modId, string name, object target, MethodInfo method);
    public static bool Start(string title, Transform actor, Transform conversant);
    public static void Stop();
    public static bool Narrate(string modId, string text);
    public static void Intercept(string modId, ConversationHandler handler);
    public static string AddResponse(string modId, string conversationTitle, string anchorText, string playerText, string npcReply, string luaCondition, Action onPicked);
    public static string AddResponse(string modId, string conversationTitle, int anchorId, string playerText, string npcReply, string luaCondition, Action onPicked);
    public static bool RemoveResponse(string modId, string key);
    public static void RemoveAll(string modId);
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when the Dialogue API works. |
| `IsActive` | `bool` | `true` while a conversation is running. |
| `CurrentTitle` | `string` | The running conversation's title. `""` when none is running. |

| Method | Returns | What it does |
|---|---|---|
| `Exists(title)` | `bool` | `true` when a conversation with this title exists in the game's dialogue database. |
| `HasVariable(name)` | `bool` | `true` when this Lua variable exists. |
| `GetVariableString(name, fallback)` | `string` | Reads a text variable. |
| `GetVariableNumber(name, fallback)` | `float` | Reads a number variable. |
| `GetVariableBool(name, fallback)` | `bool` | Reads a yes-or-no variable. |
| `SetVariable(name, value)` | `bool` | Writes a variable. `true` when it was written. |
| `GetActorField(actor, field, fallback)` | `string` | Reads a text field of a character in the dialogue database. |
| `RegisterFunction(modId, name, target, method)` | `string` | Adds a Lua function, and gives back its Lua name. |
| `Start(title, actor, conversant)` | `bool` | Starts a conversation (experimental). |
| `Stop()` | `void` | Stops the running conversations (experimental). |
| `Narrate(modId, text)` | `bool` | Shows narration text (experimental, see Dialogue authoring). |
| `Intercept(modId, handler)` | `void` | Runs your code before every conversation starts. |
| `AddResponse(modId, conversationTitle, anchorText, ...)` | `string` | Adds an answer line, placed by the text of a line (experimental). |
| `AddResponse(modId, conversationTitle, anchorId, ...)` | `string` | Adds an answer line, placed by the id of a line (experimental). |
| `RemoveResponse(modId, key)` | `bool` | Removes an answer line you added (experimental). |
| `RemoveAll(modId)` | `void` | Removes everything your mod added here. |

| Event | Gives you | When it fires |
|---|---|---|
| `Started` | `DialogueArgs` | A conversation started. `args.Title` is its title. |
| `Ended` | `DialogueArgs` | A conversation ended. `args.Title` is its title. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ConversationHandler</code> <span>your code that runs before a conversation starts</span></p>

This is the delegate you pass to `Dialogue.Intercept`. The framework calls it before each conversation starts, with the `ConversationArgs` for that conversation.

<div class="api-pair" markdown="1">

```csharp
public delegate void ConversationHandler(ConversationArgs args);
```

| Parameter | Type | What it means |
|---|---|---|
| `args` | `ConversationArgs` | The conversation about to start. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ConversationArgs</code> <span>a conversation that is about to start</span></p>

`Intercept` hands you this before a conversation starts. Set `Title` to send the player to another conversation. Setting `Cancel` stops it, and that is experimental.

<div class="api-pair" markdown="1">

```csharp
public sealed class ConversationArgs
{
    public string Title { get; set; }
    public Transform Actor { get; }
    public Transform Conversant { get; }
    public bool Cancel { get; set; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Title` | `string` | The conversation's title. Set it to send the player to a different conversation. |
| `Actor` | `Transform` | The person who starts the conversation. |
| `Conversant` | `Transform` | The person who is spoken to. |
| `Cancel` | `bool` | `true` stops the conversation (experimental). |

</div>
</div>

## Putting it together

Sends one conversation to another, sets a variable, and registers a Lua function that checks a quest step.

<div class="api-example" markdown="1">

```csharp
using System.Reflection;
using ObenseuerCore;

[ModInfo("example.dialoguetour", "Dialogue Tour", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class DialogueTour : FrameworkMod
{
    protected override void OnLoad()
    {
        Dialogue.Intercept("mymod", args => { if (args.Title == "mymod/Greeting") args.Title = "mymod/Greeting2"; });
        Dialogue.SetVariable("mymod_visits", 3);
        string lua = Dialogue.RegisterFunction("mymod", "HasKey", null, typeof(DialogueTour).GetMethod("HasKey"));
        Log.Info("Lua name: " + lua);
    }

    public static bool HasKey(string taskId)
    {
        return Quests.Reached(taskId, "key");
    }
}
```

| Step | What it does |
|---|---|
| `Dialogue.Intercept("mymod", ...)` | Sends the player from `mymod/Greeting` to `mymod/Greeting2` when the conversation starts. |
| `Dialogue.SetVariable("mymod_visits", 3)` | Writes a Lua variable the conversation scripts can read. |
| `Dialogue.RegisterFunction("mymod", "HasKey", null, ...)` | Adds `HasKey` as a Lua function, and gives back its Lua name. |
| `Log.Info("Lua name: " + lua)` | Logs the Lua name, so you can check it. |

</div>

In a conversation condition you would then write `Mod_mymod_HasKey("<taskId>")`.

## Things to know

### Variables

- `HasVariable`, `GetVariableString`, `GetVariableNumber` and `GetVariableBool` read the Lua variables, and give you your fallback if the variable is not there. A typed read returns the fallback when the variable is missing or holds another type, so `GetVariableString` returns the fallback for a number.
- `GetActorField` returns a field only when it holds text. Otherwise you get the fallback.
- `SetVariable(name, value)` returns `true` when the variable was written, and `false` (logged once) when the name or the value is not allowed. It returns `false` when:
  - the `name` does not match `^[A-Za-z_][A-Za-z0-9_]*$` (a name must start with a letter or `_`, and have only letters, digits and `_`). A newline at the end is refused too;
  - the `name` is a Lua keyword: `and break do else elseif end false for function if in local nil not or repeat return then true until while`;
  - the `name` is one the game writes itself: `Actor`, `ActorIndex`, `Alert`, `Conversant`, `ConversantIndex`, `Conversation`, `Converrsant` (the game's own spelling), `Currency`, `Day`, `PlayerAge`, `PlayerFirstName`, `PlayerGender`, `PlayerLastName`, `PoliceIdle`, `Price`, `SavedLevelName`, `Speaker`, `WakeUpReason`, or any name that starts with `DREAM_`;
  - the `value` is a string that contains a backslash or a control character (such as NUL, a new line, a carriage return or a tab). Double quotes are fine;
  - the `value` is not a number (NaN), is infinite, or is a double too large for a float;
  - the `value` is not a string, bool, int, float or double.
- A bad variable name or value can make the whole save fail to load, so the framework refuses them. The game stores the variables in the save as Lua text, and the game cannot read a bad name or value back. <span class="tech">(The game saves dialogue variables in `Globals.dialogue` as Lua text.)</span>
- Numbers are stored as floats, so whole numbers above 16,777,216 lose precision. That is 2^24.
- Start your variable names with your mod's name, for example `mymod_visits`. That keeps them apart from the game's and from other mods'.

### Starting and stopping

- `Start(title, actor, conversant)` (experimental) starts a conversation and returns whether one is running afterwards. It returns `false` when a conversation is already running, `title` is empty, or the game is loading or saving. The game can refuse to start a conversation, for example when there is no dialogue screen, so check the result. <span class="tech">(It starts the conversation through `InteractableTalk.StartDialogue` when the conversant has that component, and through `DialogueManager.StartConversation` otherwise.)</span>
- `Stop()` (experimental) ends every running conversation, and it is never refused. It works even while a save loads or saves. It may leave the person you spoke to in the middle of the conversation, because the game's cleanup runs only when the game's own close step happens. <span class="tech">(It calls the Dialogue System's `StopAllConversations`, also while `SaveGate.Busy`. The state that `InteractableTalk.CheckStartDialogueInterrupt` set when the talk started (`PlayerStats.Active`, the control locks, the dialogue registration) is cleared only when `DialogueController.OnDialogueClosed` runs.)</span>
- `Started(title)` and `Ended(title)` fire for every conversation, including ones the game starts itself. A message can reach the object twice in some setups. `CurrentTitle` is `""` whenever `IsActive` is `false`. <span class="tech">(They come from the game's `OnConversationStart` and `OnConversationEnd` messages.)</span>

### Changing before start

- `Intercept(modId, handler)` runs before every conversation starts, whether the game or this API starts it. Your changes to the game's dialogue database are in place by the time the conversation begins. Handlers run in the order they were added, and adding the same handler for the same mod again does nothing. <span class="tech">(The handler runs in a prefix on `DialogueSystemController.StartConversation`.)</span>
- Setting `args.Title` sends the player to a different conversation. The new title is used only when `Exists(title)` is `true`. Otherwise the original title stays and the refusal is logged once. `Title` is last-writer-wins: the last handler to set it counts. It is ignored once the conversation is cancelled.
- Setting `args.Cancel = true` stops the conversation, and nobody can undo it. Any `Cancel` wins: once a handler sets it, the later handlers still run and see `Cancel` as `true`, and setting it back to `false` has no effect.
- Experimental: cancelling a conversation can leave the game waiting for it. A cancelled start skips the game's `StartConversation`, so the code that asked for the conversation gets none. Cancel only where you know what started it. Known risks:
  - a behavior tree step that waits for the conversation to finish can stall. <span class="tech">(The stalling task is a BehaviorDesigner task with `waitForConversationCompletion`.)</span>
  - a cutscene starter that waits for a conversation can freeze;
  - the talk interaction changes its own state before it starts a conversation, and a later check repairs it. <span class="tech">(The talk interaction is the game's `InteractableTalk` class, and its `DelayCheck` repairs the state.)</span>

### Lua functions

- `RegisterFunction` adds a function the game's conversation scripts can call, and gives back its name. The Lua name is `Mod_<prefix>_<name>`. The `prefix` is your mod id with each character that is not an ASCII letter or digit replaced by `_`. When that replacement changes the id, the prefix also gets `_` and an 8-digit hex hash of the original id, so `a.b` and `a_b` never collide, and the name does not depend on which mod loads first. It returns `null` when the API is not available. <span class="tech">(The hash is FNV-1a.)</span>
- `name` must be a valid Lua name, and the function's parameters must be `float`, `string` or `bool`. Lua numbers arrive as `float`.
- `target` is `null` for a static method, and an object of the method's class otherwise.
- Registering a name again replaces the earlier function.
- The framework adds six functions of its own, which are not in the `Mod_` namespace. It adds them when `Dialogue` is available, at start and after each scene load.
  - `Core_QuestState(taskId)` returns `"Active"`, `"Failed"`, `"Finished"`, `"Abandoned"` or `""`.
  - `Core_ObjectiveState(taskId, objectiveId)` returns the state name of the step, or `""`.
  - `Core_CrimeTotal()` returns `Law.Total`.
  - `Core_CrimeAgainst(npcId)` returns `Law.GetAgainst(npcId)`. `npcId` is a number.
  - `Core_ResponsePicked(key)` runs the `onPicked` of the answer line with that key. The framework's own answer lines call it, so your mod does not.
  - `Core_LineText(id)` returns the text of a line whose text a mod gave as a callback. The framework's own dialogue lines call it, so your mod does not.
