---
title: Inventory sources
parent: Items and crafting
grand_parent: API reference
nav_order: 18
---

# Inventory sources

Status: experimental. Not checked in the game.

## What it is

Two things in the game take what they need straight from the player, and not from a station's own slots: the furniture workbench, where a blueprint is built at the counter, and the contractor, who renovates an apartment. This API lets your mod add extra containers to those two. You give the framework a small function that returns containers, and the furniture workbench and contractor renovations then count the items in them as available and take what they need from them.

A storage mod is the usual case: with it, a renovation can be paid for out of a chest in the room, or a blueprint built from the stock your mod keeps in its own containers.

The player's own items answer a check first, and your containers fill the rest. When the items are actually paid for, it is the other way round: your containers are drawn first and the player's own items cover what is left.

Ammo and liquid count as well. Rounds in your containers are counted and taken as rounds. An item the game tags as a liquid counts and is taken as a whole item, like any other; a container that holds that liquid is not counted, and a take gives one such container up for each item still needed once no loose stack of it is left.

## When to use it

- Let the furniture workbench take the ingredients of a blueprint from the containers your mod offers.
- Let contractor renovations take their ingredients from the containers your mod offers.

## Quick example

This mod offers the containers it knows about to both flows. It keeps a list of them and refreshes that list when a scene loads, so the function itself only reads the list and stays cheap.

```csharp
using System.Collections.Generic;
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.workshopstorage", "Workshop Storage", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class WorkshopStorage : FrameworkMod
{
    private readonly List<Storage> known = new List<Storage>();

    protected override void OnLoad()
    {
        Events.SceneLoaded += scene => Refresh();
        Events.WhenReady(Refresh);
        InventorySources.Register(Mod.Id, Sources);
    }

    private void Refresh()
    {
        known.Clear();
        foreach (Storage storage in Object.FindObjectsByType<Storage>(FindObjectsSortMode.None))
        {
            if (storage != null && storage.name.StartsWith("Pallet")) known.Add(storage);
        }
    }

    private IEnumerable<Storage> Sources(InventorySourceContext context)
    {
        return known;
    }
}
```

This example offers the same containers to both flows. A real mod would pick with `context.Kind`, and would keep its own containers rather than match them by name. `Storage` is the game's class for a container with item slots. `Mod.Id` is your mod's id (see the [glossary](../glossary.md#mod-id)). `Events.WhenReady` runs your code once a save is loaded, or at once if one already is. See [Using events](../guides/getting-started/using-events.md).

## Common tasks

Stop offering containers, for example when the player turns your mod's option off. `Unregister` returns `false` when your mod had nothing registered.

```csharp
bool removed = InventorySources.Unregister(Mod.Id);
Log.Info(removed ? "Your containers are no longer used." : "Nothing was registered.");
```

Offer containers to one flow only. `Kind` says which of the two is running. Here `known` is the list of containers from the quick example.

```csharp
private IEnumerable<Storage> OnlyForRenovations(InventorySourceContext context)
{
    if (context.Kind != "Renovation") return new List<Storage>();
    return known;
}
```

Offer different containers per flow, for example a pallet for building and a cupboard for renovations.

```csharp
private IEnumerable<Storage> ByKind(InventorySourceContext context)
{
    if (context.Kind == "FurnitureBlueprint") return pallets;
    if (context.Kind == "Renovation") return cupboards;
    return new List<Storage>();
}
```

## Reference

Text in brackets at the end of a note is extra detail you can skip on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>InventorySources</code> <span>adding and removing your containers</span></p>

Call `Register` with your mod id and a function that gives your containers, and `Unregister` to remove it.

<div class="api-pair" markdown="1">

```csharp
public static class InventorySources
{
    public static bool Register(string modId, Func<InventorySourceContext, IEnumerable<Storage>> provider);
    public static bool Unregister(string modId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Register(modId, provider)` | `bool` | Adds your function. It is `true` when it was added. |
| `Unregister(modId)` | `bool` | Removes your function. It is `false` when your mod had none. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>InventorySourceContext</code> <span>which flow is asking</span></p>

Your function is called with one of these. Use it to decide which containers to offer.

<div class="api-pair" markdown="1">

```csharp
public sealed class InventorySourceContext
{
    public string Kind { get; }
    public string Id { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Kind` | `string` | Which of the two flows is asking. See the table below. |
| `Id` | `string` | The id of the thing being checked or crafted. It is `null` in this version, because the game gives the framework no id for a blueprint or for a renovation. |

</div>
</div>

| `Kind` value | When your function is called with it |
|---|---|
| `"FurnitureBlueprint"` | The furniture workbench checks the blueprint the player picked, or builds it. |
| `"Renovation"` | The contractor checks what a renovation needs, or takes those items for it. |

### Things to know

- `Register` adds your function for your mod id. The framework calls it on every check and every take, with the kind above, which for a blueprint or a renovation order means once for each required item each time the screen refreshes, and once more when a screen asks for a have count or a workbench builds its ingredient rows. Return containers you already know.
- Your function runs on the main thread, and only while one of the two flows is running. Everything else in the game that asks how many of an item the player has is untouched, so your containers are not counted by the bank, by item givers, by trade or by the player's own panels. <span class="tech">(The scope is opened by `TaskItemBlueprint.PlayerHasResources` and `ShopBaseUI.Craft` for a blueprint, by `FurnitureResource.AddResourceInfo` while a workbench craft is running, and by `TenementController.CheckForItem`, `TenementController.GetItemAmount` and `TenementUpgrade.TakeRequiredItemsAndMoney` for a renovation. `Inventory.CheckForItem` has callers all over the game, which is why the count is only added while one of those calls is running.)</span>
- A check is answered by the player's own items when they cover the whole requirement, and your containers make up the rest. Taking is the other way round: the framework takes its part out of your containers first, and what is left is covered by the player's own items, which for a renovation come after the tenement's own storages. A requirement can therefore be paid partly from your containers and partly from the player.
- A renovation is checked against the storages of the tenement the player is in. When the contractor is not the one in the current scene, the game instead uses a snapshot it saved earlier, and then falls through to the player inventory, where your containers are added. The two are mixed in one answer: the game's saved amounts and the containers of the scene you are in now. <span class="tech">(The snapshot is `TenementController.resourceStorageSnapshotsByContractor`, filled by the game when it saves; the call then reaches `Inventory.CheckForItem`.)</span>
- The renovation screen shows a have count beside each required item, and the workbench shows an ingredient row for each thing a blueprint needs. Both of those include your containers, so a row agrees with what the craft will do. <span class="tech">(The have count is `TenementController.GetItemAmount`, which ends in `Inventory.GetItemAmountInInventory`, and the ingredient rows are built by `FurnitureResource.AddResourceInfo` while a craft is running, which reads `Inventory.GetItemAmountInInventory` or, for an ammo item, `Inventory.FindAndGetTotalAmmo`. The framework opens a read scope in both. The row for a furniture item is the exception and still reads the world.)</span>
- A container is counted once, even if it is returned twice, whether one function returns it twice or two functions both return it.
- Four things are never used, even if you return them: the player's backpack, a container with no slots, a container you already offered, and, for renovations, the tenement's own resource storages. <span class="tech">(The tenement storages are the ones the game itself uses for a renovation, so they are dropped from your list; the game reads them from `TenementController.tenementResourceStorages`.)</span>
- There is no priority and no per-slot filter. Your containers are used in the order the mods registered, and inside one mod in the order your function returns them. When two functions both offer the same container, the first one to offer it keeps it.
- Ammo in your containers counts and is taken. Rounds count on top of the player's own rounds, whatever the owner of the stack, and the take comes out of your containers first, the same way round as items. A stack of rounds that belongs to someone else, which is how a stolen one is marked, still counts, but the take skips it unless the caller asks to ignore owners, the same as the game's own ammo pair does for the player's stacks, so a blueprint can pass its check on rounds the take then leaves alone. <span class="tech">(The count is a postfix on `Inventory.FindAndGetTotalAmmo` through `InventorySources.CountAmmo`, which sums `ItemAmmodata.ammo` over every slot holding that item, and the take is a prefix on `Inventory.FindAndChangeItemAmmo` through `InventorySources.TakeAmmo`. The take runs only while items are being consumed, so a check leaves your rounds alone. `Inventory.FindAndGetTotalAmmo` reads every stack and `Inventory.FindAndChangeItemAmmo` skips an owned one, which is the same pair of answers the game gives for the player's own rounds.)</span>
- Rounds come out of the first slot of your containers that holds that ammo type, one round per call, which is how the workbench takes the rounds a blueprint needs. That slot is chosen once, and once it runs out of rounds the later calls take nothing and do not fall through to the player's own ammo, exactly as the game behaves for the player's own stacks. <span class="tech">(`Economy/ShopBaseUI.cs:794` calls `Inventory.FindAndChangeItemAmmo` once per round, and `InventorySources.TakeAmmo` mirrors the game's own slot pick: the first slot holding that item that carries ammo data.)</span>
- A stack of rounds that carries no ammo data counts nothing and is never taken from, so a container of loose round items is not an ammo source.
- An item the game tags as a liquid counts and is taken as a whole item, like any other, so a blueprint or a renovation that needs one can draw it from a stack in your containers. A container that holds that liquid is not counted, because both checks count whole items, but a take gives one such container up for each item still needed, which is what the game's own `Inventory.RemoveItems` does with the player's own containers. <span class="tech">(A take goes through `InventorySources::Take`: matching stacks first, through `Storage.RemoveItem`, and then, for an item whose `Categories` contain `Liquid` (`InventorySources::IsLiquidItem`), one container per item still needed from the slots whose stack carries that liquid, through `SlotController.TakeItem`. What it returns is the whole number of items given up, stacks and containers together.)</span>
- A blueprint that needs furniture cannot use your containers. Furniture means a piece the player has already placed, so the game looks for it in the world and not in a container. <span class="tech">(That part of a blueprint is checked with `BuildingSystem.PlayerHasFurniture` and taken with `BuildingSystem.RemoveFurniture`.)</span>
- Registering again with the same mod id replaces your function and keeps its place in the order.
- `Unregister` removes your function. It returns `false` when your mod had none.
- The framework does not check where a container is, who owns it or whether the player can reach it. It uses the containers your function returns, so the choice is yours.
- Keep your function cheap. It runs on the main thread on every check and every take, so a screen full of required items calls it many times, and it is the only place your containers are chosen. Do not search the whole scene inside it; keep a list and refresh it on a scene load, as the quick example does.

### Limits

- `Register` and `Unregister` both throw an `ArgumentException` for a mod id that is not valid (1 to 64 letters, digits, `.`, `_` or `-`). `Register` also throws an `ArgumentNullException` for a null function.
- `Register` returns `false` when the framework could not set up the inventory changes. Then no function is used at all.
- While a save is loading or being written, your containers are not used. The game then uses only the player's own items, as it does without the framework. <span class="tech">(This is `SaveData.Busy`.)</span>
- Your function is skipped for one call if it throws an error or returns `null`. The error is logged once under your mod id, and the other functions still run.
- Do not call the game's own item checks from inside your function (`Inventory.CheckForItem`, `Inventory.GetItemAmountInInventory`, `Inventory.FindAndGetTotalAmmo`). While the framework is asking your function, its own list of your containers is not filled in yet, so such a call is logged once as an error and the game's own answer is used.
- Only one of the two flows can use your containers at a time. If a second one starts while the first is still running, the second one uses none of them.
- A renovation the game runs without taking items and money uses nothing, and with the `MoneyOnly` renovation tweak on, a renovation never takes items, so your containers are not used for it. <span class="tech">(The framework checks `TenementController.ignoreUpgradePricesAndItems` and `TweaksRenovations.MoneyOnly`.)</span>
- The check and the take are separate calls. Your function runs for both and should return the same containers for the same flow, or a requirement the check counted may not be there when the items are taken.
- Not checked in the game. The page describes what the game's code does and what the framework changes, not a run in the game. Expect to be the first to try it.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Files: `src/Api/InventorySources.cs`, `src/Api/InventorySourcesPatches.cs`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

The framework changes some of the game's methods when it starts, so these two flows can use your containers. All of those changes belong to one [feature group](../glossary.md#feature-group), `obenseuer.moddingframework.inventorysources`, the name you will see in the log. Errors from your own function are logged under your mod id.

### When you can call it

- Call `Register` and `Unregister` from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game and Unity only run safely on the main thread (see the [glossary](../glossary.md#main-thread)). The framework calls your function on the main thread too.

### If something fails to set up

The framework changes some of the game's methods when it starts. If a game update breaks one of them, the framework turns off the feature group that depends on it, and writes error lines and a warning to the group's log file in `BepInEx\logs`. The rest of the framework keeps working.

- If the inventory changes can't be set up, `Register` returns `false` and no function is used. The feature group `obenseuer.moddingframework.inventorysources` turns off and error lines and a warning go to the log. <span class="tech">(The game methods involved are `Inventory.CheckForItem`, `Inventory.GetItemAmountInInventory`, `Inventory.FindAndGetTotalAmmo`, `Inventory.FindAndChangeItemAmmo` and `Inventory.RemoveItems`, `TenementController.RemoveRequiredItems`, `TenementController.CheckForItem`, `TenementController.GetItemAmount` and `TenementUpgrade.TakeRequiredItemsAndMoney`, `TaskItemBlueprint.PlayerHasResources`, `ShopBaseUI.Craft` and `FurnitureResource.AddResourceInfo`.)</span>
