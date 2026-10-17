---
title: Branches under a game NPC line
parent: Dialogue authoring
grand_parent: Story
nav_order: 4
---

# Branches under a game NPC line

`Dialogue.AddBranch` links a conversation made of your own lines into a game conversation, which is how you build a menu tree. The branch is a conversation written with `DialogueAuthoring`, so the rules above for keys, lines, and putting it back apply to it. This part is experimental.

```csharp
public static class Dialogue
{
    // Both link your branch under the "anchor" line; give the anchor by its text or by its id.
    public static string AddBranch(string modId, string conversationTitle, string anchorText, ConversationDefinition branch);
    public static string AddBranch(string modId, string conversationTitle, int anchorId, ConversationDefinition branch);
    public static bool RemoveBranch(string modId, string key);   // removes one of your branches; false for another mod's or an unknown key
}
```

- **The anchor is chosen the same way as for `AddResponse`, and the same refusals apply.** For a line that has a `TextCallback`, the original written text is the callback's markup, so use the `anchorId` overload for that line.
- **The branch starts at its first line, and that line must be a player line.** The anchor gets one link to that line, at the priority of its other player choices. `AddBranch` refuses a branch whose first line is a character line, because the player would get no choice.
- **The branch's character lines are spoken by the anchor's character, so their name and picture show.** `branch.Speaker` may be `null` or the name of that same character. Any other name is refused.
- **`AddBranch` returns a key, your mod id, a dot and eight characters.** It comes from the conversation title, the anchor and `branch.Key`. Adding the same branch again is refused, so call `RemoveBranch` first. The branch's own conversation title is your mod id, a dot and `branch.Key`, so a key already in the game is refused with `AlreadyExists`, also when it is used under another anchor.
- **The framework puts the link back by itself** after each scene load and each load of a save, as for responses.
- **`RemoveBranch` removes the link and the branch's conversation.** It returns `false` for another mod's key, an unknown key, while a save is loading or being written, when the dialogue system is not ready, and while any conversation is running (the player can reach a branch through another conversation's link). `Dialogue.RemoveAll(modId)` and `DialogueAuthoring.Clear(modId)` also remove your branches, and keep any they cannot remove right now. You can't remove a branch's conversation with `DialogueAuthoring.Remove`.
- **`AddBranch` returns `null` and logs the reason once when it is refused.** That happens when the dialogue system is not ready, a save is loading or being written, the branch is not valid, the branch key is `narration`, the first line is not a player line, the conversation or the anchor is not found, or the branch speaker is not the anchor's character. An empty `conversationTitle` throws an `ArgumentException`. A `null` `branch` throws an `ArgumentNullException`, and an invalid `modId` throws an `ArgumentException`.
- **Branches are never saved.** A save made while a branch is in place loads fine without your mod.
- **A branch cannot send the player back into the game's conversation.** A line without `Next` ends the conversation, so in the example below "Goodbye." ends the talk and the game's menu is not shown again. Put any choice that should go back to the game's menu in the game conversation itself, not in a branch.

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
