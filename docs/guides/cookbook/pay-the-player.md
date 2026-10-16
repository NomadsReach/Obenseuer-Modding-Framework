---
title: Pay the player
parent: Cookbook
nav_order: 2
---

# Pay the player

Pay 50 OC once per game day. The paid day is saved, so a reload does not pay twice.


```csharp
using System;
using ObenseuerCore;

[Serializable]
public class PayState
{
    public int lastPaidDay = -1;
}

[ModInfo("example.payplayer", "Pay Player", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class PayPlayer : FrameworkMod
{
    private const int DailyOc = 50;
    private SaveSlot<PayState> slot;

    protected override void OnLoad()
    {
        slot = SaveData.Bind<PayState>("example.payplayer", "pay");
        Events.HourPassed += OnHour;
    }

    private void OnHour(int hour)
    {
        if (SaveData.Busy || slot.Value.lastPaidDay == GameTime.Day) return;
        if (Cash.Give(MoneyCurrency.OC, DailyOc)) slot.Value.lastPaidDay = GameTime.Day;
    }
}
```

Replace the code in `Plugin.cs` of your project from [Your first mod](../getting-started/first-mod.md) with the code above. Then run `dotnet build -c Release -p:DeployToGame=true` and start the game.

Notes:

- `SaveData.Bind` returns a slot whose `Value` is never `null`, so you do not need a null fallback.
- The check runs every hour, not once a day. A day that is missed during a save is paid in a later hour.
- `Cash.Give` takes whole OC amounts. For money in a bank account, use `Banking.Refund` instead. See [Gameplay and economy](../gameplay.md#money).
- Save data is covered in [Save data](../getting-started/save-data.md).
