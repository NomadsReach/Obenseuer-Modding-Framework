---
title: Recycling
parent: Items and crafting
grand_parent: API reference
nav_order: 11
---

# Recycling

The bottle recycling machine takes bottles and cans from the player and owes them coins for it. The player collects the coins from the machine's screen. This API lists the machines in the scene you are in, reads and changes how many OC (Open Sewer Coins) a machine owes the player, and tells your mod when the player returns bottles. It does not hand the coins out. See the limits below.

## When to use it

- Show or log how much a recycling machine owes the player.
- Add a bonus to what a machine owes, or reset it to zero.
- React when the player returns bottles, for example to count them or give a reward.

## Quick example

This mod logs each bottle return: how many items the player returned and how many OC that added to what the machine owes.

```csharp
using ObenseuerCore;

[ModInfo("example.recyclingwatch", "Recycling Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class RecyclingWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Recycling.Recycled += args => Log.Info("Machine " + args.Id + ": " + args.Amount + " returned, +" + args.Money + " OC owed");
    }
}
```

`Recycled` is an event: the framework calls your code each time it happens. See [Using events](../guides/getting-started/using-events.md) for more on events.

## Common tasks

Log how much each machine in the scene owes the player.

```csharp
foreach (string id in Recycling.All())
    Log.Info(id + " owes " + Recycling.Owed(id) + " OC");
```

Add a 50 OC bonus to what a machine owes. `Owed` gives `-1` when the machine is not found, so check that first.

```csharp
string id = "put an id from All() here";
int owed = Recycling.Owed(id);
if (owed >= 0) Recycling.SetOwed(id, owed + 50);
```

Reset what a machine owes to zero.

```csharp
string id = "put an id from All() here";
Recycling.SetOwed(id, 0);
```

## Reference

This is the full list of what the API gives you. A machine is named by an id, a text name for that machine in the scene. You get the ids from `Recycling.All()` and pass them to the other members.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Recycling</code> <span>recycling machines in the scene</span></p>

Use it to list the machines in the scene, read and set what a machine owes the player, and hear when bottles are returned.

<div class="api-pair" markdown="1">

```csharp
public static class Recycling
{
    public static event Action<RecycledArgs> Recycled;
    public static bool IsAvailable { get; }
    public static List<string> All();
    public static int Owed(string id);
    public static bool SetOwed(string id, int owed);
}
```

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the `Recycled` event could not be set up. |

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<string>` | The id of every machine in the scene you are in. |
| `Owed(id)` | `int` | The OC the machine owes the player. `-1` when no machine has that id or no save is loaded. |
| `SetOwed(id, owed)` | `bool` | Sets what the machine owes. `true` when it was set. |

| Event | Gives you | When it fires |
|---|---|---|
| `Recycled` | `RecycledArgs` | The player returned bottles to a machine. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RecycledArgs</code> <span>one bottle return</span></p>

`Recycling.Recycled` hands you one of these for each return the machine accepts. `Money` is the OC that return really added, and `Amount` is the number of items.

<div class="api-pair" markdown="1">

```csharp
public sealed class RecycledArgs
{
    public string Id { get; }
    public int Amount { get; }
    public int Money { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The id of the machine. |
| `Amount` | `int` | How many items were returned. |
| `Money` | `int` | The OC this return added to what the machine owes. |

</div>
</div>

### Things to know

- The id is the machine's saved name, or its place in the scene when it has none. The place looks like `SceneName:Root[0]/Child[2]` and changes if objects in the scene are reordered. <span class="tech">(It is the object's `GUID` when it has one, otherwise the scene path.)</span>
- `All()` lists the machines in the scene you are in, and it is slow, so do not call it every frame. It searches the scene each time. A machine that is switched off in the scene is not listed. An empty list means there are none, or that something went wrong. <span class="tech">(It lists the scene id of every active `BottleRecycling` with `FindObjectsByType`. A machine with no id is skipped.)</span>
- `Owed(id)` is the OC the player can still collect from that machine. It gives `-1` when no machine has that id or no save is loaded. <span class="tech">(It reads `BottleRecycling.bottleRecyclingMoney`.)</span>
- `SetOwed(id, owed)` sets that amount directly. A negative amount returns `false` and changes nothing. It also returns `false` when no machine has that id, and while a save is loading or being written.
- Otherwise `SetOwed` returns `true`. It does not pay out anything and does not make the machine's screen show a payout. <span class="tech">(It writes `bottleRecyclingMoney`. The panel adds to this field when bottles are returned and pays it out one OC at a time, so a change made here is used the next time the panel counts. `SetOwed` does not fire the machine's `onGiveMoney` relay, which belongs to the payout.)</span>
- `Recycled` fires once for each return the machine accepts. `Amount` is how many items were in that return. `Money` is how many OC the return really added. A mushroom return, which the game gives a random value, reports the value the game actually added. <span class="tech">(It is raised after the game has added the value to `bottleRecyclingMoney`. `Money` is the change in that field across the game's call.)</span>
- `Recycled` does not fire when the machine refuses an item, and it does not fire when your mod calls `SetOwed`. A save loading or being written does not raise it either.
- A `Recycled` handler that throws an error does not stop the others. The framework logs the error and still runs the other handlers.
- If the player returns bottles while the machine is paying out, that payout stops. The rest of what the machine owes stays on it. `Money` still counts only the new return. Changing `SetOwed` while a payout is running changes how much is paid. <span class="tech">(The payout is a coroutine that counts `bottleRecyclingMoney` down while it runs.)</span>

### Limits

- You can't make a machine pay out. The game only pays out while the machine's screen is open, and a mod can't open that screen without opening the game menu. To change what the player gets, use `Owed` and `SetOwed`. <span class="tech">(`BottleRecyclingPanel.GiveCoins` is public, but it pays through `StartGiveMoney`, which reads the panel's `currentBottleRecycling` field. The game sets that field only in `Open`, which also opens the game menu, and clears it in `Close`. The game never sets it by itself with the menu closed.)</span>
- You can't read or change what each kind of bottle is worth. `Recycled` tells you what the game added, and that is all. <span class="tech">(The panel's category table in `BottleAdd` has no framework call to read or change it.)</span>
- You can't trigger the machine's payout relay. <span class="tech">(The framework has no call for the machine's `onGiveMoney` relay.)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.recycling.log`.

Everything not listed in this reference is internal and may change without notice.

The framework changes one game method when it starts, so it can tell your mod when bottles are returned. This page calls that a feature group: its name is `obenseuer.moddingframework.recycling`, the name you will see in the log.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call any of it at any time, even on the main menu or while a save is loading. If there is nothing to read yet, `All()` gives an empty list and `Owed` gives `-1`. A `null` or empty id finds nothing, and nothing is thrown to you.

### What to check for and what comes back

- `Recycling.IsAvailable` tells you whether `Recycled` works. It is `false` when the framework could not set up the event, or when the feature group was turned off. `All`, `Owed` and `SetOwed` keep working when it is `false`.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework removes all the changes of the [feature group](../glossary.md#feature-group) that depends on it, turns the group off, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the framework cannot change the game's bottle return step, `Recycled` stops and the rest keeps working. One warning goes to the log, and `IsAvailable` becomes `false`. <span class="tech">(The framework adds a prefix and a postfix to `BottleRecyclingPanel.BottleAdd`, the private method the panel runs for each returned item.)</span>
