---
title: Contests
parent: Player life and skills
grand_parent: API reference
has_children: true
nav_order: 1
---

# Contests

## What it is

The game has a few contests the player can take part in. In the drinking contest and the sauna contest, the player sits across from an NPC and tries to outlast them. In the rat fight arena, the player bets on one of the rats and watches them fight. This API lets your mod see which contest is running, who is in it, how much the opponent has left, and get told when one starts or finishes. It also lets your mod end the player's own contest as a loss, and change what a rat fight pays out. It cannot start a contest or score one.

## When to use it

- Run your own code when the player wins or loses a drinking contest, a sauna contest or a rat fight bet.
- Show a message or give a reward when a contest ends. The game itself gives no reward from the code, so a reward is up to the map or your mod.
- Make rat fights pay more or less than they do in the vanilla game.

## Quick example

This mod writes a log line when a contest starts and another when it ends. The end line says whether the player won.

```csharp
using ObenseuerCore;

[ModInfo("example.contestwatch", "Contest Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ContestWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Contests.Started += contest => Log.Info(contest.Name + " (" + contest.Kind + ") started");
        Contests.Finished += result =>
        {
            string outcome = result.PlayerWon ? "The player won" : "The player did not win";
            Log.Info(outcome + " the " + result.Contest.Name);
        };
    }
}
```

`Started` and `Finished` are events: the framework calls your code each time they happen. See [Using events](../guides/getting-started/using-events.md) for more on events.
