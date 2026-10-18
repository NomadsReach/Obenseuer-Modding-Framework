---
title: Casino
parent: Money and shops
grand_parent: API reference
has_children: true
nav_order: 3
---

# Casino

## What it is

The game has two gambling machines you can place or find in the world: the slot machine, and a coin pusher (the game calls it Pajatso). It also has scratchcards, which are items the player uses. This API lets your mod list the machines in the scene, and change how much they pay out. It tells you when a round is played, won, doubled or cashed out, and when a scratchcard is used. Both machines take and pay OC (Open Sewer Coins, see the [glossary](../glossary.md#oc)).

The player's gambling stats are not covered here: the totals of what they won and lost, the gambling need and the addiction level. `Survival` (see [Survival](../api-core/survival.md#survival)) covers only the gambling need and addiction level, not the totals.

## When to use it

- Log or react when the player plays, wins or cashes out at a machine.
- Make the machines pay more or less, for example for an event or a harder difficulty.
- React when the player scratches a card, and see what it paid.
- Find out how much the last win was.

## Quick example

This mod writes a log line each time a machine round is played, each time a machine wins, and each time the player takes their money out.

```csharp
using ObenseuerCore;

[ModInfo("example.casinowatch", "Casino Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class CasinoWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Casino.Played += args => Log.Info("Played a " + args.Machine.Kind + " for " + args.Stake + " OC.");
        Casino.Won += args => Log.Info("The " + args.Machine.Kind + " won " + args.Payout + " OC.");
        Casino.CashedOut += args => Log.Info("You took " + args.Amount + " OC out of the " + args.Machine.Kind + ".");
    }
}
```

`Played`, `Won` and `CashedOut` are events: the framework calls your code each time they happen. See [Using events](../guides/getting-started/using-events.md). A win is money won inside the machine. The money only reaches the player's wallet when they cash out.
