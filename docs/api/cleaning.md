---
title: Cleaning
parent: Objects and devices
grand_parent: API reference
nav_order: 3
---

# Cleaning

## What it is

The tenement's apartments get dirty. The game keeps a cleaning timer for each apartment: a number of days that must pass before the apartment is ready. While the timer runs, the game shows a timer marker in the apartment. When the timer ends, the game takes away the thing that was blocking the apartment. This API lets your mod read that timer for any apartment. It also lists the piles of trash lying around in the scene you are in, and what is inside each pile.

The API only reads. It never starts a timer and never cleans anything. The apartment rules (tenants, rent, evictions) are on the [Housing](housing.md) page.

## When to use it

- Show or log whether an apartment is waiting on its cleaning timer, and on which day it ends.
- Count the trash piles in the scene, or look inside them for a particular item.
- Find out where the trash is, so your mod can point the player to it.

## Quick example

This mod waits for a save to load. Then it logs the cleaning timer of every apartment and counts the trash piles in the scene.

```csharp
using ObenseuerCore;

[ModInfo("example.cleaningwatch", "Cleaning Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class CleaningWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            foreach (ApartmentInfo apartment in Housing.AllApartments())
            {
                CleaningTimerInfo timer = Cleaning.Timer(apartment.Id);
                if (timer != null) Log.Info("Apartment " + apartment.Id + ": " + timer.State);
            }
            Log.Info("There are " + Cleaning.TrashItems().Count + " trash piles in this scene.");
        });
    }
}
```

`Housing.AllApartments()` gives you the apartment numbers to ask about. `Timer` returns `null` for a number the building does not have, which is why the example checks for it. See [Using events](../guides/getting-started/using-events.md) for more on `WhenReady`.

## Common tasks

Check whether one apartment is still waiting on its timer, and when the timer ends:

```csharp
CleaningTimerInfo timer = Cleaning.Timer(1);
if (timer != null && timer.State == CleaningTimerState.Running)
    Log.Info("The timer ends on day " + timer.CompleteDay);
```

Log what is in every trash pile, and where the pile is:

```csharp
foreach (TrashInfo pile in Cleaning.TrashItems())
    foreach (ItemStackInfo stack in pile.Items)
        Log.Info("item " + stack.ItemId + " x" + stack.Count + " at " + pile.Position);
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

### Cleaning timers

`Cleaning.Timer(apartmentId)` tells you where an apartment is in its cleaning timer. The answer is a `CleaningTimerInfo`, a snapshot of the timer at the moment you asked. The snapshot does not update by itself, so ask again for fresh values.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CleaningTimerState</code> <span>where a cleaning timer is</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum CleaningTimerState
{
    NotStarted,
    Running,
    Done
}
```

| Value | Meaning |
|---|---|
| `NotStarted` | no cleaning timer has been started for this apartment |
| `Running` | the timer is counting down the days |
| `Done` | the timer has ended and the game has removed what was blocking the apartment |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CleaningTimerInfo</code> <span>a snapshot of one apartment's cleaning timer</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class CleaningTimerInfo
{
    public int ApartmentId { get; }
    public CleaningTimerState State { get; }
    public int CompleteDay { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ApartmentId` | `int` | The apartment number you asked about. |
| `State` | `CleaningTimerState` | Where the timer is. |
| `CompleteDay` | `int` | The game day the timer ends. `0` unless `State` is `Running`. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Cleaning</code> <span>read the cleaning timers</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Cleaning
{
    public static CleaningTimerInfo Timer(int apartmentId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Timer(int apartmentId)` | `CleaningTimerInfo` | The timer for this apartment. `null` when the building has no such apartment. |

</div>
</div>

#### Things to know

- `Timer` gives you one of three states for the apartment. `NotStarted` means nobody has started a timer. `Running` means the timer is counting and `CompleteDay` is the game day it ends. `Done` means it has ended. <span class="tech">(the value is the apartment's `cleaningTimerCompleteDay` (on the game's `TenementApartment`). `-1` is `NotStarted`. A day number above `0` is `Running`, and `CompleteDay` is that day. `0` is `Done`: the game's `TenementController.OnCleaningTimerDayChanged` sets it when the day arrives and removes the blocker.)</span>

#### Limits

- `Timer` returns `null` for an apartment number the building does not have. It also returns `null` when no save is loaded, for example on the main menu.
- You can't start a cleaning timer. The game only starts one from its own story scripts, and it does not give mods a safe way to start one. For now you can only read the timer. <span class="tech">(the only game entry point is `TenementController.StartApartmentCleaningTimer`, which the game registers for its Lua scripts. The framework does not call it, because it is a write that the game drives through Lua.)</span>
- You can't read how much of an apartment's furniture has been cleared. The game keeps that as a separate measure from the timer, and this page does not cover it. <span class="tech">(the game's `TenementApartment.FurnitureClearPercent`.)</span>

### Trash piles

`Cleaning.TrashItems()` lists the piles of trash in the scene you are in. Each pile is a `TrashInfo`, a snapshot of the pile and the items inside it.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TrashInfo</code> <span>one pile of trash in the scene</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class TrashInfo
{
    public string Id { get; }
    public Vector3 Position { get; }
    public List<ItemStackInfo> Items { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The pile's save id. Can be `null`. |
| `Position` | `Vector3` | Where the pile is, in world coordinates. |
| `Items` | `List<ItemStackInfo>` | What is in the pile. Empty stacks are left out. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Cleaning</code> <span>list the trash piles</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Cleaning
{
    public static List<TrashInfo> TrashItems();
}
```

| Method | Returns | What it does |
|---|---|---|
| `TrashItems()` | `List<TrashInfo>` | Every trash pile in the active scene. |

</div>
</div>

#### Things to know

- Each pile lists its items the same way `Containers.GetItems` does. Empty stacks are left out, so a pile with nothing in it has an empty `Items` list. `Slot` on these stacks is `-1`, so do not pass them to a call that takes a slot.
- A pile does not say which apartment it is in. The game does not link a pile to an apartment. Use `Position` to work out where it is.
- `Id` can be `null`. Not every pile has a save id. Do not use it as a key unless you check for `null` first. <span class="tech">(it is the object's `FurnitureTrash.GUID`.)</span>

#### Limits

- `TrashItems()` only lists piles in the scene you are standing in. Piles in other scenes are not listed. It returns an empty list when no save is loaded. <span class="tech">(it lists every `FurnitureTrash` object in the active scene. Objects that are switched off are not included.)</span>
- You can't clean up or take trash with this API. The game removes trash one object at a time, when the player uses its cleaning mode. There is no call that cleans a whole apartment. <span class="tech">(cleaning mode is `BuildingSystemMenu.GetButtonInfoCleaning`. Taking trash is `FurniturePlaceable.Take`, which takes one object and has no apartment argument. The player triggers it with the Place Take key.)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`.

Everything not listed in this reference is internal and may change without notice.

This page changes none of the game's methods, so it has no feature group and nothing here can turn off after a game update.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call at any time, even on the main menu. With no save loaded, `Timer` returns `null` and `TrashItems` returns an empty list.
- Nothing here throws an error to your code, and nothing changes the game. If the game itself fails while the framework reads, you get `null` or an empty list, and the first failure of each member goes to the log.
