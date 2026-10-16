---
title: Obenseuer Modding Framework dialogue authoring API (DialogueAuthoring)
parent: API reference
nav_order: 115
---

# Obenseuer Modding Framework dialogue authoring API (DialogueAuthoring)

Experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.dialogueauthoring`. Logs go to `BepInEx\logs\obenseuer.moddingframework.dialogueauthoring.log`.
Files: `src/Story/DialogueAuthoring.cs`. It builds on `Dialogue` (`src/Story/Dialogue.cs`), so it needs `Dialogue.IsAvailable`.

Everything not listed here is internal. Main thread only.

Scope: a mod adds a conversation to the game's Dialogue System database while the game runs, then starts it with `Dialogue.Start(title, actor, conversant)` like any game conversation. A conversation is a set of lines. An NPC line is spoken by the NPC and a player line is a choice the player picks. The framework does not write voice, sequences or camera cuts, so the conversation shows with the game's default dialogue sequence.

## Rules

- **Session only.** The conversation lives in memory. The game saves only the Lua variables of the dialogue system, never the conversation definitions. Add the conversation again on every launch, before the player talks. A conversation added once stays in the game's database across a save load in the same session. The framework adds it back after a scene load if the game cleared it.
- **Read state.** Whether the game saves "line was already shown" state for a conversation depends on a setting in the game's scene that the framework cannot read. Do not rely on it. Treat the read state of an added conversation as session only. Keep your own flags in dialogue variables (`Dialogue.SetVariable`) or in your own save data.
- **Title.** The conversation title is `modId + "." + key`. `DialogueAuthoring.TitleOf(modId, key)` builds it. A key has letters, digits, `_` and `-`, up to 48 characters, no dot. A title that is already in the database is refused (`AlreadyExists`), so call `Dialogue.Exists(title)` first if you add on every scene load. The key `narration` is reserved for `Dialogue.Narrate`, so `Add` and `AddBranch` refuse it (`ReservedKey`).
- **Lines.** A definition has 1 to 200 lines, and the first line is where the conversation starts. Each line has a unique key, text (up to 1000 characters), and the keys of the lines that follow it. A line with no next line ends the conversation. Player lines among the next lines of an NPC line are offered as the player's choices. A line can point back to an earlier line.
- **Condition and Script.** A line may have a Lua `Condition` (the line is offered only when it is true) and a Lua `Script` (run when the line is reached). Each is up to 500 characters. They use the game's Lua, so the game's functions (for example `TaskActive("<taskId>")`, see [quest authoring](quest-authoring.md)) and the variables from `Dialogue.SetVariable` work. A Lua error is logged by the game and does not stop the framework.
- **Actors.** The conversation's actor is the game's player actor, found in the game's database at the time of the call (the actor marked as the player). `Speaker` is the name of a dialogue actor in the game's database for the NPC lines. If it is null, NPC lines have no speaker name unless the NPC you start the conversation with carries its own dialogue actor, in which case the game shows that NPC's name. An unknown speaker, or a speaker that is the player, is refused (`UnknownSpeaker`).
- **Ids.** The framework gives the conversation an id from 1,000,000 up, computed from the title, so it is the same on every launch, and it skips an id the game already uses. The game keys its Lua `Conversation` table by this id.
- **Refusals.** `Add` returns `null` and sets `refusal`. `Remove` returns `false`. A write is also refused during a save or a load, and `Add` needs a running dialogue system (a game scene, not the title screen), which is `NotReady` otherwise. A refusal that is a bad argument or a conflict is logged once per reason.

## API

```csharp
public enum ConversationRefusal { None, NotReady, SaveBusy, BadInput, AlreadyExists, UnknownSpeaker, NoPlayerActor, Failed, ReservedKey }

public sealed class ConversationLine
{
    public string Key { get; set; }
    public bool Player { get; set; }        // true for a choice the player picks
    public string Text { get; set; }        // may be null when TextCallback is set
    public string Condition { get; set; }   // Lua, may be null
    public string Script { get; set; }      // Lua, may be null
    public string[] Next { get; set; }      // keys of the lines that follow, may be null
    public Func<string> TextCallback { get; set; }   // text read each time the line shows, may be null
}

public sealed class ConversationDefinition
{
    public string Key { get; set; }
    public string Speaker { get; set; }     // dialogue actor name in the game's database, may be null
    public List<ConversationLine> Lines { get; set; }
}

public static class DialogueAuthoring
{
    public static string TitleOf(string modId, string key);                                   // null when either is invalid
    public static string Add(string modId, ConversationDefinition definition, out ConversationRefusal refusal);
    public static bool Remove(string modId, string key);
    public static int Clear(string modId);
}
```

- `Add` returns the title, or `null` with the reason in `refusal`. `NoPlayerActor` means the game's database has no actor marked as the player. `Failed` means a game call threw or the database did not take the conversation. `ReservedKey` means the key is `narration`, which `Dialogue.Narrate` uses.
- `Remove` removes one of your conversations and returns `false` for another mod's, an unknown one, one that is running now, or a branch's conversation (use `RemoveBranch` for those). `Clear` removes all of yours that are not running, and the branches your mod added with `Dialogue.AddBranch`, and returns how many.

## Example

```csharp
ConversationDefinition talk = new ConversationDefinition { Key = "greeting" };
talk.Lines.Add(new ConversationLine { Key = "hello", Text = "Back again?", Next = new[] { "ask", "leave" } });
talk.Lines.Add(new ConversationLine { Key = "ask", Player = true, Text = "Any work for me?", Next = new[] { "work" } });
talk.Lines.Add(new ConversationLine { Key = "leave", Player = true, Text = "Not now." });
talk.Lines.Add(new ConversationLine { Key = "work", Text = "Check the cellar.", Script = "Variable[\"mymod_cellar\"] = true" });

ConversationRefusal refusal;
string title = DialogueAuthoring.Add("example.mymod", talk, out refusal);
if (title != null) Dialogue.Start(title, playerTransform, npcTransform);
```

## Responses to a game conversation

`Dialogue.AddResponse` adds one player choice to a conversation the game already has, for example a second reply to an NPC line. Nothing is copied: the choice is written into the game's own conversation for this session, and `Dialogue.RemoveResponse` or `Dialogue.RemoveAll` takes it out again. Experimental.

```csharp
public static class Dialogue
{
    public static string AddResponse(string modId, string conversationTitle, string anchorText, string playerText, string npcReply, string luaCondition, Action onPicked);
    public static string AddResponse(string modId, string conversationTitle, int anchorId, string playerText, string npcReply, string luaCondition, Action onPicked);
    public static bool RemoveResponse(string modId, string key);
}
```

- **Anchor.** The anchor is an NPC line of the conversation. `anchorText` is matched against the line's authored Dialogue Text, not the translated text the player sees: an exact match is tried first, then a match that ignores surrounding spaces and letter case. Zero matches, or more than one, is refused, so use the `anchorId` overload when the text is not unique. A player line cannot be the anchor. An anchor whose links lead only to NPC lines is refused, because the game then shows that NPC line and never the choice. An anchor with no links is accepted. If the anchor links to both NPC and player lines, the game plays an NPC line whose condition is true instead of showing the choices, as it does for its own lines, so the new choice only shows when none of those NPC lines can play.
- **Shape.** The player line is offered with the anchor's other choices, at the same priority as they have. If `npcReply` is not null, the NPC answers the choice with `npcReply`, and the conversation ends after `npcReply`. If it is null, the conversation ends after the player line.
- **Condition.** `luaCondition` is Lua, up to 500 characters, or null. The choice is offered only while it is true, as for `Condition` above.
- **Text.** A reply's text can change from one visit to the next through Dialogue System variable markup: put `[var=Name]` in the text, and set `Name` with `Dialogue.SetVariable`.
- **onPicked.** Runs when the player picks the choice. It also runs when the choice is the only one and the game picks it for the player. An exception is logged once and does not reach the game.
- **Key.** `AddResponse` returns the key, `modId` plus a dot and eight hex digits. The key comes from the conversation, the anchor and the texts, so the same arguments give the same key. Calling `AddResponse` again with the same arguments returns that key and replaces `onPicked`.
- **Re-apply.** The game can make new copies of its conversations on a scene load, so the framework adds the responses back on every scene load and after each load of a save. Your mod does not re-add them.
- **Refusals.** `AddResponse` returns `null` when the dialogue system is not installed or not ready (no game scene yet), when a save or load is running, when a text is empty or too long, when the conversation is not in the database, when the anchor is missing, ambiguous, not an NPC line, or links only to NPC lines, or when the database has no player actor. An empty `conversationTitle` or `playerText` throws `ArgumentException`.
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
    public static string AddBranch(string modId, string conversationTitle, string anchorText, ConversationDefinition branch);
    public static string AddBranch(string modId, string conversationTitle, int anchorId, ConversationDefinition branch);
    public static bool RemoveBranch(string modId, string key);
}
```

- **Anchor.** The anchor is an NPC line of the game conversation, chosen as for `AddResponse`, and the same refusals apply. For a line with a `TextCallback`, the authored text is the callback's markup, so use the `anchorId` overload for that line.
- **Link.** The branch starts at its first line, the first entry of `Lines`. The anchor gets one link to that line, at the priority of its other player choices. The first line must be a player line. `AddBranch` refuses a branch whose first line is an NPC line, because the player would get no choice.
- **Speaker.** The branch's NPC lines are spoken by the anchor's NPC, so its name and portrait show. `branch.Speaker` may be null or the name of that same actor. Any other name is refused.
- **Key.** `AddBranch` returns the key, `modId` plus a dot and eight hex digits. It comes from the conversation title, the anchor and `branch.Key`. Calling `AddBranch` again with the same arguments is refused, so call `RemoveBranch` first. The branch's conversation title is the mod id, a dot and `branch.Key`, so a key already in the database is refused as `AlreadyExists`, also when it is used on another anchor.
- **Re-apply.** The framework writes the anchor link again after each scene load and each load of a save, as it does for responses. Your mod does not re-add it.
- **Remove.** `RemoveBranch` removes the link and the branch's conversation. It returns `false` for another mod's key, an unknown key, a save or load running, the dialogue database not ready, or while any conversation is running, because the player can reach a branch through another conversation's link. `Dialogue.RemoveAll(modId)` removes the mod's branches too, and keeps any it cannot remove now, while a save, a load or a conversation is running. `DialogueAuthoring.Clear(modId)` does the same. A branch conversation that is still in place cannot be removed with `DialogueAuthoring.Remove`.
- **Refusals.** `AddBranch` returns `null` and logs the reason once when the dialogue system is not ready, a save or load is running, the branch is not valid, the branch key is `narration`, the first line is not a player line, the conversation or anchor is not found, or the branch speaker is not the anchor's NPC. An empty `conversationTitle` throws `ArgumentException`. A null `branch` throws `ArgumentNullException`, and an invalid `modId` throws `ArgumentException`.
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

## Self-check

Set `Debug.DialogueAuthoringSelfCheck` in the framework's config to log a check of the definition rules, the line links and the speakers at start. Set `Debug.DialogueResponsesSelfCheck` to log a check of the anchor matching and the id allocation for responses.
