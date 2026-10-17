---
title: Shop hours
parent: Money and shops
grand_parent: API reference
nav_order: 8
---

# Shop hours

Status: experimental.

## What it is

Traders in the game open and close at set hours, and the player sees "(Trade)" or "(Closed)" next to a trader's name. This API reads those opening hours for each day of the week and tells you whether a trader is open right now. It only reads. It can't change opening hours or open or close a shop.

## When to use it

- Show the opening hours of a trader in your own screen.
- Check whether a trader is open before your mod sends the player there.
- Find out why a trader is closed: the clock says closed, or the game is holding it closed.

## Quick example

This mod, once a save is loaded, logs for each trader whether it is open now and whether its opening hours say it should be.

```csharp
using ObenseuerCore;

[ModInfo("example.hourswatch", "Hours Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class HoursWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (ShopInfo shop in Shops.All())
                if (shop.Kind == ShopKind.Trader)
                    Log.Info(shop.Name + ": open=" + ShopHours.IsOpen(shop.Id) + ", by the clock=" + ShopHours.IsScheduledOpen(shop.Id));
        });
    }
}
```

`Shops.All()` is on the [Economy](economy/shops.md#shops) page. It gives each shop's `Id`, which is what you pass to `ShopHours`. `WhenReady` runs your code once the game is ready. See [Using events](../guides/getting-started/using-events.md) for more on it.

## Common tasks

Log the opening hours of one shop, one line per day.

```csharp
string shopId = "put a shop id from Shops.All() here";
foreach (ShopDayInfo day in ShopHours.Hours(shopId))
    Log.Info(day.Day + ": " + day.Opens + " to " + day.Closes);
```

Check whether a shop is open to the player now.

```csharp
string shopId = "put a shop id from Shops.All() here";
if (ShopHours.IsOpen(shopId)) Log.Info("The shop is open.");
```

Find out whether the game is holding a shop open or closed apart from its opening hours.

```csharp
string shopId = "put a shop id from Shops.All() here";
if (ShopHours.IsOpen(shopId) != ShopHours.IsScheduledOpen(shopId))
    Log.Info("The shop is not following its opening hours right now.");
```

## Reference

This is the full list of what the API gives you. In the code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

### Opening hours

`ShopHours` reads the opening hours of a trader and whether it is open. A shop is named by its `Id`, which you get from `Shops.All()` on the [Economy](economy/shops.md#shops) page.

```csharp
public static class ShopHours
{
    public static List<ShopDayInfo> Hours(string shopId);   // seven days, Monday first; empty when the shop is not found
    public static bool IsOpen(string shopId);                // true when the trader is open to the player now; false when not found
    public static bool IsScheduledOpen(string shopId);       // true when the opening hours say open at the current game time; false when not found
}

public sealed class ShopDayInfo    // the opening hours for one day
{
    public DayOfWeek Day { get; }    // the weekday (System.DayOfWeek)
    public int Opens { get; }        // the hour it opens, 0 to 24
    public int Closes { get; }       // the hour it closes, 0 to 24
}
```

- **You can only read opening hours.** You can't change a shop's opening hours or open and close a shop. The framework only reads what the game has.
- **Vending machines have no opening hours.** For a vending machine, `Hours` is empty and `IsOpen` and `IsScheduledOpen` are both `true`.
  - Technical: the shop id is `ShopInfo.Id`, the `GUID` of a `Trade` (a trader) or a `VendingMachine`.
- **`Hours` always lists seven days, Monday first.**
  - Technical: Monday is the game's weekday 0.
- **If `Opens` and `Closes` are the same hour, the shop is closed all day.**
- **If `Opens` is before `Closes`, the shop is open from `Opens` until `Closes`.**
- **If `Opens` is after `Closes`, the shop is open late into the night, but the night part counts for the same weekday row.** A row does not carry on into the next day. For example, Monday with `Opens` 20 and `Closes` 4 means open on Monday from midnight until 04:00 and again from 20:00.
  - Technical: the game applies both windows (`Opens` until midnight, and midnight until `Closes`) to the same weekday row.
- **`IsOpen` and `IsScheduledOpen` can disagree.** `IsOpen` is what the player sees: "(Trade)" or "(Closed)". `IsScheduledOpen` is only what the opening hours say at the current game time. The game can hold a trader open or closed apart from the clock, for example while the player is standing close by, and for a short moment right after a scene starts.
  - Technical: `IsOpen` reads `Trade.open`. `IsScheduledOpen` reads `OpeningTimes.IsOpen()`. They differ when the trader has `autoOpenClose` off or the player is near, and in the first half second after a scene starts.
- **A shop that is not found gives the plain default, and a `null` id throws an error.** An unknown id, an empty id, or a shop that is not in the scene you are in gives an empty list or `false`. A `null` id throws an `ArgumentException`.
  - Technical: the lookup covers only the active scene.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.shophours.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

### When you can call it

- **Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- **It is safe to call any of it at any time, even on the main menu or while a save is loading.** If there is nothing to read yet, you get an empty list or `false`.
  - Technical: if the game itself fails, you get the same plain default and the failure goes to the log at debug level.

### If something fails to set up

- **There is nothing to set up here.** The framework does not change any game method for shop hours, so there is no feature group for this page, no events and no `IsAvailable`.
