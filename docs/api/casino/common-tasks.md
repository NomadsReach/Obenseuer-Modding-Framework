---
title: Common tasks
parent: Casino
grand_parent: Money and shops
nav_order: 1
---

# Common tasks

List the machines in the scene and the stake each one is set to.

```csharp
foreach (CasinoMachineInfo machine in Casino.Machines())
    Log.Info(machine.Kind + " " + machine.Id + ", stake " + machine.Stake);
```

Make every slot machine pay double. Payout factors are forgotten when a scene loads, so set it again each time a scene loads.

```csharp
Events.SceneLoaded += scene => Casino.SetPayoutFactor(Info.Id, CasinoMachineKind.Slot, 2f);
```

Take your payout factors back, so the machines pay what the game normally pays.

```csharp
Casino.Clear(Info.Id);
```

React when the player doubles a slot win.

```csharp
Casino.Doubled += args => Log.Info(args.Won ? "The double paid off." : "The double failed.");
```

React when a scratchcard wins.

```csharp
Casino.Scratched += args =>
{
    if (args.Won) Log.Info(args.CardTitle + " won " + args.PrizeOc + " OC worth.");
};
```

Read the last win, and check whether the events and payout factors work.

```csharp
Log.Info("The last win was " + Casino.LastPayout + " OC.");

if (!Casino.IsAvailable) Log.Warn("Casino events are off.");
if (!Casino.PayoutFactorsAvailable) Log.Warn("Payout factors are off.");
```
