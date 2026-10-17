---
title: Inventory
parent: Items and crafting
grand_parent: API reference
has_children: true
nav_order: 6
---

# Inventory

## What it is

This is the player's inventory and the storage in the world around it. The player carries things in a main grid, a backpack and a few equipment slots (hat, watch, hands). Containers are the chests, lockers, shelves and similar storage the player can open. A mod can count, give and take items, see what the player holds in each hand, watch items come and go, and look inside containers.

## When to use it

- Give the player a reward item when a quest step is done.
- Take a required item (a key, a part) from the player before something happens.
- Show a message when the player picks up a certain item.
- Look into a container, for example to show what a shop's storage holds.

## Quick example

This mod waits until a save is loaded, logs how many bottles the player has and how much room is left, then gives two. It also logs every item that is added to or removed from the inventory. Items are found by name with `Items.Find` and used by their id (see [Items](items.md)).

```csharp
using ObenseuerCore;

[ModInfo("example.inventorytour", "Inventory Tour", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class InventoryTour : FrameworkMod
{
    protected override void OnLoad()
    {
        PlayerInventory.ItemAdded += args => Log.Info("added " + args.Count + " of item " + args.ItemId);
        PlayerInventory.ItemRemoved += args => Log.Info("removed " + args.Count + " of item " + args.ItemId);
        Events.WhenReady(Tour);
    }

    private void Tour()
    {
        ItemDef bottle = Items.Find("Empty Glass Bottle");
        if (bottle == null) return;

        Log.Info("bottles: " + PlayerInventory.Count(bottle.Id) + ", free slots: " + PlayerInventory.FreeSlots + "/" + PlayerInventory.Capacity);
        if (PlayerInventory.Give(bottle.Id, 2)) Log.Info("gave 2 bottles");
        else Log.Warn("no room for 2 bottles");
    }
}
```

`Give` refuses, and adds nothing, when the whole amount does not fit, so always check what it returns. `Count`, `Give` and `Take` work with item ids, never names. `ItemAdded` and `ItemRemoved` are events: the framework calls your code each time they happen. See [Using events](../guides/getting-started/using-events.md) for more on events and `WhenReady`.
