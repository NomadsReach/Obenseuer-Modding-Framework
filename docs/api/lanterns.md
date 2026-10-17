---
title: Lanterns
parent: Objects and devices
grand_parent: API reference
nav_order: 9
---

# Lanterns

## What it is

A lantern is an object placed in the world, not an item you carry. The player puts a candle from their inventory into it, and the candle burns for a while until it runs out. This API lets your mod list the lanterns in the scene you are in, read whether each one is lit and how many seconds of candle are left, and get told when a candle burns out. The candle itself is an inventory item: see [Inventory](inventory.md).

## When to use it

- Log or react when a lantern's candle burns out.
- Show which lanterns in a scene are lit.
- Check how long a candle has left.

## Quick example

This mod writes a log line when a candle burns out. When the game is ready, it also logs how many lanterns in the scene are lit.

```csharp
using ObenseuerCore;

[ModInfo("example.lanternwatch", "Lantern Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class LanternWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Lanterns.CandleBurntOut += args => Log.Info("a candle burnt out at " + args.Position);
        Events.WhenReady(() =>
        {
            int lit = 0;
            foreach (LanternInfo lantern in Lanterns.All())
                if (lantern.Lit) lit++;
            Log.Info(lit + " of " + Lanterns.All().Count + " lanterns are lit");
        });
    }
}
```

`CandleBurntOut` is an event: the framework calls your code each time it happens. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.

## Common tasks

Look up one lantern by its id. `Get` returns null when no lantern has that id.

```csharp
LanternInfo lantern = Lanterns.Get("some-lantern-id");
if (lantern != null) Log.Info("lit: " + lantern.Lit + ", seconds left: " + lantern.RemainingSeconds);
```

List the lanterns that are lit and how long their candles have left.

```csharp
foreach (LanternInfo lantern in Lanterns.All())
    if (lantern.Lit) Log.Info(lantern.Id + " has " + lantern.RemainingSeconds + " seconds left");
```

## Reference

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public static class Lanterns
{
    public static List<LanternInfo> All();       // every lantern in the scene you are in
    public static LanternInfo Get(string id);    // the lantern with this id; null when no lantern has it

    public static bool IsAvailable { get; }                           // false when the CandleBurntOut event could not be set up
    public static event Action<CandleBurntOutArgs> CandleBurntOut;    // a candle burnt out
}

public sealed class LanternInfo    // a snapshot of one lantern
{
    public string Id { get; }              // names this lantern; see the first bullet below
    public Vector3 Position { get; }       // where the lantern is, in world coordinates
    public bool Lit { get; }               // true when a candle is burning in it
    public int RemainingSeconds { get; }   // seconds of candle left; 0 when the lantern is not lit
}

public sealed class CandleBurntOutArgs    // what you get when a candle burns out
{
    public string Id { get; }              // the id of the lantern, as LanternInfo.Id
    public Vector3 Position { get; }       // where the lantern is, in world coordinates
}
```

- **A lantern's `Id` names one lantern in the scene you are in.** If the game gave the lantern a save id, that is the id. If not, the id is the lantern's place in the scene, and it changes if someone edits the objects around it.
  - Technical: the id is the object's GUID when it has one, otherwise a scene path such as `Scene:Root[0]/Child[2]`.
- **`All()` lists every lantern in the scene you are in, and `Get(id)` finds one.** A null or empty id finds nothing.
- **`RemainingSeconds` is a snapshot.** It counts down while the game runs, so ask again for the current value.
  - Technical: `Lit` is true when `Lantern.candleTimer.remainingInSeconds` is above 0.
- **`CandleBurntOut` fires when a candle's time runs out.** It does not fire when the player puts the candle out by hand, or when another script puts out a candle that still has time left. The position and id in the event are read after the game has put the candle out.
  - Technical: the framework changes `Lantern.CandleExtinguished`. It reads the timer before the game's method runs, and fires only when `remainingInSeconds` was below 0 then.
- **After a save loads, every lantern whose candle ran out while the game was saved fires `CandleBurntOut` once, on the first timer tick.**

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.lanterns.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes one of the game's methods when it starts, so it can tell your mod when a candle burns out. This page's feature group is `obenseuer.moddingframework.lanterns`, the name you will see in the log.

### When you can call it

- **Call everything from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for every member on this page and for the code that handles `CandleBurntOut`.
- **It is safe to call any of it at any time, even on the main menu or while a save is loading.** If there is nothing to read yet, you get an empty list or `null`. Nothing throws, and no argument makes a call throw.
- **Every call only reads.** Nothing here changes a lantern.
- **Your `CandleBurntOut` handlers run inside the game's own call, one at a time.** If one of your handlers throws an error, the framework logs it and still runs the other handlers.
  - Technical: the event is raised synchronously after the game has finished putting the candle out.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- **If the change behind `CandleBurntOut` cannot be made, `Lanterns.IsAvailable` becomes `false` and `CandleBurntOut` never fires.** The feature group is turned off and error lines and a warning go to `BepInEx\logs\obenseuer.moddingframework.lanterns.log`. `All` and `Get` keep working.
  - Technical: the one change is on `Lantern.CandleExtinguished`.

### What you can't do (yet)

- **You can't light a lantern or put it out from your mod.** The game's own call that lights a lantern does not take the candle from the player's inventory, so a call from a mod would give a free candle. The game takes the candle only when the player interacts with the lantern, so no write is offered.
  - Technical: `Lantern.CandleInserted` does not take the candle from the inventory, while `Interact` does. Putting a candle out by hand is `Lantern.CandleExtinguished` through `Interact`.
- **You can't read how long a new candle will burn.** That comes from the candle item's fuel data, unless the lantern sets its own time.
  - Technical: it is the candle item's fuel data unless `overrideCandleTime` is set.
- **You can't read or change the candle item itself here.** It is an inventory item: see [Inventory](inventory.md).

### While a save is loading

The framework does not hold back `CandleBurntOut` while a save is loading. If your handler should ignore events raised during a load, check `SaveData.Busy` first. It is `true` while a save is loading or being written.
