---
title: Lanterns
parent: Objects and devices
grand_parent: API reference
nav_order: 9
---

# Lanterns

This page uses `Vector3` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

## What it is

A lantern is an object placed in the world, not an item you carry. The player puts a candle from their inventory into it, and the candle burns for a while until it runs out. This API lets your mod list the lanterns in the scene you are in, and read whether each one is lit and how many seconds of candle are left. It tells you when a candle burns out. The candle itself is an inventory item: see [Inventory](inventory.md).

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

Text in brackets at the end of a note is extra detail you can skip on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Lanterns</code> <span>the lanterns in the scene you are in</span></p>

Call `All()` or `Get(id)` to read the lanterns in the scene, and subscribe to `CandleBurntOut` to hear when a candle runs out.

<div class="api-pair" markdown="1">

```csharp
public static class Lanterns
{
    public static List<LanternInfo> All();
    public static LanternInfo Get(string id);

    public static bool IsAvailable { get; }
    public static event Action<CandleBurntOutArgs> CandleBurntOut;
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<LanternInfo>` | Every lantern in the scene you are in. |
| `Get(id)` | `LanternInfo` | The lantern with this id. It is `null` when no lantern has it. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the `CandleBurntOut` event could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `CandleBurntOut` | `CandleBurntOutArgs` | A candle burnt out. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>LanternInfo</code> <span>a snapshot of one lantern</span></p>

`All()` and `Get(id)` give you one of these for each lantern. `Lit` and `RemainingSeconds` are as they were when you asked. <span class="tech">(`RemainingSeconds` is the game's `candleTimer.remainingInSeconds`, or `0` when the lantern is not lit.)</span>

<div class="api-pair" markdown="1">

```csharp
public sealed class LanternInfo
{
    public string Id { get; }
    public Vector3 Position { get; }
    public bool Lit { get; }
    public int RemainingSeconds { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | Names this lantern. See the first note below. |
| `Position` | `Vector3` | Where the lantern is, in world coordinates. |
| `Lit` | `bool` | `true` when a candle is burning in it. |
| `RemainingSeconds` | `int` | Seconds of candle left. It is `0` when the lantern is not lit. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CandleBurntOutArgs</code> <span>what you get when a candle burns out</span></p>

`CandleBurntOut` gives you one of these when a candle burns out. Pass its `Id` to `Get` to find the lantern.

<div class="api-pair" markdown="1">

```csharp
public sealed class CandleBurntOutArgs
{
    public string Id { get; }
    public Vector3 Position { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The id of the lantern, as `LanternInfo.Id`. |
| `Position` | `Vector3` | Where the lantern is, in world coordinates. |

</div>
</div>

### Things to know

- A lantern's `Id` names one lantern in the scene you are in. If the game gave the lantern a save id, that is the id. If not, the id is the lantern's place in the scene, and it changes if someone edits the objects around it. <span class="tech">(The id is the object's GUID when it has one, otherwise a scene path such as `Scene:Root[0]/Child[2]`.)</span>
- `All()` lists every lantern in the scene you are in, and `Get(id)` finds one. A null or empty id finds nothing.
- `RemainingSeconds` is a snapshot. It counts down while the game runs, so ask again for the current value. <span class="tech">(`Lit` is true when `Lantern.candleTimer.remainingInSeconds` is above 0.)</span>
- `CandleBurntOut` fires when a candle's time runs out. It does not fire when the player puts the candle out by hand, or when another script puts out a candle that still has time left. The position and id in the event are read after the game has put the candle out. <span class="tech">(The framework changes `Lantern.CandleExtinguished`. It reads the timer before the game's method runs, and fires only when `remainingInSeconds` was below 0 then.)</span>
- After a save loads, every lantern whose candle ran out while the game was saved fires `CandleBurntOut` once, on the first timer tick.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.lanterns.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes one of the game's methods when it starts, so it can tell your mod when a candle burns out. This page's feature group is `obenseuer.moddingframework.lanterns`, the name you will see in the log.

### When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../glossary.md#main-thread)). This goes for every member on this page and for the code that handles `CandleBurntOut`.
- It is safe to call any of it at any time, even on the main menu or while a save is loading. If there is nothing to read yet, you get an empty list or `null`. Nothing throws, and no argument makes a call throw.
- Every call only reads. Nothing here changes a lantern.
- Your `CandleBurntOut` handlers run inside the game's own call, one at a time. If one of your handlers throws an error, the framework logs it and still runs the other handlers. <span class="tech">(The event is raised synchronously after the game has finished putting the candle out.)</span>

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework turns off the [feature group](../glossary.md#feature-group) that depends on it, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the change behind `CandleBurntOut` cannot be made, `Lanterns.IsAvailable` becomes `false` and `CandleBurntOut` never fires. The feature group is turned off and error lines and a warning go to `BepInEx\logs\obenseuer.moddingframework.lanterns.log`. `All` and `Get` keep working. <span class="tech">(The one change is on `Lantern.CandleExtinguished`.)</span>

### What you can't do (yet)

- You can't light a lantern or put it out from your mod. The game's own call that lights a lantern does not take the candle from the player's inventory, so a call from a mod would give a free candle. The game takes the candle only when the player interacts with the lantern, so no write is offered. <span class="tech">(`Lantern.CandleInserted` does not take the candle from the inventory, while `Interact` does. Putting a candle out by hand is `Lantern.CandleExtinguished` through `Interact`.)</span>
- You can't read how long a new candle will burn. That comes from the candle item's fuel data, unless the lantern sets its own time. <span class="tech">(It is the candle item's fuel data unless `overrideCandleTime` is set.)</span>
- You can't read or change the candle item itself here. It is an inventory item: see [Inventory](inventory.md).

### While a save is loading

The framework does not hold back `CandleBurntOut` while a save is loading. If your handler should ignore events raised during a load, check `SaveData.Busy` first. It is `true` while a save is loading or being written.
