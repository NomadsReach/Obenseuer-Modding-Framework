---
title: Writing a conversation
parent: Dialogue authoring
grand_parent: Story
nav_order: 2
---

# Writing a conversation

This is the full list of what the API gives you, one section for each part. Lua is the small scripting language the game's dialogue system uses for conditions and scripts. You do not need to learn much of it: a condition such as `not mymod_asked` or a script such as `Variable["mymod_cellar"] = true` is enough for most conversations.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ConversationRefusal</code> <span>why Add refused a conversation</span></p>
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

| Value | Meaning |
|---|---|
| `None` | Nothing was refused. |
| `NotReady` | The dialogue system is not running yet, for example on the title screen. |
| `SaveBusy` | A save is loading or being written. |
| `BadInput` | The conversation is not valid. See the rules below. |
| `AlreadyExists` | A conversation with this title is already in the game. |
| `UnknownSpeaker` | The speaker is not a character in the game, or is the player. |
| `NoPlayerActor` | The game has no character marked as the player. |
| `Failed` | The game failed while adding it. |
| `ReservedKey` | The key is `"narration"`, which the framework keeps for itself. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ConversationLine</code> <span>one line of a conversation</span></p>
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

| Property | Type | What it means |
|---|---|---|
| `Key` | `string` | This line's name. Other lines use it to point at this line. |
| `Player` | `bool` | `true` for a choice the player picks, `false` for a line the character speaks. |
| `Text` | `string` | What the line says. May be `null` when `TextCallback` is set. |
| `Condition` | `string` | A Lua test. The line is offered only when it is true. May be `null`. |
| `Script` | `string` | Lua that runs when the line is reached. May be `null`. |
| `Next` | `string[]` | The keys of the lines that follow. May be `null`. |
| `TextCallback` | `Func<string>` | A function that gives the text each time the line shows. May be `null`. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ConversationDefinition</code> <span>a whole conversation</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ConversationDefinition
{
    public string Key { get; set; }
    public string Speaker { get; set; }
    public List<ConversationLine> Lines { get; set; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Key` | `string` | The conversation's name. |
| `Speaker` | `string` | The name of the game character who speaks the NPC lines. May be `null`. |
| `Lines` | `List<ConversationLine>` | The lines. The first one is where the conversation starts. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>DialogueAuthoring</code> <span>add, remove and title your conversations</span></p>
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

| Method | Returns | What it does |
|---|---|---|
| `TitleOf(string modId, string key)` | `string` | The title a conversation gets. `null` when either part is not valid. |
| `Add(string modId, ConversationDefinition definition, out ConversationRefusal refusal)` | `string` | Adds a conversation. Gives its title, or `null` and the reason. |
| `Remove(string modId, string key)` | `bool` | Removes one of your conversations. |
| `Clear(string modId)` | `int` | Removes everything your mod added, and returns how many. |

</div>
</div>

## Things to know

- The conversation is kept in memory only, so add it again every time the game starts. The game saves the dialogue variables, but never the conversations themselves. Add your conversation before the player can talk to the character.
- A conversation you added once stays in the game for the rest of the session, also across a save load. The framework puts it back after a scene load if the game cleared it.
- A conversation's title is your mod id, a dot, and the key. `TitleOf(modId, key)` builds it. A key has letters, digits, `_` and `-`, up to 48 characters, and no dot. The key `narration` belongs to `Dialogue.Narrate`, so `Add` and `AddBranch` refuse it with `ReservedKey`.
- A conversation has 1 to 200 lines, and the first line is where it starts. Each line has a key that no other line uses, text of up to 1000 characters, and the keys of the lines that follow it (`Next`).
- A line with no next line ends the conversation. When a character's line is followed by player lines, they are offered as the player's choices. A line can point back to an earlier line.
- `Condition` and `Script` are Lua, up to 500 characters each. The line is offered only when its condition is true, and the script runs when the line is reached. They use the game's own Lua, so the game's functions work, such as `TaskActive("<taskId>")` (see [Quest authoring](../quest-authoring.md)), and so do the variables you set with `Dialogue.SetVariable`. If the Lua has an error, the game logs it and the framework carries on.
- The player's side is the game's player character. The framework finds it in the game's list of characters when you call `Add`. `Speaker` is the name of a game character, who speaks the NPC lines. If `Speaker` is `null`, the NPC lines have no speaker name, unless the character you start the conversation with has its own, in which case the game shows that character's name. <span class="tech">(The player's side is the actor marked as the player in the Dialogue System database.)</span>
- The game identifies the conversation by a number that the framework works out from the title. It is the same on every start, and it skips a number the game already uses. <span class="tech">(The id counts up from 1,000,000. The game keys its Lua `Conversation` table by it.)</span>
- `Add` returns the title, or `null` and the reason in `refusal`. `NoPlayerActor` means the game has no character marked as the player. `Failed` means a game call failed or the game did not take the conversation.
- `Remove` removes one of your conversations. It returns `false` for another mod's conversation, for one that does not exist, for one that is running right now, and for the conversation of a branch (use `RemoveBranch` for those).
- `Clear` removes all of your conversations that are not running, and the branches your mod added with `Dialogue.AddBranch`. It returns how many it removed.

## Limits

- Do not rely on the game remembering which lines were already shown. Whether the game saves that for a conversation depends on a setting in the game's scene that the framework cannot read. Keep your own flags in dialogue variables (`Dialogue.SetVariable`) or in your own save data.
- A title that is already in the game is refused with `AlreadyExists`. If you add the conversation on every scene load, call `Dialogue.Exists(title)` first.
- A refusal because of a bad value or a conflict goes to the log once for each reason.
- A write is also refused while a save is loading or being written, and `Add` needs a running dialogue system. The title screen does not have one, so you get `NotReady` there. The dialogue system must also be available (`Dialogue.IsAvailable`).
