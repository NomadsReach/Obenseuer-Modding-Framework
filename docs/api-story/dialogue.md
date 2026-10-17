---
title: Dialogue
parent: Quests, dialogue and law
grand_parent: Story
nav_order: 4
---

# Dialogue

The game's conversations are run by a dialogue system that keeps variables and runs small scripts written in Lua (a scripting language). `Dialogue` lets your mod read and set those variables, start and stop a conversation, redirect or cancel a conversation when it starts, add answer lines, and add your own functions that conversation scripts can call.

```csharp
public delegate void ConversationHandler(ConversationArgs args);   // your code that runs before a conversation starts

public sealed class ConversationArgs    // a conversation that is about to start
{
    public string Title { get; set; }           // the conversation's title; set it to send the player to a different conversation
    public Transform Actor { get; }             // the person who starts the conversation
    public Transform Conversant { get; }        // the person who is spoken to
    public bool Cancel { get; set; }            // true stops the conversation (experimental)
}

public static class Dialogue
{
    public static event Action<DialogueArgs> Started; // a conversation started; args.Title is its title
    public static event Action<DialogueArgs> Ended;   // a conversation ended; args.Title is its title
    public static bool IsAvailable { get; }           // true when the Dialogue API works
    public static bool IsActive { get; }              // true while a conversation is running
    public static string CurrentTitle { get; }        // the running conversation's title; "" when none is running
    public static bool Exists(string title);          // true when a conversation with this title exists in the game's dialogue database
    public static bool HasVariable(string name);                                  // true when this Lua variable exists
    public static string GetVariableString(string name, string fallback);         // read a text variable
    public static float GetVariableNumber(string name, float fallback);           // read a number variable
    public static bool GetVariableBool(string name, bool fallback);               // read a yes-or-no variable
    public static bool SetVariable(string name, object value);                    // write a variable; true when it was written
    public static string GetActorField(string actor, string field, string fallback);   // read a text field of a character in the dialogue database
    public static string RegisterFunction(string modId, string name, object target, MethodInfo method);   // add a Lua function; gives back its Lua name
    public static bool Start(string title, Transform actor, Transform conversant);   // start a conversation (experimental)
    public static void Stop();                                                       // stop the running conversations (experimental)
    public static bool Narrate(string modId, string text);                           // show narration text (experimental, see Dialogue authoring)
    public static void Intercept(string modId, ConversationHandler handler);         // run your code before every conversation starts
    public static string AddResponse(string modId, string conversationTitle, string anchorText, string playerText, string npcReply, string luaCondition, Action onPicked);   // add an answer line, placed by the text of a line (experimental)
    public static string AddResponse(string modId, string conversationTitle, int anchorId, string playerText, string npcReply, string luaCondition, Action onPicked);      // add an answer line, placed by the id of a line (experimental)
    public static bool RemoveResponse(string modId, string key);   // remove an answer line you added (experimental)
    public static void RemoveAll(string modId);                    // remove everything your mod added here
}
```

**Variables.**

- **The `GetVariable...` calls read a variable and give you your fallback if it is not there.** `HasVariable`, `GetVariableString`, `GetVariableNumber` and `GetVariableBool` read the Lua variables. A typed read returns the fallback when the variable is missing or holds another type, so `GetVariableString` returns the fallback for a number.
- **`GetActorField` returns a field only when it holds text.** Otherwise you get the fallback.
- **`SetVariable(name, value)` returns `true` when the variable was written, and `false` (logged once) when the name or the value is not allowed.** It returns `false` when:
  - the `name` does not match `^[A-Za-z_][A-Za-z0-9_]*$` (a name must start with a letter or `_`, and have only letters, digits and `_`). A newline at the end is refused too;
  - the `name` is a Lua keyword: `and break do else elseif end false for function if in local nil not or repeat return then true until while`;
  - the `name` is one the game writes itself: `Actor`, `ActorIndex`, `Alert`, `Conversant`, `ConversantIndex`, `Conversation`, `Converrsant` (the game's own spelling), `Currency`, `Day`, `PlayerAge`, `PlayerFirstName`, `PlayerGender`, `PlayerLastName`, `PoliceIdle`, `Price`, `SavedLevelName`, `Speaker`, `WakeUpReason`, or any name that starts with `DREAM_`;
  - the `value` is a string that contains a backslash or a control character (such as NUL, a new line, a carriage return or a tab). Double quotes are fine;
  - the `value` is not a number (NaN), is infinite, or is a double too large for a float;
  - the `value` is not a string, bool, int, float or double.
- **A bad variable name or value can make the whole save fail to load, so the framework refuses them.** The game stores the variables in the save as Lua text, and the game cannot read a bad name or value back.
  - Technical: the game saves dialogue variables in `Globals.dialogue` as Lua text.
- **Numbers are stored as floats, so whole numbers above 16,777,216 lose precision.** That is 2^24.
- **Start your variable names with your mod's name, for example `mymod_visits`.** That keeps them apart from the game's and from other mods'.

**Starting and stopping conversations.**

- **`Start(title, actor, conversant)` (experimental) starts a conversation and returns whether one is running afterwards.** It returns `false` when a conversation is already running, `title` is empty, or the game is loading or saving. The game can refuse to start a conversation, for example when there is no dialogue screen, so check the result.
  - Technical: it starts the conversation through `InteractableTalk.StartDialogue` when the conversant has that component, and through `DialogueManager.StartConversation` otherwise.
- **`Stop()` (experimental) ends every running conversation, and it is never refused.** It works even while a save loads or saves. It may leave the person you spoke to in the middle of the conversation, because the game's cleanup runs only when the game's own close step happens.
  - Technical: it calls the Dialogue System's `StopAllConversations`, also while `SaveGate.Busy`. The state that `InteractableTalk.CheckStartDialogueInterrupt` set when the talk started (`PlayerStats.Active`, the control locks, the dialogue registration) is cleared only when `DialogueController.OnDialogueClosed` runs.
- **`Started(title)` and `Ended(title)` fire for every conversation, including ones the game starts itself.** A message can reach the object twice in some setups. `CurrentTitle` is `""` whenever `IsActive` is `false`.
  - Technical: they come from the game's `OnConversationStart` and `OnConversationEnd` messages.

**Changing a conversation before it starts (`Intercept`).**

- **`Intercept(modId, handler)` runs before every conversation starts, whether the game or this API starts it.** Your changes to the game's dialogue database are in place by the time the conversation begins. Handlers run in the order they were added, and adding the same handler for the same mod again does nothing.
  - Technical: the handler runs in a prefix on `DialogueSystemController.StartConversation`.
- **Setting `args.Title` sends the player to a different conversation.** The new title is used only when `Exists(title)` is `true`. Otherwise the original title stays and the refusal is logged once. `Title` is last-writer-wins: the last handler to set it counts. It is ignored once the conversation is cancelled.
- **Setting `args.Cancel = true` stops the conversation, and nobody can undo it.** Any `Cancel` wins: once a handler sets it, the later handlers still run and see `Cancel` as `true`, and setting it back to `false` has no effect.
- **Experimental: cancelling a conversation can leave the game waiting for it.** A cancelled start skips the game's `StartConversation`, so the code that asked for the conversation gets none. Cancel only where you know what started it. Known risks:
  - a behavior tree step that waits for the conversation to finish can stall;
  - a cutscene starter that waits for a conversation can freeze;
  - the talk interaction changes its own state before it starts a conversation, and a later check repairs it.
  - Technical: the stalling task is a BehaviorDesigner task with `waitForConversationCompletion`. The talk interaction is the game's `InteractableTalk` class, and its `DelayCheck` repairs the state.

**Lua functions.**

- **`RegisterFunction` adds a function the game's conversation scripts can call, and gives back its name.** The Lua name is `Mod_<prefix>_<name>`. The `prefix` is your mod id with each character that is not an ASCII letter or digit replaced by `_`. When that replacement changes the id, the prefix also gets `_` and an 8-digit hex hash of the original id, so `a.b` and `a_b` never collide, and the name does not depend on which mod loads first. It returns `null` when the API is not available.
  - Technical: the hash is FNV-1a.
- **`name` must be a valid Lua name, and the function's parameters must be `float`, `string` or `bool`.** Lua numbers arrive as `float`.
- **`target` is `null` for a static method, and an object of the method's class otherwise.**
- **Registering a name again replaces the earlier function.**
- **The framework adds six functions of its own, which are not in the `Mod_` namespace.** It adds them when `Dialogue` is available, at start and after each scene load.
  - `Core_QuestState(taskId)` returns `"Active"`, `"Failed"`, `"Finished"`, `"Abandoned"` or `""`.
  - `Core_ObjectiveState(taskId, objectiveId)` returns the state name of the step, or `""`.
  - `Core_CrimeTotal()` returns `Law.Total`.
  - `Core_CrimeAgainst(npcId)` returns `Law.GetAgainst(npcId)`. `npcId` is a number.
  - `Core_ResponsePicked(key)` runs the `onPicked` of the answer line with that key. The framework's own answer lines call it, so your mod does not.
  - `Core_LineText(id)` returns the text of a line whose text a mod gave as a callback. The framework's own dialogue lines call it, so your mod does not.

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

In a conversation condition you would then write `Mod_mymod_HasKey("<taskId>")`.
