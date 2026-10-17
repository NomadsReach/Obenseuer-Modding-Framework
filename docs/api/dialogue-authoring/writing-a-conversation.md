---
title: Writing a conversation
parent: Dialogue authoring
grand_parent: Story
nav_order: 2
---

# Writing a conversation

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public enum ConversationRefusal
{
    None,            // nothing was refused
    NotReady,        // the dialogue system is not running yet, for example on the title screen
    SaveBusy,        // a save is loading or being written
    BadInput,        // the conversation is not valid (see the rules below)
    AlreadyExists,   // a conversation with this title is already in the game
    UnknownSpeaker,  // the speaker is not a character in the game, or is the player
    NoPlayerActor,   // the game has no character marked as the player
    Failed,          // the game failed while adding it
    ReservedKey      // the key is "narration", which the framework keeps for itself
}

public sealed class ConversationLine    // one line of a conversation
{
    public string Key { get; set; }                  // this line's name, used by other lines to point at it
    public bool Player { get; set; }                 // true for a choice the player picks, false for a line the character speaks
    public string Text { get; set; }                 // what the line says; may be null when TextCallback is set
    public string Condition { get; set; }            // a Lua test; the line is offered only when it is true; may be null
    public string Script { get; set; }               // Lua that runs when the line is reached; may be null
    public string[] Next { get; set; }               // the keys of the lines that follow; may be null
    public Func<string> TextCallback { get; set; }   // a function that gives the text each time the line shows; may be null
}

public sealed class ConversationDefinition    // a whole conversation
{
    public string Key { get; set; }                  // the conversation's name
    public string Speaker { get; set; }              // the name of the game character who speaks the NPC lines; may be null
    public List<ConversationLine> Lines { get; set; }   // the lines; the first one is where the conversation starts
}

public static class DialogueAuthoring
{
    public static string TitleOf(string modId, string key);                                       // the title a conversation gets; null when either part is not valid
    public static string Add(string modId, ConversationDefinition definition, out ConversationRefusal refusal);   // adds a conversation; gives its title, or null and the reason
    public static bool Remove(string modId, string key);                                          // removes one of your conversations
    public static int Clear(string modId);                                                        // removes everything your mod added; gives how many
}
```

Lua is the small scripting language the game's dialogue system uses for conditions and scripts. You do not need to learn much of it: a condition such as `not mymod_asked` or a script such as `Variable["mymod_cellar"] = true` is enough for most conversations.

- **The conversation is kept in memory only, so add it again every time the game starts.** The game saves the dialogue variables, but never the conversations themselves. Add your conversation before the player can talk to the character. A conversation you added once stays in the game for the rest of the session, also across a save load, and the framework puts it back after a scene load if the game cleared it.
- **Do not rely on the game remembering which lines were already shown.** Whether the game saves that for a conversation depends on a setting in the game's scene that the framework cannot read. Keep your own flags in dialogue variables (`Dialogue.SetVariable`) or in your own save data.
- **A conversation's title is your mod id, a dot, and the key.** `TitleOf(modId, key)` builds it. A key has letters, digits, `_` and `-`, up to 48 characters, and no dot. The key `narration` belongs to `Dialogue.Narrate`, so `Add` and `AddBranch` refuse it with `ReservedKey`.
- **A title that is already in the game is refused with `AlreadyExists`.** If you add the conversation on every scene load, call `Dialogue.Exists(title)` first.
- **A conversation has 1 to 200 lines, and the first line is where it starts.** Each line has a key that is not used by another line, text of up to 1000 characters, and the keys of the lines that follow it. A line with no next line ends the conversation. When a character's line is followed by player lines, they are offered as the player's choices. A line can point back to an earlier line.
- **`Condition` and `Script` are Lua, up to 500 characters each.** The line is offered only when its condition is true, and the script runs when the line is reached. They use the game's own Lua, so the game's functions work, such as `TaskActive("<taskId>")` (see [Quest authoring](../quest-authoring.md)), and so do the variables you set with `Dialogue.SetVariable`. If the Lua has an error, the game logs it and the framework carries on.
- **The player's side is the game's player character.** The framework finds it in the game's list of characters when you call `Add`. `Speaker` is the name of a game character, who speaks the NPC lines. If `Speaker` is `null`, the NPC lines have no speaker name, unless the character you start the conversation with has its own, in which case the game shows that character's name. An unknown speaker, or a speaker that is the player, is refused with `UnknownSpeaker`.
  - Technical: the player's side is the actor marked as the player in the Dialogue System database.
- **The game identifies the conversation by a number that the framework works out from the title.** It is the same on every start, and it skips a number the game already uses.
  - Technical: the id counts up from 1,000,000. The game keys its Lua `Conversation` table by it.
- **`Add` returns the title, or `null` and the reason in `refusal`.** `NoPlayerActor` means the game has no character marked as the player. `Failed` means a game call failed or the game did not take the conversation. A refusal because of a bad value or a conflict goes to the log once for each reason.
- **A write is also refused while a save is loading or being written, and `Add` needs a running dialogue system.** The title screen does not have one, so you get `NotReady` there. The dialogue system must also be available (`Dialogue.IsAvailable`).
- **`Remove` removes one of your conversations.** It returns `false` for another mod's conversation, for one that does not exist, for one that is running right now, and for the conversation of a branch (use `RemoveBranch` for those).
- **`Clear` removes all of your conversations that are not running, and the branches your mod added with `Dialogue.AddBranch`.** It returns how many it removed.
