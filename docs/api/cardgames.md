---
title: Card games
parent: Money and shops
grand_parent: API reference
has_children: true
nav_order: 2
---

# Card games

## What it is

The game has card tables where the player bets against NPC opponents. There are two games. Players know them as Shittypants and Crosseven. In the code they are called `ChinaHand` and `Ristiseiska`. This API lets your mod list the card tables in the scene, and see which have a game running and what the stake is. It tells you when a game starts or ends, and it can change how much a game pays out. It cannot start a game, deal cards, play a card or set the stake.

## When to use it

- Write something to the log or show a message when the player wins or loses a card game.
- Make the card games pay more or less than they do in the vanilla game.
- Check whether a game is running at a table before your mod does something else there.

## Quick example

This mod writes a log line when a card game starts and another when it ends. The end line says whether the player won, what the stake was and what the game paid back.

```csharp
using ObenseuerCore;

[ModInfo("example.cardwatch", "Card Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class CardWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        CardGames.Started += table => Log.Info(table.Kind + " started with " + table.Players + " players, stake " + table.Stake);
        CardGames.Ended += result =>
        {
            string won = result.PlayerWon ? "won" : "did not win";
            string unit = result.ItemBet ? "items" : result.Currency.ToString();
            Log.Info("The player " + won + " " + result.Kind + ": stake " + result.Stake + ", paid back " + result.Payout + " " + unit);
        };
    }
}
```

`Started` and `Ended` are events: the framework calls your code each time they happen. See [Using events](../guides/getting-started/using-events.md) for more on events.
