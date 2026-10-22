---
title: Branches under a game NPC line
parent: Dialogue authoring
grand_parent: Story
nav_order: 4
---

# Branches under a game NPC line

`Dialogue.AddBranch` links a conversation made of your own lines into a game conversation, which is how you build a menu tree. The branch is a conversation written with `DialogueAuthoring`, so the rules for keys, lines, and putting it back on the [Writing a conversation](writing-a-conversation.md) page apply to it. The overloads link your branch under the "anchor" line, and you give the anchor by its text or by its id. This part is experimental.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Dialogue</code> <span>links your branches into game conversations</span></p>

You call `AddBranch` and keep the key it gives back, so you can call `RemoveBranch` with that key later.

<div class="api-pair" markdown="1">

```csharp
public static class Dialogue
{
    public static string AddBranch(string modId, string conversationTitle, string anchorText, ConversationDefinition branch);
    public static string AddBranch(string modId, string conversationTitle, int anchorId, ConversationDefinition branch);
    public static string AddBranch(string modId, string conversationTitle, string anchorText, ConversationDefinition branch, Action onPicked);
    public static string AddBranch(string modId, string conversationTitle, int anchorId, ConversationDefinition branch, Action onPicked);
    public static bool RemoveBranch(string modId, string key);
}
```

| Method | Returns | What it does |
|---|---|---|
| `AddBranch(modId, conversationTitle, anchorText, branch)` | `string` | Links your branch under the anchor line, found by its text. Gives back the branch key. |
| `AddBranch(modId, conversationTitle, anchorId, branch)` | `string` | Links your branch under the anchor line, found by its id. Gives back the branch key. |
| `AddBranch(modId, conversationTitle, anchorText, branch, onPicked)` | `string` | The same, and runs `onPicked` when the player picks the branch. |
| `AddBranch(modId, conversationTitle, anchorId, branch, onPicked)` | `string` | The same, and runs `onPicked` when the player picks the branch. |
| `RemoveBranch(modId, key)` | `bool` | Removes one of your branches. `false` for another mod's or an unknown key. |

</div>
</div>

## Putting it together

This example adds a "Membership" menu under the line "What can I do for you?". Its reply shows the days left and changes each visit. `MembershipDaysLeft()` stands for your own method that returns a whole number.

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

string key = Dialogue.AddBranch(Mod.Id, "Shop.Greeting", "What can I do for you?", membership);
```

The tree is: the game's line "What can I do for you?", then your player choice "Membership", then the character's reply with the days left, then the two choices "Renew for 30 days." and "Goodbye.".

## Things to know

- For a line that has a `TextCallback`, the original written text is the callback's markup, so use the `anchorId` overload for that line. The anchor is chosen the same way as for `AddResponse`, and the same refusals apply.
- The branch starts at its first line. A player line gives the player a choice; a character line is played straight after the anchor. The anchor gets one link to that line, at the priority of its other player choices, and a character first line gets one tier below them by default: the game takes a character line before the player's choices at the same tier, so the branch plays only when none of the choices above it can. Pass `priority` to place it yourself, or add a `Condition` to the line.
- With `onPicked`, your code runs when the player picks the branch. Adding the same branch again is still refused, so the first `onPicked` stays.
- When the conversation or the anchor is not loaded yet, `AddBranch` gives back the key and links the branch as soon as both are there. You do not need to call it again.
- `Dialogue.AddQuestTopic` builds a topic like this for you and hangs it under the first character line of a conversation. It needs the conversation loaded when you call it, and refuses when it is not, unlike `AddBranch`. Its rules are on the Dialogue authoring page.
- The branch's character lines are spoken by the anchor's character, so their name and picture show. `branch.Speaker` may be `null` or the name of that same character.
- `AddBranch` returns a key, your mod id, a dot and eight characters. It comes from the conversation title, the anchor you asked for and `branch.Key`, so it is the same whether the anchor was loaded at the time or not, and a second call with the same arguments is refused.
- The branch's own conversation title is your mod id, a dot and `branch.Key`, so a key already in the game is refused with `AlreadyExists`, also when it is used under another anchor.
- The framework puts the link back by itself after each scene load and each load of a save, as for responses.
- `RemoveBranch` removes the link and the branch's conversation.
- `Dialogue.RemoveAll(modId)` and `DialogueAuthoring.Clear(modId)` also remove your branches, and keep any they cannot remove right now. You can't remove a branch's conversation with `DialogueAuthoring.Remove`.
- Branches are never saved. A save made while a branch is in place loads fine without your mod.

## Limits

- `AddBranch` refuses a branch whose speaker is not the anchor's character. Any other name is refused.
- Adding the same branch again is refused, so call `RemoveBranch` first. A key already in the game is refused with `AlreadyExists`, also when it is used under another anchor.
- `RemoveBranch` returns `false` for another mod's key, an unknown key, while a save is loading or being written, when the dialogue system is not ready, and while any conversation is running. The player can reach a branch through another conversation's link.
- `AddBranch` returns `null` and logs the reason once when it is refused. That happens when the dialogue system is not ready, a save is loading or being written, the branch is not valid, the branch key is `narration`, the anchor text is empty or only spaces, the anchor text is matched by more than one line, the anchor is not a character line or leads only to character lines, or the branch speaker is not the anchor's character. A conversation or an anchor that is not loaded yet is not a refusal: the branch waits for it.
- An empty `conversationTitle` throws an `ArgumentException`. A `null` `branch` throws an `ArgumentNullException`, and an invalid `modId` throws an `ArgumentException`.
- A branch cannot send the player back into the game's conversation. A line without `Next` ends the conversation, so in the example above "Goodbye." ends the talk and the game's menu is not shown again. Put any choice that should go back to the game's menu in the game conversation itself, not in a branch.
