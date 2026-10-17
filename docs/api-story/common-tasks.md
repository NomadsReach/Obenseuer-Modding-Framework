---
title: Common tasks
parent: Quests, dialogue and law
grand_parent: Story
nav_order: 1
---

# Common tasks

List every task the player has started, with its state.

```csharp
foreach (QuestInfo quest in Quests.All())
    Log.Info(quest.Title + ": " + quest.State);
```

Finish one step of a task. The ids come from the game, so the two here are placeholders. This works only when the step is active, and it returns `false` if it was refused.

```csharp
bool done = Quests.SetObjective("<taskId>", "<objectiveId>", ObjectiveState.Finished);
```

Read and set a conversation variable. Put your mod's name at the start of your variable names.

```csharp
Dialogue.SetVariable("mymod_visits", 3);
float visits = Dialogue.GetVariableNumber("mymod_visits", 0f);
```

Send a conversation somewhere else when it starts. The title here is an example, the conversation title has to exist in the game.

```csharp
Dialogue.Intercept("mymod", args => { if (args.Title == "mymod/Greeting") args.Title = "mymod/Greeting2"; });
```

Check how much trouble the player is in, and record a crime.

```csharp
float total = Law.Total;
bool recorded = Law.Commit(CrimeKind.Vandalism, 5, 0, false);
```
