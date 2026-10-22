---
title: Pay the player
parent: Cookbook
nav_order: 2
---

# Pay the player

Pay the player 50 OC (Open Sewer Coins, the game's main currency) once per game day. Use this when your mod rewards the player over time. The paid day is saved with the game, so loading a save does not pay twice. See [OC](../../glossary.md#oc).

`PayState` holds the number of the last day paid. `SaveData.Bind` keeps it with each save. Every game hour the mod checks whether today was paid yet, and if not, calls `Cash.Give`.

```csharp
using System;
using ObenseuerCore;

[Serializable]
public class PayState
{
    // The game day that was paid last, or -1 if never.
    public int lastPaidDay = -1;
}

[ModInfo("example.payplayer", "Pay Player", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class PayPlayer : FrameworkMod
{
    // How much to pay each day.
    private const int DailyOc = 50;
    // The saved data. Its Value is never null.
    private SaveSlot<PayState> slot;

    protected override void OnLoad()
    {
        // Keep PayState with each save, under the key "pay".
        slot = SaveData.Bind<PayState>("example.payplayer", "pay");
        // Check once every game hour.
        Events.HourPassed += OnHour;
    }

    private void OnHour(int hour)
    {
        // Skip while a save or load runs, or when today was paid already.
        if (SaveData.Busy || slot.Value.lastPaidDay == GameTime.Day) return;
        // Pay, and remember today only if the payment worked.
        if (Cash.Give(MoneyCurrency.OC, DailyOc))
        {
            slot.Value.lastPaidDay = GameTime.Day;
            return;
        }
        // false means the wallet was full or the game was not ready.
        Log.Warn("the payment was refused");
    }
}
```

Replace the code in `Plugin.cs` of your project from [Your first mod](../getting-started/first-mod.md) with the code above. Then run `dotnet build -c Release -p:DeployToGame=true` with the game closed, and start the game. Load a save and wait until the next full game hour. You receive 50 OC, and the next hour does not pay again.

Notes:

- `SaveData.Bind` returns a slot whose `Value` is never `null`, so you do not need a null fallback.
- The check runs every hour, not once a day. A day that is missed during a save is paid in a later hour.
- `Cash.Give` takes whole OC amounts. For money in a bank account, use `Banking.Refund` instead. See [Gameplay and economy](../gameplay.md#money).
- `Cash.Give` returns `false` when the amount does not fit in the wallet or the game is not ready, so the code above logs a warning. See [Money and cash limits](../../api/economy/money-cash.md#limits).
- Save data is covered in [Save data](../getting-started/save-data.md).

## Where to go next

Next: [React to a new day](react-to-a-new-day.md).
