---
title: Obenseuer Modding Framework shop hours API (opening times)
parent: API reference
nav_order: 71
---

# Obenseuer Modding Framework shop hours API (opening times)

Experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.shophours`. Logs go to `BepInEx\logs\obenseuer.moddingframework.shophours.log`.

Everything not listed here is internal. Main thread only.

Scope: the opening schedule and open state of traders. The shop id is the one `Shops.All()` returns (`ShopInfo.Id`, the GUID of a trader or vending machine). Vending machines have no opening times: `Hours` is empty and both `IsOpen` and `IsScheduledOpen` are true for them.

## Rules

- **Read-only.** The facade does not change opening times or open and close a shop.
- **State and schedule.** `IsOpen` is the trader's open flag, the one the game shows the player as "(Trade)" or "(Closed)". `IsScheduledOpen` is the opening times at the current game time. They differ when a trader is held open or closed by the game (`autoOpenClose` off, or the player standing close by), and in the first half second after a scene starts.
- **Bad arguments.** A `null` shop id throws `ArgumentException`. An unknown id returns the fallback.

## API

```csharp
public static class ShopHours
{
    public static List<ShopDayInfo> Hours(string shopId);   // seven entries, Monday first; empty when unknown
    public static bool IsOpen(string shopId);                // the trader open flag; false when unknown or when no shop is loaded
    public static bool IsScheduledOpen(string shopId);       // the schedule at the current game time; false when unknown or when the game has no clock yet
}

public sealed class ShopDayInfo
{
    public DayOfWeek Day { get; }                            // System.DayOfWeek
    public int Opens { get; }                                // hour, 0 to 24
    public int Closes { get; }                               // hour, 0 to 24
}
```

- `Opens == Closes` means the shop is closed all day.
- `Opens > Closes` means the shop is open overnight. The game applies both windows to the same weekday row: from `Opens` until midnight and from midnight until `Closes`, on that day. A row does not carry on into the next day, so Monday `20` to `4` is open on Monday from 00:00 to 04:00 and from 20:00.
- `Opens < Closes` means the shop is open from `Opens` up to `Closes`.
- `Hours` starts at Monday (`DayOfWeek.Monday`), which is the game's weekday 0.
- The facade has no events and no patches, so there is no `IsAvailable`.
