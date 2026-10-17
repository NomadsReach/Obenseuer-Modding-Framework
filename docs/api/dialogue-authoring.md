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

A conversation is a set of lines that a character speaks, and choices the player picks. This API lets your mod write its own conversations while the game runs and start them like any conversation from the game. It can also add a new answer to a conversation the game already has, hang a whole menu of your own lines under a line of a game character, change a line's text each time it shows, and show a plain block of narration text on a black screen.

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
