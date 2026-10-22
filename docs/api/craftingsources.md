---
title: Crafting sources
parent: Items and crafting
grand_parent: API reference
nav_order: 1
---

# Crafting sources

This page uses `Vector3`, `Transform` from Unity. The [glossary](../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Status: experimental.

## What it is

When the player crafts at a station, the game only looks for ingredients in the station's own input slots. This API lets your mod add more places to look. You give the framework a small function that, for a station, returns the containers near it. The station then counts the items in those containers as ingredients, and takes the ingredients from them when it crafts.

A storage mod is the usual case: with it, a station can use the items in a chest standing next to it.

## When to use it

- Let crafting stations use the items in containers nearby, as a storage or "build from nearby chests" mod would.
- Let crafting stations use a special container that your mod adds to the world.

## Quick example

This mod lets every crafting station use the containers within 6 metres of it. It keeps a list of the containers in the scene and refreshes it whenever a scene loads. The provider function only reads that list, so it stays cheap.

```csharp
using System.Collections.Generic;
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.nearbystorage", "Nearby Storage", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NearbyStorage : FrameworkMod
{
    private const float Range = 6f;
    private readonly List<Storage> known = new List<Storage>();

    protected override void OnLoad()
    {
        Events.SceneLoaded += scene => Refresh();
        Events.WhenReady(Refresh);
        CraftingSources.Register(Mod.Id, Nearby);
    }

    private void Refresh()
    {
        known.Clear();
        known.AddRange(Object.FindObjectsByType<Storage>(FindObjectsSortMode.None));
    }

    private IEnumerable<Storage> Nearby(CraftingStationInfo station)
    {
        foreach (Storage storage in known)
        {
            if (storage != null && Vector3.Distance(storage.transform.position, station.Position) <= Range) yield return storage;
        }
    }
}
```

This example offers every container in range. A real mod would pick only the containers it wants to offer. `Storage` is the game's class for a container with item slots. `Mod.Id` is your mod's id (see the [glossary](../glossary.md#mod-id)). `Events.WhenReady` runs your code once a save is loaded, or at once if one already is. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

Stop offering containers, for example when the player turns your mod's option off. `Unregister` returns `false` when your mod had nothing registered.

```csharp
bool removed = CraftingSources.Unregister(Mod.Id);
Log.Info(removed ? "Nearby containers are no longer used." : "Nothing was registered.");
```

Offer containers only to one kind of station. `CraftingStationInfo.Kind` is the name of the station's type, such as `Growing`. See [Crafting stations](craftingstations.md) for the station details. Here `known` is the list of containers from the quick example.

```csharp
private IEnumerable<Storage> OnlyForGrowing(CraftingStationInfo station)
{
    if (station.Kind != "Growing") return new List<Storage>();
    return known;
}
```

Log what a craft took out of your containers, liquid included. `Consumed` gives you one `CraftingTaken` for each slot of yours that changed.

```csharp
CraftingSources.Consumed += taken =>
{
    Log.Info("A craft took " + taken.Amount + " of item " + taken.ItemId + " from slot " + taken.SlotIndex + ".");
    if (taken.LiquidItemId != -1) Log.Info("It also poured " + taken.LiquidAmount + " of liquid " + taken.LiquidItemId + ".");
};
```

In that call, `ItemId` and `Amount` describe the container stack, and `LiquidItemId` and `LiquidAmount` describe a pour out of it. The liquid fields are `-1` and `0` when the slot lost no liquid. This is a report, not a permission: the liquid and the items are already gone when it fires.

## Reference

Text in brackets at the end of a note is extra detail you can skip on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CraftingSources</code> <span>adding and removing your containers</span></p>

Call `Register` with your mod id and a function that gives the containers near a station, and `Unregister` to remove it.

<div class="api-pair" markdown="1">

```csharp
public static class CraftingSources
{
    public static bool Register(string modId, Func<CraftingStationInfo, IEnumerable<Storage>> provider);
    public static bool Register(string modId, Func<CraftingStationInfo, IEnumerable<Storage>> provider, int priority);
    public static bool Register(string modId, Func<CraftingStationInfo, IEnumerable<Storage>> provider, int priority, Func<Storage, int, StorageSlot, bool> slotFilter);
    public static bool SetPriority(string modId, int priority);
    public static bool SetSlotFilter(string modId, Func<Storage, int, StorageSlot, bool> slotFilter);
    public static bool Unregister(string modId);
    public static event Action<CraftingTaken> Consumed;
}
```

| Method | Returns | What it does |
|---|---|---|
| `Register(modId, provider)` | `bool` | Adds your function. It is `true` when it was added. |
| `Register(modId, provider, priority)` | `bool` | The same, with a priority that decides where your containers come in the order. |
| `Register(modId, provider, priority, slotFilter)` | `bool` | The same, with a function that refuses single slots of your containers. |
| `SetPriority(modId, priority)` | `bool` | Changes the priority of a function you already registered. It is `false` when your mod had none. |
| `SetSlotFilter(modId, slotFilter)` | `bool` | Sets the per-slot filter, or clears it when you pass `null`. It is `false` when your mod had none. |
| `Unregister(modId)` | `bool` | Removes your function. It is `false` when your mod had none. |

| Event | Gives you | When it fires |
|---|---|---|
| `Consumed(item)` | `CraftingTaken` | A craft took an item out of one of your containers. One call per slot that changed. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CraftingTaken</code> <span>one slot a craft took from</span></p>

`Consumed` hands you one of these for each of your slots that a craft changed. `ItemId` and `Amount` are about the stack in that slot. When the craft also poured liquid out of it, `LiquidItemId` and `LiquidAmount` say what was poured.

<div class="api-pair" markdown="1">

```csharp
public sealed class CraftingTaken
{
    public string ModId { get; }
    public Storage Storage { get; }
    public int SlotIndex { get; }
    public int ItemId { get; }
    public int Amount { get; }
    public int LiquidItemId { get; }
    public float LiquidAmount { get; }
    public CraftingBase Station { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ModId` | `string` | The mod id whose function offered the container. |
| `Storage` | `Storage` | The container the slot belongs to. |
| `SlotIndex` | `int` | The number of the slot in that container. |
| `ItemId` | `int` | The item the container stack held, before the craft. |
| `Amount` | `int` | How many of that item are gone. `0` when the slot only lost liquid. |
| `LiquidItemId` | `int` | The liquid poured out of the stack, or `-1` when no liquid was taken. |
| `LiquidAmount` | `float` | How much liquid was poured out, or `0` when no liquid was taken. |
| `Station` | `CraftingBase` | The station that crafted. |

</div>
</div>

### Things to know

- `Register` adds your function for your mod id. When a station checks what it can craft, or crafts, the framework calls your function with that station.
- Your function returns the containers near that station. Use `CraftingStationInfo.Position` to find them, and `CraftingStationInfo.Id` names the station. <span class="tech">(`CraftingStationInfo.Id` is the station's GUID.)</span>
- Registering again with the same mod id replaces your function. It keeps its place in the order.
- `Unregister` removes your function. It returns `false` when your mod had none.
- A station looks in its own slots first, then in your containers. If several mods register, their containers come next, one mod after the other, in the order the mods registered and in the order each function returns them.
- A container is counted once, even if it is returned twice. That holds whether one function returns it twice or two functions both return it. The station's own input and output containers are never counted as extra.
- Your containers are used wherever a station decides what it can make. That means the list of recipes the station shows as craftable, the "how many can I make" count used by the grow amount and the process amount, and the decision to start a craft. The craft itself takes its ingredients from the same containers the check counted. <span class="tech">(Crafting covers `Craft`, `StartProcess`, `StartGrow` and `OpenLid`.)</span>
- A craft that the containers can no longer support is refused before anything is taken. If the start check fails, the station refuses without a message and refreshes its recipe list. If the containers change after the start check, the craft stops with an `InvalidOperationException` before any item is removed.
- Items are taken from the real containers. After a craft takes from one of your containers, the framework refreshes that container's slots so they show the change.
- Keep your function cheap, because it runs often. A station's recipe list makes one check for every recipe, so your function can run dozens of times when the list is rebuilt. Return containers you already know. Do not search the whole scene inside it.
- Return the same containers for the same station within one frame. The start check and the craft each call your function, and they must see the same containers.
- Taking items from your container does not trigger that container's own logic outputs. The game triggers them only for changes it makes itself. <span class="tech">(The outputs are `onUpdate`.)</span>
- `priority` decides where your mod's containers come in the order. A higher number comes first. The normal value is `0`, and mods with the same number keep the order in which they registered. Passing a priority to `Register` does not change when your function is called, only the order of the containers that are used. <span class="tech">(The order is your containers first, then the next mod's, highest priority first and registration order inside one priority.)</span>
- The per-slot filter is called for each slot of each of your containers, with the container, the slot number and the slot. Return `false` to keep that slot out of the craft. A refused slot stays in the list as an empty slot, so the number of slots the station sees does not change and the other slots keep their places. <span class="tech">(A refused slot is replaced by an empty `StorageSlot` in the combined list. The station's own slots, and the slots of the containers after it, must keep their positions because the game indexes that list by slot number while it crafts.)</span>
- `Consumed` tells you what a craft took from your containers. The framework writes down each slot of your containers before the take and compares it afterward, so you get one call per slot that lost items, with your mod id, the container, the slot number, the item id, the amount and the station. A stack that was only made smaller is reported with the amount that is gone. A pour out of a slot is reported with the liquid item id and the amount poured, in `LiquidItemId` and `LiquidAmount`. A refused craft, and a check that takes nothing, report nothing.
- `Consumed` is a report, not a permission. It fires after the items have been taken, so a handler cannot stop the take. Use it to update your own books.

### Limits

- `Register`, `SetPriority`, `SetSlotFilter` and `Unregister` all throw an `ArgumentException` for a mod id that is not valid (1 to 64 letters, digits, `.`, `_` or `-`). `Register` also throws an `ArgumentNullException` for a null function.
- `Register` returns `false` when the framework could not set up crafting. Then no function is used at all.
- Two things do not use your containers. The ingredient ticks in the recipe info panel only look at the station's own slots. The dishwasher never uses extra containers. <span class="tech">(`Dishwasher` is skipped on purpose. The invoice payment machine is not affected either, because it is not one of the crafting station classes this API changes (`manufacturing`, `manufacturingProcess`, `Growing`).)</span>
- While a save is loading or being written, your containers are not used. The station then uses only its own slots, as the game does without the framework. <span class="tech">(This is `SaveData.Busy`.)</span>
- Your function is skipped for one call if it throws an error or returns `null`. The error is logged once under your mod id, and the other functions still run.
- Your slot filter is skipped for the slots it throws on, and that slot is left out of the craft; the error is logged once under your mod id. It runs while a station checks what it can make, so keep it cheap.
- Liquid in your containers is an ingredient like any other, and a craft draws it in place. <span class="tech">(The game reads and writes liquid through the same slot list the framework swaps. `CraftingBase.RemoveLiquids` pours out of the container's own slot, and the framework refreshes that container afterwards.)</span>
- `Consumed` reports a pour as well as an item stack. `LiquidItemId` and `LiquidAmount` carry it, and they are `-1` and `0` when the slot lost no liquid. A pour that leaves the container stack untouched is reported with `Amount` `0`, so read the liquid fields as well as `ItemId`. When a craft uses a whole liquid container as an item ingredient, one call carries both: `Amount` is the number of containers gone and `LiquidAmount` is the liquid that was in them.
- The recipe list can be out of date while the station panel is open. It refreshes when the station's own slots change, when the panel opens, and after each craft. A change in one of your containers while the panel is open does not refresh it. The start check still refuses a craft that is no longer possible and refreshes the list at that point.
- A craft that starts a timed process can fail after its start outputs have fired. The game triggers the process's start outputs after the start check and before it takes the items. If those outputs empty or change your containers, the take check refuses after the outputs have fired, and nothing is taken. The framework can't check earlier without changing those outputs. <span class="tech">(This is `manufacturingProcess.StartProcess`.)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Files: `src/Api/CraftingSources.cs`, `src/Api/CraftingSourcesPatches.cs`, `src/Api/CraftingSourcesSelfTest.cs`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes some of the game's methods when it starts, so a station can use your containers. All of those changes belong to one feature group, `obenseuer.moddingframework.craftingsources`, the name you will see in the log. Errors from your own function are logged under your mod id.

### When you can call it

- Call `Register` and `Unregister` from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../glossary.md#main-thread)). The framework calls your function on the main thread too.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework turns off the [feature group](../glossary.md#feature-group) that depends on it, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the crafting changes can't be set up, `Register` returns `false` and no function is used. The feature group `obenseuer.moddingframework.craftingsources` turns off and error lines and a warning go to the log. <span class="tech">(The game methods involved are `CraftingBase.CompareRecipeToItems`, `CountItems`, `RemoveItems` and `UpdateSlots`, plus `manufacturing.Craft`, `manufacturingProcess.StartProcess` and `Growing.StartGrow`.)</span>
