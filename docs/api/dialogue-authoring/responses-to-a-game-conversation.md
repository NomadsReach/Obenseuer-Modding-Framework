---
title: Responses to a game conversation
parent: Dialogue authoring
grand_parent: Story
nav_order: 3
---

# Responses to a game conversation

`Dialogue.AddResponse` adds one player choice to a conversation the game already has, for example a second reply to a character's line. Nothing is copied: the choice is written into the game's own conversation for this session, and `Dialogue.RemoveResponse` or `Dialogue.RemoveAll` takes it out again. This part is experimental.

```csharp
public static class Dialogue
{
    // Both add one player choice under the "anchor" line; give the anchor by its text or by its id.
    public static string AddResponse(string modId, string conversationTitle, string anchorText, string playerText, string npcReply, string luaCondition, Action onPicked);
    public static string AddResponse(string modId, string conversationTitle, int anchorId, string playerText, string npcReply, string luaCondition, Action onPicked);
    public static bool RemoveResponse(string modId, string key);   // removes one of your responses; false for another mod's or an unknown key
}
```

- **The anchor is the line of the character that your new choice hangs under.** `anchorText` is matched against the line's original written text, not the translated text the player sees. The framework tries an exact match first, then a match that ignores spaces around the text and capital letters. If nothing matches, or more than one line matches, the call is refused. Use the `anchorId` overload when the text is not unique.
- **The anchor has to be a character's line that leads to choices.** A player line cannot be the anchor. An anchor whose links lead only to character lines is refused, because the game then plays that line and never shows the choices. An anchor with no links at all is accepted.
- **If the anchor leads to both character lines and player lines, your choice only shows when none of those character lines can play.** The game plays a character line whose condition is true instead of showing the choices, as it does for its own lines.
- **Your choice sits with the anchor's other choices, at the same priority.** If `npcReply` is not `null`, the character answers with `npcReply` and the conversation ends after it. If it is `null`, the conversation ends after your player line.
- **`luaCondition` is Lua, up to 500 characters, or `null`.** The choice is offered only while it is true.
- **The text of a choice can change from one visit to the next.** Put `[var=Name]` in the text and set `Name` with `Dialogue.SetVariable`.
- **`onPicked` runs when the player picks your choice.** It also runs when yours is the only choice and the game picks it for the player. If it throws an error, the framework logs it once and the game carries on.
- **`AddResponse` returns a key, your mod id, a dot and eight characters.** The key comes from the conversation, the anchor and the texts, so the same arguments give the same key. Calling `AddResponse` again with the same arguments returns that key and replaces `onPicked`.
- **The framework puts your responses back by itself.** The game can make new copies of its conversations when a scene loads, so the framework adds the responses again on every scene load and after each load of a save. Your mod does not add them again.
- **`AddResponse` returns `null` when it was refused.** That happens when the dialogue system is not installed or not ready (no game scene yet), when a save is loading or being written, when a text is empty or too long, when the conversation is not in the game, when the anchor is missing, matches more than one line, is not a character's line, or leads only to character lines, and when the game has no player character. An empty `conversationTitle` or `playerText` throws an `ArgumentException`.
- **`RemoveResponse` returns `false` for another mod's key, an unknown key, or while a save is loading or being written.** `Dialogue.RemoveAll(modId)` also removes your interceptors and Lua functions at any time. While a save is loading or being written, it keeps your responses, as `RemoveResponse` refuses then.
- **Responses are never saved.** A save made while a response is in place loads fine without your mod. The "already shown" marks for the response lines are ignored.
