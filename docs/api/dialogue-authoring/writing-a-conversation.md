---
title: Writing a conversation
parent: Dialogue authoring
grand_parent: Story
nav_order: 2
---

# Writing a conversation

This is the full list of what the API gives you, one section for each part. Lua is the small scripting language the game's dialogue system uses for conditions and scripts. You do not need to learn much of it: a condition such as `not mymod_asked` or a script such as `Variable["mymod_cellar"] = true` is enough for most conversations.

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

You call `DialogueAuthoring.Add` to put a conversation in the game, `Remove` to take one of yours out, and `TitleOf` to work out the title it will get.
{: .api-desc}

| Method | Returns | What it does |
|---|---|---|
| `TitleOf(modId, key)` | `string` | The title a conversation gets. `null` when either part is not valid. |
| `Add(modId, definition, out refusal)` | `string` | <details>{::nomarkdown}<summary>{:/nomarkdown}Adds a conversation. Gives its title, or `null` and the reason.{::nomarkdown}</summary>{:/nomarkdown}When it returns `null`, read `refusal` to see why. The reasons are listed in `ConversationRefusal` below.</details> |
| `Remove(modId, key)` | `bool` | Removes one of your conversations. |
| `Clear(modId)` | `int` | Removes everything your mod added, and returns how many. |

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

You fill one in and pass it to `DialogueAuthoring.Add`, or to `Dialogue.AddBranch` for a branch. The game has no such type: the framework builds the game's own conversation from it when you add it.
{: .api-desc}

| Property | Type | What it means |
|---|---|---|
| `Key` | `string` | The conversation's name. |
| `Speaker` | `string` | <details>{::nomarkdown}<summary>{:/nomarkdown}The name of the game character who speaks the NPC lines. May be `null`.{::nomarkdown}</summary>{:/nomarkdown}Use the name of a game character, not the player. A name the game does not have, or the player's name, is refused with `UnknownSpeaker`. See `ConversationRefusal` below.</details> |
| `Lines` | `List<ConversationLine>` | The lines. The first one is where the conversation starts. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ConversationRefusal</code> <span>why your conversation was not added</span></p>

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

This tells you why your conversation was not added to the game. `Add` gives it to you through its `out refusal` parameter: when `Add` returns `null`, read it to see what went wrong; when `Add` works it is `None`. It has nothing to do with a character refusing to talk to the player. The game has no refusal codes of its own: the framework makes these checks itself while it adds the conversation.
{: .api-desc}

| Value | Meaning |
|---|---|
| `None` | Nothing was refused. |
| `NotReady` | <details>{::nomarkdown}<summary>{:/nomarkdown}The dialogue system is not running yet, for example on the title screen.{::nomarkdown}</summary>{:/nomarkdown}The game's dialogue database is not loaded yet. Add your conversation again later, for example from `Events.WhenReady`, which runs your code once a save has loaded (see [Events.WhenReady](../core-helpers/events-whenready.md)).</details> |
| `SaveBusy` | <details>{::nomarkdown}<summary>{:/nomarkdown}A save is loading or being written.{::nomarkdown}</summary>{:/nomarkdown}Add the conversation again after the save finishes.</details> |
| `BadInput` | <details>{::nomarkdown}<summary>{:/nomarkdown}The conversation is not valid.{::nomarkdown}</summary>{:/nomarkdown}See the [rules on this page](#things-to-know). The main ones: keys use letters, digits, `_` and `-`, up to 48 characters, and each line key is used once. Text is up to 1000 characters per line, and a condition or script up to 500. A conversation has 1 to 200 lines. Each `Next` must name a line in the same conversation.</details> |
| `AlreadyExists` | <details>{::nomarkdown}<summary>{:/nomarkdown}A conversation with this title is already in the game.{::nomarkdown}</summary>{:/nomarkdown}You added it already, or the game has one with this title. Call `Remove` with your key first, or use another key. If you add it on every scene load, check `Dialogue.Exists(title)` first.</details> |
| `UnknownSpeaker` | <details>{::nomarkdown}<summary>{:/nomarkdown}The speaker is not a character in the game, or is the player.{::nomarkdown}</summary>{:/nomarkdown}Set `Speaker` to the name of a game character, not the player. See `ConversationDefinition` above.</details> |
| `NoPlayerActor` | <details>{::nomarkdown}<summary>{:/nomarkdown}The game has no character marked as the player.{::nomarkdown}</summary>{:/nomarkdown}This is rare. The game's dialogue database has no character marked as the player, so the framework cannot build the conversation.</details> |
| `Failed` | <details>{::nomarkdown}<summary>{:/nomarkdown}The game failed while adding it.{::nomarkdown}</summary>{:/nomarkdown}The game threw an error or did not take the conversation. The framework logs the error once.</details> |
| `ReservedKey` | <details>{::nomarkdown}<summary>{:/nomarkdown}The key is `"narration"`, which the framework keeps for itself.{::nomarkdown}</summary>{:/nomarkdown}Pick another key.</details> |

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

You put these in a `ConversationDefinition`'s `Lines` list. `Player` is `false` for a line the character speaks and `true` for a choice the player picks, and `Next` names the lines that follow it. <span class="tech">(Each line becomes a `DialogueEntry` in the game's Dialogue System conversation.)</span>
{: .api-desc}

| Property | Type | What it means |
|---|---|---|
| `Key` | `string` | This line's name. Other lines use it to point at this line. |
| `Player` | `bool` | `true` for a choice the player picks, `false` for a line the character speaks. |
| `Text` | `string` | <details>{::nomarkdown}<summary>{:/nomarkdown}What the line says. May be `null` when `TextCallback` is set.{::nomarkdown}</summary>{:/nomarkdown}A line needs text unless it has a `TextCallback`. Blank text, or text over 1000 characters, is refused with `BadInput`.</details> |
| `Condition` | `string` | A Lua test. The line is offered only when it is true. May be `null`. |
| `Script` | `string` | Lua that runs when the line is reached. May be `null`. |
| `Next` | `string[]` | <details>{::nomarkdown}<summary>{:/nomarkdown}The keys of the lines that follow. May be `null`.{::nomarkdown}</summary>{:/nomarkdown}Each key must be the `Key` of another line in the same conversation. Any other key is refused with `BadInput`.</details> |
| `TextCallback` | `Func<string>` | A function that gives the text each time the line shows. May be `null`. |

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
