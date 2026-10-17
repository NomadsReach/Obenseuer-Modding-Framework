---
title: Tanks
parent: Objects and devices
grand_parent: API reference
nav_order: 18
---

# Tanks

## What it is

A tank is a container in the world that holds a liquid, such as water. It has a liquid, an amount, a capacity (the most it can hold), and sometimes a lock. This API lets your mod list the tanks in the scene you are in, read what they hold, fill one with a liquid, empty one, lock or unlock one, and get told when your mod changes a tank.

It does not cover the pipes between tanks, the items used to fill and drain tanks, or the tank's screen in the game.

## When to use it

- Check how much water a tank holds.
- Fill a tank with a liquid as a quest reward, or empty it as a penalty.
- Lock a tank so the player can't use it, or unlock it.
- React when your mod (or another mod) changes a tank through this API.

## Quick example

This mod logs every tank in the scene when the game is ready, with the liquid it holds and how full it is. It also writes a log line each time a tank is changed through this API.

```csharp
using ObenseuerCore;

[ModInfo("example.tankwatch", "Tank Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TankWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Tanks.Changed += args => Log.Info("tank " + args.Id + " now holds " + args.Amount + " of item " + args.LiquidItemId);
        Events.WhenReady(() =>
        {
            foreach (TankInfo tank in Tanks.All())
                Log.Info(tank.Id + ": " + tank.Amount + " of " + tank.Capacity + ", liquid item " + tank.LiquidItemId);
        });
    }
}
```

`Changed` is an event: the framework calls your code each time it happens. It only fires for changes made through this API, not for the player filling or draining a tank (see the reference below). See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.

## Common tasks

Fill a tank with 50 units of clean water. `Set` returns true when the tank now holds that liquid at that amount. The number 10840 is the item id of clean water.

```csharp
if (Tanks.Set("some-tank-id", 10840, 50f)) Log.Info("the tank is filled");
```

Empty a tank.

```csharp
Tanks.Empty("some-tank-id");
```

Lock a tank, then unlock it.

```csharp
Tanks.SetLocked("some-tank-id", true);
Tanks.SetLocked("some-tank-id", false);
```

Read one tank by its id. `Get` returns null when no tank has that id.

```csharp
TankInfo tank = Tanks.Get("some-tank-id");
if (tank != null) Log.Info("locked: " + tank.Locked + ", main water source: " + tank.Global);
```

## Reference

Each tank is a `TankInfo`, a snapshot of its state at the moment you asked. Ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Tanks</code> <span>read the tanks</span></p>

This is the class to start from. `All()` and `Get(id)` read the tanks, and `Set`, `Empty` and `SetLocked` change them.

<div class="api-pair" markdown="1">

```csharp
public static class Tanks
{
    public static List<TankInfo> All();
    public static TankInfo Get(string id);
    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<TankInfo>` | Every tank in the scene you are in. |
| `Get(id)` | `TankInfo` | The tank with this id. `null` when there is none. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` only when the framework turned this feature off. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TankInfo</code> <span>a snapshot of one tank</span></p>

`All()` and `Get(id)` give you one of these. `Amount` is how much liquid is in the tank and `Capacity` is the most it holds, and a `Capacity` below 0 means the tank is infinite.

<div class="api-pair" markdown="1">

```csharp
public sealed class TankInfo
{
    public string Id { get; }
    public int LiquidItemId { get; }
    public float Amount { get; }
    public float Capacity { get; }
    public bool Locked { get; }
    public bool Global { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | Names this tank. See the note on ids below. |
| `LiquidItemId` | `int` | The item id of the liquid. `0` when the tank holds nothing. |
| `Amount` | `float` | How much liquid is in the tank. |
| `Capacity` | `float` | The most the tank can hold. Below 0 when it is infinite. |
| `Locked` | `bool` | `true` when the tank is locked. `false` when it has no lock. |
| `Global` | `bool` | `true` when this is a tank that keeps its state across level changes. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Tanks</code> <span>change a tank</span></p>

`Set` fills a tank with a liquid, `Empty` empties it, and `SetLocked` locks or unlocks it. Each returns `false` when it could not make the change.

<div class="api-pair" markdown="1">

```csharp
public static class Tanks
{
    public static bool Set(string id, int itemId, float amount);
    public static bool Empty(string id);
    public static bool SetLocked(string id, bool locked);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Set(id, itemId, amount)` | `bool` | Puts the liquid in the tank at that amount. |
| `Empty(id)` | `bool` | Empties the tank. `true` when the tank holds no liquid afterwards, including when it was already empty. |
| `SetLocked(id, locked)` | `bool` | Locks or unlocks the tank. `false` when the tank has no lock. Otherwise `true` when the lock now matches `locked`. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Tanks</code> <span>your mod changed a tank</span></p>

The `Changed` event fires after your mod's `Set`, `Empty` or `SetLocked` changes a tank. It does not tell you about changes that the player or the game makes.

<div class="api-pair" markdown="1">

```csharp
public static class Tanks
{
    public static event Action<TankChangedArgs> Changed;
}
```

| Event | Gives you | When it fires |
|---|---|---|
| `Changed` | `TankChangedArgs` | Your mod changed a tank through this API. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TankChangedArgs</code> <span>what you get when a tank is changed</span></p>

The `Changed` event gives you one of these, with the tank's liquid, `Amount` and `Locked` as they are now.

<div class="api-pair" markdown="1">

```csharp
public sealed class TankChangedArgs
{
    public string Id { get; }
    public int LiquidItemId { get; }
    public float Amount { get; }
    public bool Locked { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The id of the tank, as `TankInfo.Id`. |
| `LiquidItemId` | `int` | The item id of the liquid now in the tank. `0` when empty. |
| `Amount` | `float` | How much liquid is in the tank now. |
| `Locked` | `bool` | `true` when the tank is locked now. |

</div>
</div>

### Things to know

- A tank's `Id` names one tank in the scene you are in. If the game gave the tank a save id, that is the id. If not, the id is the tank's place in the scene, and it changes if someone edits the objects around it. <span class="tech">(The id is the object's GUID when it has one, otherwise a scene path such as `Scene:Root[0]/Child[2]`.)</span>
- `All()` and `Get` read the current values every time. `All()` lists the tanks in the scene you are in, including the main water source.
- A `Capacity` below 0 means the tank is infinite. An infinite tank can't be filled to a limit. <span class="tech">(This follows the game's `HasInfiniteOutputCapasity`.)</span>
- `Global` is `true` for a tank that keeps its state when the level changes. The game saves it separately from the level. Other tanks are saved with their level. <span class="tech">(It is `LiquidStorage.global`. A global tank is saved in the cross-level save data under its GUID.)</span>
- `Set(id, itemId, amount)` puts that liquid in the tank at that amount. The amount is held between 0 and the tank's capacity, and rounded to a tenth, the way the game rounds a liquid level. A negative amount counts as 0. If the tank already holds the same liquid, only the amount changes and the liquid's quality and freshness stay. An empty tank takes the liquid fresh. <span class="tech">(The level is limited to 0 through `Capacity`, then rounded with `ItemLiquidData.SetAndGetLiquidAmount`. A value that rounds above `Capacity` is set to `Capacity`.)</span>
- `Set` with a different liquid throws away the liquid that was there. The game would refuse that without being forced, so this API makes the choice to replace it.
- `Set` with an amount of 0 leaves the tank holding that liquid at 0. To clear the tank properly, use `Empty`.
- `Empty(id)` empties the tank the way the game does, and the game's "emptied" triggers run. Quests or doors that wait for the tank to be emptied can react to this. <span class="tech">(It calls the game's `OnEmpty`, which clears the liquid and its metadata and fires the tank's `onEmpty` relay.)</span>
- `SetLocked` uses the game's own lock and unlock for the tank. <span class="tech">(It calls the game's `Lock` or `Unlock`.)</span>
- `Changed` only fires for changes made through this API. It fires after `Set`, `Empty` and `SetLocked`. It does not fire when the player or the game fills a tank, drains it, or changes it in any other way. If your mod needs to know about every change, read `All()` or `Get` on your own schedule. <span class="tech">(The game changes `LiquidAmount` in many places, and the framework does not change those methods.)</span>
- This page installs no changes to the game's methods, because everything it needs is public in the game. Its feature group is `obenseuer.moddingframework.tanks`, the name you will see in the log, and nothing can break it at startup.

### Limits

- You can't change the main water source. `Set`, `Empty` and `SetLocked` refuse the tank that supplies the game's main water. Reading it still works, with `Global` set. Changing it would change what the water supply reports for the whole game. <span class="tech">(This is the tank `WaterSourceController.instance.globalStorage` returns, which `WorldWater` reads for `Amount` and `IsInfinite`.)</span>
- `Set` returns `false` when it can't do it. That happens for an unknown item, an amount that is not a real number or is infinite, an unknown tank id, a tank with infinite or zero capacity, the main water source, or while a save is loading or being written. A tank with infinite or zero capacity is refused because the game draws its level as the amount divided by the capacity. Otherwise it returns `true` when the tank now holds that item at that amount.
- `Set` accepts any item that exists, not only liquids. The game does not check that the item is a liquid, so check that yourself.
- Changing an infinite tank usually does not change what the taps give. An infinite tank that has no outside storage takes its water from the main water source, and taps read that source, not the tank's own numbers. `Get` still reports the tank's own numbers. <span class="tech">(This happens for an infinite tank with no external output storage, unless `NotPartOfMainSource` is set.)</span>
- `Empty` runs the tank's "emptied" triggers. Emptying a tank can advance a quest or open a door that the tank's trigger drives, so think about that before you call it.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.tanks.log`.

Everything not listed in this reference is internal and may change without notice.

### When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)). This goes for every member on this page and for the code that handles `Changed`.
- It is safe to call any of it at any time, even on the main menu or while a save is loading. With no save loaded, `All` returns an empty list, `Get` returns `null`, and the changing calls return `false`. Nothing throws, and a null or empty id finds nothing.
- The changing calls return `false` while a save is loading or being written. <span class="tech">(The check is `SaveData.Busy`.)</span>

### If something fails to set up

- Nothing can fail to set up here. The framework installs no changes for this page, so `Tanks.IsAvailable` is `true` unless the feature group was turned off.
