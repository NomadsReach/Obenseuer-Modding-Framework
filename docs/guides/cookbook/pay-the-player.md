---
title: Pay the player
parent: Cookbook
nav_order: 2
---

# Pay the player

Pay 50 OC once per game day. The paid day is saved, so a reload does not pay twice.

1. Build and copy the DLL, as in [Your first mod](../getting-started/first-mod.md).

```csharp
using System;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

[Serializable]
public class PayState
{
    public int lastPaidDay = -1;
}

[BepInPlugin("example.payplayer", "Pay Player", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.10.0")]
public class PayPlayer : BaseUnityPlugin
{
    private const int DailyOc = 50;
    private SaveSlot<PayState> slot;

    private void Awake()
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

On BepInEx 5, write `"0.10.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](../getting-started/packaging.md#supporting-bepinex-5-too).

Notes:

- `SaveData.Bind` returns a slot whose `Value` is never `null`, so you do not need a null fallback.
- The check runs every hour, not once a day. A day that is missed during a save is paid in a later hour.
- `Cash.Give` takes whole OC amounts. For money in a bank account, use `Banking.Refund` instead. See [Gameplay and economy](../gameplay.md#money).
- Save data is covered in [Save data](../getting-started/save-data.md).
