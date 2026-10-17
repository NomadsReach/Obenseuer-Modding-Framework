---
title: Quests, dialogue and law
parent: Story
grand_parent: API reference
has_children: true
nav_order: 3
---

# Quests, dialogue and law

Status: experimental.

## What it is

These three APIs cover the story side of the game. `Quests` reads the player's tasks and their steps, moves a task or a step on, and tells you when one changes. `Dialogue` reads and sets the variables the game's conversations use, starts, stops and redirects conversations, and lets your mod add its own functions to the conversation scripts. `Law` reads how much trouble the player is in, records and clears crimes, sends the player to prison, and tells you when the player is spotted, fined, arrested or released.

Writing a whole quest or a whole conversation from scratch is a different job, and it is on its own pages: [Quest authoring](api/quest-authoring.md) and [Dialogue authoring](api/dialogue-authoring.md).

## When to use it

- Do something when the player finishes a task or a step of a task.
- Finish or fail a task step from your own code.
- Read or set a variable that a conversation checks, or send the player into a different conversation.
- Add your own check to a conversation line, for example "only show this answer if the player has a key".
- Record a crime when the player does something your mod counts as wrong, or cancel a crime you do not want.
- Show a message when the player is arrested, fined or released.

## Quick example

This mod writes a line to its log when any task changes state, when the player commits a crime, and when a conversation starts. Each of these is an event: the framework calls your code each time it happens. See [Using events](guides/getting-started/using-events.md) for more on events.

```csharp
using ObenseuerCore;

[ModInfo("example.storywatch", "Story Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class StoryWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Quests.TaskChanged += args => Log.Info("Task " + args.TaskId + " is now " + args.State);
        Law.Committed += args => Log.Info("The player committed " + args.Kind + ", severity " + args.Severity);
        Dialogue.Started += args => Log.Info("A conversation started: " + args.Title);
    }
}
```
