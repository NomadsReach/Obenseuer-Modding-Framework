---
title: Dialogue authoring
parent: Story
grand_parent: API reference
has_children: true
nav_order: 1
---

# Dialogue authoring

Status: experimental.

## What it is

A conversation is a set of lines that a character speaks, and choices the player picks. This API lets your mod write its own conversations while the game runs and start them like any conversation from the game. It can also add a new answer to a conversation the game already has, and hang a whole menu of your own lines under a line of a game character. It can change a line's text each time it shows, and show a plain block of narration text on a black screen.

An NPC line is spoken by the character. A player line is a choice the player picks. The framework does not write voice, camera cuts or other effects, so a conversation shows with the game's normal look. It builds on the `Dialogue` part of the [Story API](../api-story/dialogue.md#dialogue), which starts and stops conversations.

## When to use it

- Give a character of your own mod something to say.
- Add a new answer for the player to a game character's conversation, for example a new shop question.
- Add a menu (such as "Membership") under a line of an existing character, with its own replies.
- Show a short piece of narration on a black screen, such as "Three days later".

## Quick example

This mod adds a small conversation as soon as the game is ready, and logs the title that the game knows it by. You then start it with `Dialogue.Start`, as the next section shows.

```csharp
using ObenseuerCore;

[ModInfo("example.greeter", "Greeter", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class Greeter : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            ConversationDefinition talk = new ConversationDefinition { Key = "greeting" };
            talk.Lines.Add(new ConversationLine { Key = "hello", Text = "Back again?", Next = new[] { "ask", "leave" } });
            talk.Lines.Add(new ConversationLine { Key = "ask", Player = true, Text = "Any work for me?", Next = new[] { "work" } });
            talk.Lines.Add(new ConversationLine { Key = "leave", Player = true, Text = "Not now." });
            talk.Lines.Add(new ConversationLine { Key = "work", Text = "Check the cellar.", Script = "Variable[\"mymod_cellar\"] = true" });

            ConversationRefusal refusal;
            string title = DialogueAuthoring.Add(Mod.Id, talk, out refusal);
            Log.Info(title == null ? "The conversation was refused: " + refusal : "Added " + title);
        });
    }
}
```

The first line, "Back again?", is spoken by the character. It offers two choices, "Any work for me?" and "Not now.", and a line with no `Next` ends the conversation. The `Script` on the last line sets a conversation variable when the player reaches it. `Mod.Id` is the id you gave in `[ModInfo]`. See [Using events](../guides/getting-started/using-events.md) for more on `WhenReady`.
- `Add` returns the title, or `null` with the reason in `refusal`. `NoPlayerActor` means the game's database has no actor marked as the player. `Failed` means a game call threw or the database did not take the conversation. `ReservedKey` means the key is `narration`, which `Dialogue.Narrate` uses.
- `Remove` removes one of your conversations and returns `false` for another mod's, an unknown one, one that is running now, or a branch's conversation (use `RemoveBranch` for those). `Clear` removes all of yours that are not running, and the branches your mod added with `Dialogue.AddBranch`, and returns how many.

## Complete example

This mod adds its own conversation, adds a choice and a menu under one of its lines, shows a line whose text changes, and starts the conversation with an NPC. It needs no game conversation title, because the choice and the menu go under a line of the mod's own conversation.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.dockgreeter", "Dock Greeter", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class DockGreeter : FrameworkMod
{
    // The name of the NPC that starts the conversation. Use the name of a character in your save.
    private const string TalkerName = "Replace with an NPC name";
    private const string Greeting = "greeting";
    private ModKeybind talkKey;

    protected override void OnLoad()
    {
        talkKey = Mod.Key("Talk", "Start the greeting", KeyCode.F6);
        Events.WhenReady(AddConversation);
    }

    private void AddConversation()
    {
        string title = DialogueAuthoring.TitleOf(Mod.Id, Greeting);
        if (Dialogue.Exists(title)) return;

        ConversationDefinition talk = new ConversationDefinition { Key = Greeting };
        talk.Lines.Add(new ConversationLine { Key = "hello", Text = "Back again?", Next = new[] { "ask", "leave" } });
        talk.Lines.Add(new ConversationLine { Key = "ask", Player = true, Text = "Any work for me?", Next = new[] { "work" } });
        talk.Lines.Add(new ConversationLine { Key = "leave", Player = true, Text = "Not now." });
        talk.Lines.Add(new ConversationLine
        {
            Key = "work",
            Text = "Check the cellar.",
            TextCallback = () => "Check the cellar. You owe " + Dialogue.GetVariableNumber("dock_owed", 0f) + " coins.",
            Script = "Variable[\"dock_cellar\"] = true"
        });

        ConversationRefusal refusal;
        string added = DialogueAuthoring.Add(Mod.Id, talk, out refusal);
        if (added == null)
        {
            Log.Warn("The conversation was refused: " + refusal);
            return;
        }

        if (Dialogue.AddResponse(Mod.Id, added, "Back again?", "Do you have any rope?", "Second shelf, by the door.", null, () => Dialogue.SetVariable("dock_rope_asked", true)) == null)
            Log.Warn("The rope answer was refused, so the anchor text may not match");

        ConversationDefinition membership = new ConversationDefinition { Key = "membership" };
        membership.Lines.Add(new ConversationLine { Key = "ask", Player = true, Text = "Membership", Next = new[] { "status" } });
        membership.Lines.Add(new ConversationLine { Key = "status", Text = "Your card is in order.", Next = new[] { "renew", "back" } });
        membership.Lines.Add(new ConversationLine { Key = "renew", Player = true, Text = "Renew for 30 days.", Script = "Variable[\"dock_renew\"] = true" });
        membership.Lines.Add(new ConversationLine { Key = "back", Player = true, Text = "Goodbye." });
        if (Dialogue.AddBranch(Mod.Id, added, "Back again?", membership) == null)
            Log.Warn("The membership menu was refused");
    }

    private void Update()
    {
        if (UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked) return;
        if (talkKey == null || !talkKey.Down()) return;
        NpcInfo npc = Npcs.Find(TalkerName);
        if (npc == null || !NpcActions.StartDialogue(npc.Id, DialogueAuthoring.TitleOf(Mod.Id, Greeting)))
            Log.Warn("The greeting did not start. Check TalkerName and that the NPC is in the scene.");
    }
}
```

The example adds the conversation the first time a save loads, or a new game starts, in a game session. `DialogueAuthoring.Add` refuses a title that already exists, so `Dialogue.Exists` is checked first. For a later save load in the same session, run the same method from `Events.Loaded` as well, which is safe because `Dialogue.Exists` is checked before anything is added. The choice "Do you have any rope?" and the "Membership" menu both hang under "Back again?". `TextCallback` on "Check the cellar." shows the amount owed each time that line shows, and it only reads a variable. The F6 key starts the conversation with the NPC through `NpcActions.StartDialogue`, which takes the NPC's id and the conversation title.

To add a choice to a game conversation instead, pass that conversation's title and a line of its text to `Dialogue.AddResponse` or `Dialogue.AddBranch` in the same way. The title and the text must match what the game has.

## Responses to a game conversation

`Dialogue.AddResponse` adds one player choice to a conversation the game already has, for example a second reply to an NPC line. Nothing is copied: the choice is written into the game's own conversation for this session, and `Dialogue.RemoveResponse` or `Dialogue.RemoveAll` takes it out again. Experimental.

```csharp
public static class Dialogue
{
    public static string AddResponse(string modId, string conversationTitle, string anchorText, string playerText, string npcReply, string luaCondition, Action onPicked, int insertAt = -1, int priority = -1);
    public static string AddResponse(string modId, string conversationTitle, int anchorId, string playerText, string npcReply, string luaCondition, Action onPicked, int insertAt = -1, int priority = -1);
    public static string AddResponse(string modId, string conversationTitle, string anchorText, string playerText, string npcReply, string luaCondition, Action onPicked);
    public static string AddResponse(string modId, string conversationTitle, int anchorId, string playerText, string npcReply, string luaCondition, Action onPicked);
    public static bool RemoveResponse(string modId, string key);
}
```

- **Anchor.** The anchor is an NPC line of the conversation. `anchorText` is matched against the line's authored Dialogue Text, not the translated text the player sees: an exact match is tried first, then a match that ignores surrounding spaces and letter case. Zero matches, or more than one, is refused, so use the `anchorId` overload when the text is not unique. A player line cannot be the anchor. An anchor whose links lead only to NPC lines is refused, because the game then shows that NPC line and never the choice. An anchor with no links is accepted. If the anchor links to both NPC and player lines, the game plays an NPC line whose condition is true instead of showing the choices, as it does for its own lines, so the new choice only shows when none of those NPC lines can play.
- **Shape.** The player line is offered with the anchor's other choices, at the same priority as they have unless `priority` sets another. If `npcReply` is not null, the NPC answers the choice with `npcReply`, and the conversation ends after `npcReply`. If it is null, the conversation ends after the player line.
- **Position.** `insertAt` is the index in the anchor's outgoing links where the choice is inserted, counting from 0, so `0` puts it first. The default `-1` appends, which places the choice after links the game appends itself, such as the leave option. An index past the end appends too. The position is kept when the framework writes the choice again after a scene load.
- **Priority.** `priority` is the game's link priority: `0` Low, `1` BelowNormal, `2` Normal, `3` AboveNormal, `4` High. The default `-1` copies the priority of the anchor's own player links, and `Normal` when it has none. The game considers links of one priority tier at a time, from High down, so a higher priority offers the choice before the anchor's own choices and a lower one after them. A value outside -1 to 4 throws `ArgumentException`.
- **Condition.** `luaCondition` is Lua, up to 500 characters, or null. The choice is offered only while it is true, as for `Condition` above.
- **Text.** A reply's text can change from one visit to the next through Dialogue System variable markup: put `[var=Name]` in the text, and set `Name` with `Dialogue.SetVariable`.
- **onPicked.** Runs when the player picks the choice. It also runs when the choice is the only one and the game picks it for the player. An exception is logged once and does not reach the game.
- **Key.** `AddResponse` returns the key, `modId` plus a dot and eight hex digits. The key comes from the conversation, the anchor and the texts, so the same arguments give the same key. Calling `AddResponse` again with the same arguments returns that key and replaces `onPicked`.
- **Re-apply.** The game can make new copies of its conversations on a scene load, so the framework adds the responses back on every scene load and after each load of a save. Your mod does not re-add them. When the conversation or the anchor is not in the database at that moment, the framework keeps trying once per frame for a bounded number of frames and logs one failure per key when it gives up, so a response whose conversation loads late is applied when it appears. A later scene load or save load starts the attempts again.
- **Refusals.** `AddResponse` returns `null` when the dialogue system is not installed or not ready (no game scene yet), when a save or load is running, when a text is empty or too long, when the conversation is not in the database, when the anchor is missing, ambiguous, not an NPC line, or links only to NPC lines, or when the database has no player actor. An empty `conversationTitle` or `playerText` throws `ArgumentException`, and so do an `insertAt` below -1 and a `priority` outside -1 to 4. The original seven-argument form without `insertAt` and `priority` is kept as a thin wrapper, so a mod compiled against 0.11.0-beta still binds it; it is the same as passing -1 for both.
- **Remove.** `RemoveResponse` returns `false` for another mod's key, an unknown key, or while a save or load is running. `Dialogue.RemoveAll(modId)` also removes the mod's interceptors and Lua functions, whatever the save state. Its responses are kept while a save or load is running, as `RemoveResponse` refuses then.
- **Saves.** The framework never saves responses. A save made while a response is in place loads without the mod: the read-state rows of the response lines are ignored.

```csharp
string key = Dialogue.AddResponse("example.mymod", "Shop.Greeting", "Need anything today?", "Do you have any rope?", "Second shelf, by the door.", "not mymod_asked", () => Dialogue.SetVariable("mymod_asked", true));
if (key == null) return;
```

## Branches under a game NPC line

`Dialogue.AddBranch` links a conversation of your own lines into a game conversation, for a menu tree. The branch is an authored conversation, so the rules above for ids, validation and re-adding apply to it. Experimental.

```csharp
public static class Dialogue
{
    public static string AddBranch(string modId, string conversationTitle, string anchorText, ConversationDefinition branch, int insertAt = -1, int priority = -1);
    public static string AddBranch(string modId, string conversationTitle, int anchorId, ConversationDefinition branch, int insertAt = -1, int priority = -1);
    public static string AddBranch(string modId, string conversationTitle, string anchorText, ConversationDefinition branch);
    public static string AddBranch(string modId, string conversationTitle, int anchorId, ConversationDefinition branch);
    public static string AddBranch(string modId, string conversationTitle, string anchorText, ConversationDefinition branch, Action onPicked);
    public static string AddBranch(string modId, string conversationTitle, int anchorId, ConversationDefinition branch, Action onPicked);
    public static string AddBranch(string modId, string conversationTitle, string anchorText, ConversationDefinition branch, int insertAt, int priority, Action onPicked);
    public static string AddBranch(string modId, string conversationTitle, int anchorId, ConversationDefinition branch, int insertAt, int priority, Action onPicked);
    public static string AddQuestTopic(string modId, string conversationTitle, string playerLine, string[] npcLines, string showWhen, Action onDone);
    public static bool RemoveBranch(string modId, string key);
}
```

- **Anchor.** The anchor is an NPC line of the game conversation, chosen as for `AddResponse`, and the same refusals apply. For a line with a `TextCallback`, the authored text is the callback's markup, so use the `anchorId` overload for that line.
- **Link.** The branch starts at its first line, the first entry of `Lines`. The anchor gets one link to that line, at the priority of its other player choices unless `priority` sets another, and at the index `insertAt` in the anchor's links (`-1` appends, as it does for `AddResponse`). The first line may be a player line, which the player picks, or a character line, which plays straight after the anchor. A character first line gets one priority tier below the anchor's player choices by default, because the game takes a character line before the player's choices when both are at the same tier (see the responses page): the branch then plays only when no choice above it can. Pass `priority` to place it yourself, and give the line a `Condition` to decide when it plays. When the anchor's player choices are already at the lowest tier, there is no tier below them and the branch shares theirs, so it is shown instead of the menu; that is logged once.
- **onPicked.** `onPicked` runs when the player picks the branch, that is, when its first line is reached. It also runs when the branch is the only choice and the game picks it for the player. An exception is logged once and does not reach the game. Adding the same branch again is refused as before, so a later call with another `onPicked` does not replace the first.
- **Not loaded yet.** When the anchor conversation or the anchor is not in the database at the time of the call, `AddBranch` still gives back the key and links the branch as soon as both appear. The link is tried again on each scene load and each load of a save, and on the same bounded per-frame retries as a re-apply. A branch that can never be linked, such as one whose anchor text is matched by more than one line, is refused once and then left alone.
- **Speaker.** The branch's NPC lines are spoken by the anchor's NPC, so its name and portrait show. `branch.Speaker` may be null or the name of that same actor. Any other name is refused.
- **Key.** `AddBranch` returns the key, `modId` plus a dot and eight hex digits. It comes from the conversation title, the anchor and `branch.Key`. Calling `AddBranch` again with the same arguments is refused, so call `RemoveBranch` first. The key comes from the arguments of the call, not from the anchor entry that was found, so it is the same whether the anchor was loaded yet or not; a second call while the first branch is still waiting is refused as already added. The branch's conversation title is the mod id, a dot and `branch.Key`, so a key already in the database is refused as `AlreadyExists`, also when it is used on another anchor.
- **Re-apply.** The framework writes the anchor link again after each scene load and each load of a save, as it does for responses, and retries it the same bounded way when the conversation, the anchor or the branch conversation is not in the database yet. Your mod does not re-add it.
- **Remove.** `RemoveBranch` removes the link and the branch's conversation. It returns `false` for another mod's key, an unknown key, a save or load running, the dialogue database not ready, or while any conversation is running, because the player can reach a branch through another conversation's link. `Dialogue.RemoveAll(modId)` removes the mod's branches too, and keeps any it cannot remove now, while a save, a load or a conversation is running. `DialogueAuthoring.Clear(modId)` does the same. A branch conversation that is still in place cannot be removed with `DialogueAuthoring.Remove`.
- **Refusals.** `AddBranch` returns `null` and logs the reason once when the dialogue system is not ready, a save or load is running, the branch is not valid, the branch key is `narration`, `anchorText` is empty or only spaces, the anchor is matched by more than one line, the anchor is not an NPC line, its links lead only to NPC lines, or the branch speaker is not the anchor's NPC. A conversation or an anchor that is simply not in the database yet is not refused: the branch waits for it, as described above. An empty `conversationTitle` throws `ArgumentException`. A null `branch` throws `ArgumentNullException`, an invalid `modId` throws `ArgumentException`, and so do an `insertAt` below -1 and a `priority` outside -1 to 4. The original four-argument form without `insertAt` and `priority` is kept as a thin wrapper as well, and so are the two forms without `onPicked`.
- **Saves.** Like responses, the framework never saves branches. A save made while a branch is in place loads without the mod.

```csharp
ConversationDefinition membership = new ConversationDefinition { Key = "membership" };
membership.Lines.Add(new ConversationLine { Key = "ask", Player = true, Text = "Membership", Next = new[] { "status" } });
membership.Lines.Add(new ConversationLine
{
    Key = "status",
    Text = "Your card is in order.",
    TextCallback = () => "You have " + MembershipDaysLeft() + " days left.",
    Next = new[] { "renew", "back" }
});
membership.Lines.Add(new ConversationLine { Key = "renew", Player = true, Text = "Renew for 30 days.", Script = "Variable[\"mymod_renew\"] = true" });
membership.Lines.Add(new ConversationLine { Key = "back", Player = true, Text = "Goodbye." });

string key = Dialogue.AddBranch("example.mymod", "Shop.Greeting", "What can I do for you?", membership);
```

The tree is: the anchor "What can I do for you?", then the player choice "Membership", then the NPC reply with the days left, then the two choices "Renew for 30 days." and "Goodbye.".

A line without `Next` ends the conversation. A branch cannot return to the game conversation, so "Goodbye." ends the talk and the game's menu is not shown again. Put any choice that should go back to the game's menu in the game conversation itself, not in a branch.

## Quest topics

`Dialogue.AddQuestTopic(modId, conversationTitle, playerLine, npcLines, showWhen, onDone)` is a short way to add one topic to a conversation the game already has. It builds the lines for you and calls `Dialogue.AddBranch` with them, under the first character line of the conversation. Experimental, and every rule above for branches applies.

```csharp
public static class Dialogue
{
    public static string AddQuestTopic(string modId, string conversationTitle, string playerLine, string[] npcLines, string showWhen, Action onDone);
}
```

- **Shape.** `playerLine` becomes a player choice under the conversation's first character line, shown only while `showWhen` is true. That line is the first entry of the conversation that is not a group entry, has text, is spoken by a character, and can reach a player choice; the root entry and character lines whose links lead only to character lines are skipped. `npcLines` are spoken by that character, one after another, in the order you give them, and the conversation ends after the last one. `onDone` runs when the player picks the topic.
- **Key.** The branch key comes from the conversation, the anchor line and `playerLine`, and starts with `topic`, so two topics in one conversation never share a key. The same arguments give the same key.
- **Refusals.** It returns `null` and logs the reason once when the dialogue system is not ready, the conversation is not in the database, the conversation has no character line that can take a topic, or the branch is refused. Unlike `AddBranch`, it needs the conversation loaded at the time of the call, because it looks for that line at once, and it refuses rather than waiting for the conversation to appear. An empty `conversationTitle`, an empty `playerLine`, and a null or empty `npcLines` return `null` and log the reason once instead of throwing `ArgumentException`.

```csharp
string key = Dialogue.AddQuestTopic("example.mymod", "Shop.Greeting", "Any news about the cellar?", new[] { "Still locked.", "Ask me tomorrow." }, "Quest[\"mymod_cellar\"] ~= \"Finished\"", () => Dialogue.SetVariable("mymod_asked", true));
```

## Text callbacks

`ConversationLine.TextCallback` sets a line's text from a function. The game calls it when the line shows, so the text can change from one visit to the next. It works for a conversation from `DialogueAuthoring.Add` and for a branch.

- The callback returns the text. When it returns null, the line shows `Text` instead. When it throws, the line shows nothing and the exception is logged once.
- A returned text over 1000 characters, or with a control character, is replaced by `Text` and logged once.
- When `TextCallback` is set, `Text` may be null. Without a callback, `Text` is required as before.
- The callback can run several times for one showing of a line. While the game works out the links, it parses each NPC reply it considers, and it parses a player choice once for the menu and again for the subtitle. The callback must have no side effects.
- The callback runs on the game's main thread while the conversation is open. Keep it short. Do not start a conversation or write to the save from it.
- The returned text is parsed as Dialogue System markup, as authored text is: `[var=Name]`, emphasis tags such as `[em1]`, and `|` as a line break (on by default in the game's subtitle settings) all work.
- `Remove` drops the callbacks of that conversation, and `RemoveBranch` drops those of its branch. `DialogueAuthoring.Clear` drops those of each conversation it removes, and `Dialogue.RemoveAll` drops those of the branches it removes. `Dialogue.RemoveAll` does not remove conversations made with `DialogueAuthoring.Add`, so use `Clear` for those.

## Narration

`Dialogue.Narrate(modId, text)` (experimental) shows `text` in the game's dialogue box on the game's black screen. It is built on this API, with one conversation per mod: the key is `narration`, so the title is `modId.narration`. Each call replaces that conversation's text, then starts it through the game's `BlackCanvas.StartDialogue`.

```csharp
public static class Dialogue
{
    public static bool Narrate(string modId, string text);
}
```

- It returns `true` when a conversation is running after the call. That can be a different conversation when a handler from `Dialogue.Intercept` redirects the start.
- It returns `false` when the text is null, empty, only spaces, over 1000 characters, or has a control character other than a line break (`\n` or `\r`). It also returns `false` when `Dialogue.IsAvailable` is false, the dialogue system is not ready, a save or load is running, a conversation is already running, `BlackCanvas.instance` is null, or the player is waiting or asleep. It returns `false` too when `DialogueAuthoring.Add` refuses the conversation, for example when the database has no player actor.
- A call from a wake-up callback is refused while the sleep event's text (`WaitingController.eventDialogue`) is still waiting to show, for the same reason: starting a conversation wakes the player and cuts that text off.
- The screen starts to fade to black at once, before the conversation text shows.
- When the conversation ends, the framework fades the screen back out. It uses the game's end-of-talk callback on the black screen's talk component. The framework keeps any callback already set there and runs it after the fade. If the conversation does not start, the framework fades the screen back at once and `Narrate` returns `false`. If the start throws after the conversation has begun, `Narrate` returns `true` and the fade back happens when that conversation ends.
- An invalid `modId` throws `ArgumentException`, as for the other `Dialogue` calls.
- The key `narration` belongs to `Narrate`. Each call removes the mod's own conversation with that key first. `Add` and `AddBranch` refuse the key `narration` (`ReservedKey`), and the refusal is logged once.

## Reference

The types and calls the examples above use. The rules for each one are in the sections above.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ConversationDefinition</code> <span>a conversation your mod writes</span></p>

Pass one of these to `DialogueAuthoring.Add`. `Key` is the part of the title your mod chooses, and `Lines` holds the lines.

<div class="api-pair" markdown="1">

```csharp
public sealed class ConversationDefinition
{
    public string Key { get; set; }
    public string Speaker { get; set; }
    public List<ConversationLine> Lines { get; set; }
}
```

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ConversationLine</code> <span>one line a character or the player says</span></p>

A line is a character line unless `Player` is `true`, which makes it a choice the player picks. `Next` lists the keys of the lines that follow it, and a line without `Next` ends the conversation.

<div class="api-pair" markdown="1">

```csharp
public sealed class ConversationLine
{
    public string Key { get; set; }
    public bool Player { get; set; }
    public string Text { get; set; }
    public string Condition { get; set; }
    public string Script { get; set; }
    public string[] Next { get; set; }
    public Func<string> TextCallback { get; set; }
}
```

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ConversationRefusal</code> <span>why a conversation was not added</span></p>

`Add` sets this through its `out refusal` parameter. `None` means nothing was refused.

<div class="api-pair" markdown="1">

```csharp
public enum ConversationRefusal
{
    None,
    NotReady,
    SaveBusy,
    BadInput,
    AlreadyExists,
    UnknownSpeaker,
    NoPlayerActor,
    Failed,
    ReservedKey
}
```

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DialogueAuthoring</code> <span>adds, removes and names your conversations</span></p>

`TitleOf` gives the title a conversation of yours gets, so you can check it with `Dialogue.Exists`. `Add` returns that title, or `null` with the reason in `refusal`.

<div class="api-pair" markdown="1">

```csharp
public static class DialogueAuthoring
{
    public static string TitleOf(string modId, string key);
    public static string Add(string modId, ConversationDefinition definition, out ConversationRefusal refusal);
    public static bool Remove(string modId, string key);
    public static int Clear(string modId);
}
```

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Dialogue</code> <span>starts, checks and follows conversations</span></p>

`Start` begins a conversation with two `Transform` sides from Unity. `Exists` tells you whether a title is in the game's database, and `IsActive` and `CurrentTitle` tell you what is running. `NpcActions.StartDialogue` starts one with an NPC by its id, as the examples on the [common tasks](dialogue-authoring/common-tasks.md) page do.

<div class="api-pair" markdown="1">

```csharp
public static class Dialogue
{
    public static bool Start(string title, Transform actor, Transform conversant);
    public static void Stop();
    public static bool IsActive { get; }
    public static string CurrentTitle { get; }
    public static bool Exists(string title);
    public static event Action<DialogueArgs> Started;
    public static event Action<DialogueArgs> Ended;
}
```

</div>
</div>

## Self-check

Set `Debug.DialogueAuthoringSelfCheck` in the framework's config to log a check of the definition rules, the line links and the speakers at start. Set `Debug.DialogueResponsesSelfCheck` to log a check of the anchor matching and the id allocation for responses.
