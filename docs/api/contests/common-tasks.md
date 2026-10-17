---
title: Common tasks
parent: Contests
grand_parent: Player life and skills
nav_order: 1
---

# Common tasks

List the contests that are running, who is in them and how much the opponent has left. A tolerance of `-1` means there is nothing to read. `LastResult()` is the last contest that finished since the game was started, or null before the first.

```csharp
Events.WhenReady(() =>
{
    foreach (ContestInfo contest in Contests.Active())
    {
        Log.Info(contest.Name + " is running with " + Contests.Participants(contest).Count + " participants");
        float tolerance = Contests.OpponentTolerance(contest);
        if (tolerance >= 0f) Log.Info("The opponent has " + (int)(tolerance * 100f) + " percent left");
    }

    ContestResultInfo last = Contests.LastResult();
    if (last != null) Log.Info("The last contest was the " + last.Contest.Name);
});
```

End the player's drinking or sauna contest as a loss, the same as holding the interact key to give up. It returns `false` when there is nothing to give up.

```csharp
if (!Contests.GiveUp()) Log.Info("No contest to give up.");
```

Double the winnings of a rat fight when it starts, and take every factor your mod set back with `Clear`.

```csharp
Contests.Started += contest =>
{
    if (contest.Kind == ContestKind.RatFight && Contests.PayoutFactorsAvailable)
        Contests.SetPayoutFactor(Mod.Id, contest.Id, 2f);
};

Contests.Clear(Mod.Id);
```
